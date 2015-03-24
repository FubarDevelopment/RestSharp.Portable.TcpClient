using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using RestSharp.Portable.TcpClient.Pooling;
using RestSharp.Portable.TcpClient.ProxyHandlers;

namespace RestSharp.Portable.TcpClient
{
    public abstract class TcpClientMessageHandler : HttpMessageHandler
    {
        private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(100);

        private static readonly TimeSpan s_defaultReadWriteTimeout = TimeSpan.FromSeconds(300);

        private readonly ConcurrentDictionary<TcpConnectionKey, IPooledConnection> _connections = new ConcurrentDictionary<TcpConnectionKey, IPooledConnection>(TcpConnectionKeyComparer.Default);

        protected TcpClientMessageHandler()
        {
            MaximumStatusLineLength = 100;
        }

        public int MaximumStatusLineLength { get; set; }

        public CookieContainer CookieContainer { get; set; }

        public bool UseCookies { get; set; }

        public bool AllowRedirect { get; set; }

        public TimeSpan? Timeout { get; set; }

        public TimeSpan? ReadWriteTimeout { get; set; }

        public bool ResolveHost { get; set; }

        protected abstract AddressCompatibility AddressCompatibility { get; }

        protected abstract INativeTcpClientFactory NativeTcpClientFactory { get; }

        protected virtual IProxyHandler GetProxyHandler(IWebProxy proxy)
        {
            return new NoProxyHandler();
        }

        protected virtual void OnResponseReceived(HttpResponseMessage message)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            bool secondTry;

            HttpResponseMessage response;
            try
            {
                response = await InternalSendAsync(request, request.Method, request.RequestUri, cancellationToken, false);
                secondTry = false;
            }
            catch
            {
                secondTry = true;
                response = null;
            }

            if (secondTry)
                response = await InternalSendAsync(request, request.Method, request.RequestUri, cancellationToken, true);

            if (response.StatusCode == HttpStatusCode.Found || (AllowRedirect && IsRedirectStatusCode(response.StatusCode)))
            {
                HttpMethod requestMethod = (response.StatusCode == HttpStatusCode.SeeOther)
                    ? HttpMethod.Get
                    : request.Method;
                response.Dispose();
                var location = new Uri(request.RequestUri, response.Headers.Location);
                response = await InternalSendAsync(request, requestMethod, location, cancellationToken, false);
            }

            return response;
        }

        private static bool IsRedirectStatusCode(HttpStatusCode httpStatusCode)
        {
            switch (httpStatusCode)
            {
                case HttpStatusCode.MovedPermanently: // 301
                case HttpStatusCode.TemporaryRedirect: // 307
                case HttpStatusCode.SeeOther: // 303
                    return true;
            }

            return false;
        }

        private async Task<IPooledConnection> GetOrCreateConnection(Uri requestUri, bool forceRecreate)
        {
            var useSsl = string.Equals(requestUri.Scheme, "https", StringComparison.OrdinalIgnoreCase);
            var destinationAddress = new EndPoint(requestUri);

            switch (destinationAddress.HostNameType)
            {
                case EndPointType.IPv4:
                    // All SOCKS implementations support IPv4
                    break;
                case EndPointType.IPv6:
                    if ((AddressCompatibility & AddressCompatibility.SupportsIPv6) != AddressCompatibility.SupportsIPv6)
                        throw new NotSupportedException();
                    break;
                case EndPointType.HostName:
                    if ((AddressCompatibility & AddressCompatibility.SupportsHost) != AddressCompatibility.SupportsHost)
                    {
                        destinationAddress = await EndPointUtilities.ResolveHost(destinationAddress, AddressCompatibility);
                        if (destinationAddress == null)
                            throw new NotSupportedException();
                    }

                    break;
            }

            if (ResolveHost && destinationAddress.HostNameType == EndPointType.HostName)
                destinationAddress = await EndPointUtilities.ResolveHost(destinationAddress, AddressCompatibility) ?? destinationAddress;

            var tcpClientConfiguration = new NativeTcpClientConfiguration(destinationAddress)
            {
                Timeout = Timeout ?? s_defaultTimeout,
                ReadWriteTimeout = ReadWriteTimeout ?? s_defaultReadWriteTimeout,
            };

            var proxyHandler = GetProxyHandler(null);

            var key = new TcpConnectionKey(destinationAddress, useSsl);
            var connection = (IPooledConnection)new TcpConnection(
                key,
                NativeTcpClientFactory,
                proxyHandler.CreateConnection(NativeTcpClientFactory, tcpClientConfiguration));

            if (forceRecreate)
            {
                connection = _connections.AddOrUpdate(
                    key,
                    connection,
                    (k, conn) =>
                    {
                        conn.Dispose();
                        return connection;
                    });
            }
            else
            {
                connection = _connections.GetOrAdd(key, connection);
            }

            return connection;
        }

        private async Task<HttpResponseMessage> InternalSendAsync(
            HttpRequestMessage request,
            HttpMethod requestMethod,
            Uri requestUri,
            CancellationToken cancellationToken,
            bool forceRecreate)
        {
            var connection = await GetOrCreateConnection(requestUri, forceRecreate);
            var destinationAddress = new EndPoint(requestUri);
            var stream = await connection.EnsureConnectionIsOpen(destinationAddress, cancellationToken);

            await ValidateHeader(request);

            // Send request
            await WriteRequestHeader(stream, request, requestMethod, requestUri, cancellationToken);
            await WriteContent(stream, request, cancellationToken);
            await stream.FlushAsync(cancellationToken);

            if (UseCookies && CookieContainer == null)
                CookieContainer = new CookieContainer();

            // Parse response
            var response = new TcpClientResponseMessage(request, requestUri, this);
            await response.Parse(stream, cancellationToken);
            OnResponseReceived(response);
            return response;
        }

        private async Task ValidateHeader(HttpRequestMessage request)
        {
            if (request.Method != HttpMethod.Put && request.Method != HttpMethod.Post)
                return;
            if (request.Content.Headers.ContentLength.HasValue)
                return;
            await request.Content.LoadIntoBufferAsync();
            if (request.Content.Headers.ContentLength.HasValue)
                return;
            throw new NotSupportedException("You must specify a content length when Keep-Alive is used.");
        }

        private async Task WriteContent(Stream stream, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content == null)
                return;
            await stream.FlushAsync(cancellationToken);
            using (var input = await request.Content.ReadAsStreamAsync())
                await input.CopyToAsync(stream, 4096, cancellationToken);
        }

        private async Task WriteRequestHeader(Stream stream, HttpRequestMessage request, HttpMethod requestMethod, Uri requestUri, CancellationToken cancellationToken)
        {
            using (var writer = new StringWriter
            {
                NewLine = "\r\n"
            })
            {
                var headers = new WebHeaderCollection
                {
                    { "Host", requestUri.Host }
                };
                if (request.Version >= WellKnownHttpVersions.Version10)
                    headers.Add("Connection", "Keep-Alive");

                if (UseCookies && CookieContainer != null)
                {
                    var cookieHeader = CookieContainer.GetCookieHeader(request.RequestUri);
                    if (!string.IsNullOrEmpty(cookieHeader))
                        headers.Add("Cookie", cookieHeader);
                }

                headers.AddHeaders(request);

                var proxyHandler = GetProxyHandler(null);
                var requestLine = proxyHandler.CreateRequestLine(
                    requestMethod,
                    request.Version ?? new Version(1, 1),
                    requestUri);
                writer.WriteLine(requestLine);
                foreach (var header in headers)
                    writer.WriteLine("{0}: {1}", header.Key, string.Join(",", header.Value));

                writer.WriteLine();

                var encoding = new UTF8Encoding(false);
                var data = encoding.GetBytes(writer.ToString());
                await stream.WriteAsync(data, 0, data.Length, cancellationToken);
            }
        }
    }
}
