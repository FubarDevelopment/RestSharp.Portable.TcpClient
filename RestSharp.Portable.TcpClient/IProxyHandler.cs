using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RestSharp.Portable.TcpClient
{
    public interface IProxyHandler
    {
        string CreateRequestLine(HttpMethod method, Version version, Uri requestUri);

        INativeTcpClient CreateConnection(INativeTcpClientFactory factory, NativeTcpClientConfiguration configuration);

        Task<Stream> CreateSslStream(INativeTcpClientFactory factory, Stream networkStream, EndPoint destination, CancellationToken cancellationToken);
    }
}
