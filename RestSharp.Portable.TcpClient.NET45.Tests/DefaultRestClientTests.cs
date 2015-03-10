using System;
using System.Net;
using System.Threading.Tasks;

using Xunit;

namespace RestSharp.Portable.TcpClient.Tests
{
    public class DefaultRestClientTests
    {
        [Theory(DisplayName = "DefaultClient.TestCookies")]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestCookies(bool useSsl)
        {
            var client = CreateClient(useSsl, "httpbin.org", "/cookies");
            await TestRequests.TestCookies(client);
        }

        private static RestClient CreateClient(bool useSsl, string host, string path)
        {
            var urlBuilder = new UriBuilder(string.Format("http{0}", useSsl ? "s" : string.Empty), host)
            {
                Path = path
            };

            var client = new RestClient(urlBuilder.Uri)
            {
                CookieContainer = new CookieContainer(),
            };
            return client;
        }
    }
}
