using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RestSharp.Portable.TcpClient.ProxyHandlers
{
    public class HttpConnectProxyHandler : IProxyHandler
    {
        private readonly Uri _proxyUri;

        public HttpConnectProxyHandler(Uri proxyUri)
        {
            _proxyUri = proxyUri;
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

        public async Task<Stream> CreateSslStream(INativeTcpClientFactory factory, Stream networkStream, EndPoint destination, CancellationToken cancellationToken)
        {
            var proxyHeaders = new WebHeaderCollection
            {
                { "Host", destination.Host },
            };

            using (var writerStream = new NonDisposableStream(networkStream))
            {
                var response = await ConnectToProxy(proxyHeaders, factory, writerStream, destination, cancellationToken);
                response.EnsureSuccessStatusCode();
            }

            return await factory.CreateSslStream(networkStream, destination.Host, cancellationToken);
        }

        private async Task<TcpClientResponseMessage> ConnectToProxy(
            WebHeaderCollection headers,
            INativeTcpClientFactory factory,
            NonDisposableStream networkStream,
            EndPoint destination,
            CancellationToken cancellationToken)
        {
            using (var writer = new StreamWriter(networkStream))
            {
                var requestLine = string.Format("CONNECT {0} HTTP/1.1", destination);
                await writer.WriteLineAsync(requestLine);
                await writer.WriteHttpHeaderAsync(headers);
                await writer.WriteLineAsync();
                await writer.FlushAsync();
            }

            var response = new TcpClientResponseMessage(null, null, null);
            await response.Parse(networkStream, cancellationToken, null);
            return response;
        }
    }
}
