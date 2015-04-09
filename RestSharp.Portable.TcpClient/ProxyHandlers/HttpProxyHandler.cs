using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RestSharp.Portable.TcpClient.ProxyHandlers
{
    public class HttpProxyHandler : IProxyHandler
    {
        private static readonly HttpMethod _httpConnectMethod = new HttpMethod("CONNECT");

        private readonly Uri _proxyUri;

        private readonly IWebProxy _proxy;

        public HttpProxyHandler(IWebProxy proxy, Uri proxyUri)
        {
            _proxyUri = proxyUri;
            _proxy = proxy;
        }

        public IWebProxy Proxy
        {
            get { return _proxy; }
        }

        public Uri ProxyUri
        {
            get { return _proxyUri; }
        }

        public string CreateRequestLine(System.Net.Http.HttpMethod method, Version version, Uri requestUri)
        {
            return string.Format("{1} {2} HTTP/{0}", version, method.Method, requestUri);
        }

        public INativeTcpClient CreateConnection(INativeTcpClientFactory factory, NativeTcpClientConfiguration configuration)
        {
            var proxyConfiguration = new NativeTcpClientConfiguration(new EndPoint(ProxyUri))
            {
                ReadWriteTimeout = configuration.ReadWriteTimeout,
                Timeout = configuration.Timeout,
            };
            return factory.CreateClient(proxyConfiguration);
        }

        public async Task<Stream> CreateSslStream(TcpClientMessageHandler messageHandler, Stream networkStream, EndPoint destination, CancellationToken cancellationToken)
        {
            var requestMessage = new HttpRequestMessage(_httpConnectMethod, ProxyUri)
            {
                Version = WellKnownHttpVersions.Version11
            };
            requestMessage.Headers.Host = destination.Host;

            using (var writerStream = new NonDisposableStream(networkStream))
            {
                var response = await ConnectToProxy(messageHandler, requestMessage, writerStream, destination, cancellationToken);
                if (!response.IsSuccessStatusCode && response.StatusCode == HttpStatusCode.ProxyAuthenticationRequired)
                {
                    if (messageHandler.ProxyAuthenticator.CanPreAuthenticate(null, requestMessage, Proxy.Credentials))
                        await messageHandler.ProxyAuthenticator.PreAuthenticate(null, requestMessage, Proxy.Credentials);
                }

                response.EnsureSuccessStatusCode();
            }

            return await messageHandler.NativeTcpClientFactory.CreateSslStream(networkStream, destination.Host, cancellationToken);
        }

        private async Task<TcpClientResponseMessage> ConnectToProxy(
            TcpClientMessageHandler messageHandler,
            HttpRequestMessage requestMessage,
            NonDisposableStream networkStream,
            EndPoint destination,
            CancellationToken cancellationToken)
        {
            using (var writer = new StreamWriter(networkStream))
            {
                var requestLine = string.Format("{0} {1} HTTP/{2}", requestMessage.Method, destination, requestMessage.Version);
                await writer.WriteLineAsync(requestLine);
                await writer.WriteHttpHeaderAsync(requestMessage.Headers);
                await writer.WriteLineAsync();
                await writer.FlushAsync();
            }

            var response = new TcpClientResponseMessage(requestMessage, requestMessage.RequestUri, messageHandler, null);
            await response.Parse(networkStream, cancellationToken, null);
            return response;
        }
    }
}
