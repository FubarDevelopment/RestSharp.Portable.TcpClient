using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RestSharp.Portable.TcpClient.ProxyHandlers
{
    public class NoProxyHandler : IProxyHandler
    {
        public string CreateRequestLine(System.Net.Http.HttpMethod method, Version version, Uri requestUri)
        {
            var pathAndQuery = requestUri.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped);
            return string.Format("{1} {2} HTTP/{0}", version, method.Method, pathAndQuery);
        }

        public INativeTcpClient CreateConnection(INativeTcpClientFactory factory, NativeTcpClientConfiguration configuration)
        {
            return factory.CreateClient(configuration);
        }

        public async Task<Stream> CreateSslStream(INativeTcpClientFactory factory, Stream networkStream, EndPoint destination, CancellationToken cancellationToken)
        {
            return await factory.CreateSslStream(networkStream, destination.Host, cancellationToken);
        }
    }
}
