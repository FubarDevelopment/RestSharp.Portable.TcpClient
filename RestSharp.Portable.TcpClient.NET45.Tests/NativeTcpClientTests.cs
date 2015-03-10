using System;
using System.Net;
using System.Threading.Tasks;

using Xunit;

namespace RestSharp.Portable.TcpClient.Tests
{
    public class NativeTcpClientTests
    {
        [Theory(DisplayName = "TcpClient.TestCookies")]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task TestCookies(bool useSsl, bool useHost)
        {
            var client = CreateClient(useSsl, useHost, "httpbin.org", "/cookies");
            await TestRequests.TestCookies(client);
        }

        private static RestClient CreateClient(bool useSsl, bool useHost, string host, string path)
        {
            var nativeFactory = new NativeTcpClientFactory();
            if (!useHost && useSsl)
            {
                nativeFactory.CertificateValidationCallback = (sender, certificate, chain, errors) => true;
            }

            var urlBuilder = new UriBuilder(string.Format("http{0}", useSsl ? "s" : string.Empty), host)
            {
                Path = path
            };

            var client = new RestClient(urlBuilder.Uri)
            {
                HttpClientFactory = new DefaultTcpClientFactory(nativeFactory)
                {
                    Proxy = WebRequest.DefaultWebProxy ?? WebRequest.GetSystemWebProxy(),
                    ResolveHost = !useHost,
                },
                CookieContainer = new CookieContainer(),
            };
            return client;
        }
    }
}
