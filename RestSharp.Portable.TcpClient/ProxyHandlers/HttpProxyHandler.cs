using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RestSharp.Portable.TcpClient.ProxyHandlers
{
    public class HttpProxyHandler : IProxyHandler
    {
        private readonly Uri _proxyUri;

        public HttpProxyHandler(Uri proxyUri)
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
            return await factory.CreateSslStream(networkStream, destination.Host, cancellationToken);
        }
    }
}
