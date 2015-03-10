using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RestSharp.Portable.TcpClient
{
    public interface INativeTcpClientFactory
    {
        INativeTcpClient CreateClient(NativeTcpClientConfiguration configuration);

        Task<Stream> CreateSslStream(Stream networkStream, string destinationHost);
    }
}
