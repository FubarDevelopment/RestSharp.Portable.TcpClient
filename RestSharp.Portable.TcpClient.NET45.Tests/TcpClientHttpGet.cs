using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;

using RestSharp.Portable.Authenticators;

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
            Assert.True(response.IsSuccess);
            Assert.NotNull(response.Data);
            Assert.Equal(string.Format("{0}://httpbin.org/get?a=1", scheme), response.Data.Url);
            Assert.Equal(1, response.Data.Args.Count);
            Assert.True(response.Data.Args.ContainsKey("a"));
            Assert.Equal("1", response.Data.Args["a"]);
        }

        private static RestClient CreateClient(string baseUrl)
        {
            var nativeFactory = new NativeTcpClientFactory();
            var testCredentials = new CredentialCache();

            // Fiddler
            testCredentials.Add(new Uri("http://localhost:8888"), "Basic", new NetworkCredential("1", "1"));
            testCredentials.Add(new Uri("http://localhost:8889"), "Basic", new NetworkCredential("1", "1"));
            testCredentials.Add(new Uri("http://127.0.0.1:8888"), "Basic", new NetworkCredential("1", "1"));
            testCredentials.Add(new Uri("http://127.0.0.1:8889"), "Basic", new NetworkCredential("1", "1"));

            // Squid
            testCredentials.Add(new Uri("http://localhost:3128"), "Digest", new NetworkCredential("TestUser", "testpwd"));
            testCredentials.Add(new Uri("http://127.0.0.1:3128"), "Digest", new NetworkCredential("TestUser", "testpwd"));

            var challengeHandler = new AuthenticationChallengeHandler(AuthHeader.Proxy);
            challengeHandler.Register("Basic", new HttpBasicAuthenticator(AuthHeader.Proxy), 1);
            challengeHandler.Register("Digest", new HttpDigestAuthenticator(AuthHeader.Proxy), 2);

            var client = new RestClient(baseUrl)
            {
                HttpClientFactory = new DefaultTcpClientFactory(nativeFactory),
                CookieContainer = new CookieContainer(),
                Credentials = testCredentials,
                Authenticator = challengeHandler,
                Proxy = WebRequest.DefaultWebProxy ?? WebRequest.GetSystemWebProxy(),
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
