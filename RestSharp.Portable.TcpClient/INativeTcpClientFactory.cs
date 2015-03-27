using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RestSharp.Portable.TcpClient
{
    public interface INativeTcpClientFactory
    {
        INativeTcpClient CreateClient(NativeTcpClientConfiguration configuration);

        Task<Stream> CreateSslStream(Stream networkStream, string destinationHost, CancellationToken cancellationToken);
    }
}
