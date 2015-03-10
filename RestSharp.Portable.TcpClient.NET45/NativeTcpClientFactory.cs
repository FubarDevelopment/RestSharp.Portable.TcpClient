using System;
using System.IO;
using System.Net.Security;
using System.Threading.Tasks;

namespace RestSharp.Portable.TcpClient
{
    public class NativeTcpClientFactory : INativeTcpClientFactory
    {
        public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; }

        public INativeTcpClient CreateClient(NativeTcpClientConfiguration configuration)
        {
            return new NativeTcpClient(configuration);
        }

        public async Task<Stream> CreateSslStream(Stream networkStream, string destinationHost)
        {
            SslStream sslStream;

            if (CertificateValidationCallback != null)
            {
                sslStream = new SslStream(networkStream, true, CertificateValidationCallback);
            }
            else
            {
                sslStream = new SslStream(networkStream, true);
            }

            await sslStream.AuthenticateAsClientAsync(destinationHost);
            return sslStream;
        }
    }
}
