using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;

using Xunit;

namespace RestSharp.Portable.TcpClient.Tests
{
    public class TcpClientHttpGet
    {
        [Theory(DisplayName = "TcpClient.TestHttpGet")]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestHttpGet(bool useSsl)
        {
            var scheme = string.Format("http{0}", useSsl ? "s" : string.Empty);
            var client = CreateClient(string.Format("{0}://httpbin.org", scheme));
            var request = new RestRequest("get");
            request.AddParameter("a", 1);
            var response = await client.Execute<HttpBinGetResponse>(request);
            Assert.Equal(string.Format("{0}://httpbin.org/get?a=1", scheme), response.Data.Url);
            Assert.Equal(1, response.Data.Args.Count);
            Assert.True(response.Data.Args.ContainsKey("a"));
            Assert.Equal("1", response.Data.Args["a"]);
        }

        private static RestClient CreateClient(string baseUrl)
        {
            var nativeFactory = new NativeTcpClientFactory();

            var client = new RestClient(baseUrl)
            {
                HttpClientFactory = new DefaultTcpClientFactory(nativeFactory)
                {
                    Proxy = WebRequest.DefaultWebProxy ?? WebRequest.GetSystemWebProxy(),
                },
                CookieContainer = new CookieContainer(),
            };
            return client;
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local", Justification = "Fehler von ReSharper")]
        private class HttpBinGetResponse
        {
            public IDictionary<string, string> Args { get; set; }

            public IDictionary<string, string> Headers { get; set; }

            public string Origin { get; set; }

            public string Url { get; set; }
        }
    }
}
