using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Xunit;

namespace RestSharp.Portable.TcpClient.Tests
{
    public class TestRequests
    {
        public static async Task TestCookies(RestClient client)
        {
            await TestSetCookies(client);
            await TestGetCookies(client);
            await TestDeleteCookies(client);
        }

        private static async Task TestDeleteCookies(RestClient client)
        {
            var request = new RestRequest("delete");
            request.AddQueryParameter("n1", null);
            var response = await client.Execute<ResponseData>(request);
            var clientCookies = client.CookieContainer.GetCookies(client.BaseUrl).Cast<Cookie>()
                .ToDictionary(x => x.Name);
            Assert.Equal(0, clientCookies.Count);
            Assert.Equal(0, response.Data.Cookies.Count);
            foreach (var expectedCookie in response.Data.Cookies)
            {
                var k = expectedCookie.Key;
                var v = expectedCookie.Value;
                Assert.True(clientCookies.ContainsKey(k));
                Assert.Equal(v, clientCookies[k].Value);
            }
        }

        private static async Task TestGetCookies(RestClient client)
        {
            var request = new RestRequest();
            var response = await client.Execute<ResponseData>(request);
            var clientCookies = client.CookieContainer.GetCookies(client.BaseUrl).Cast<Cookie>()
                .ToDictionary(x => x.Name);
            Assert.Equal(1, clientCookies.Count);
            Assert.Equal(1, response.Data.Cookies.Count);
            foreach (var expectedCookie in response.Data.Cookies)
            {
                var k = expectedCookie.Key;
                var v = expectedCookie.Value;
                Assert.True(clientCookies.ContainsKey(k));
                Assert.Equal(v, clientCookies[k].Value);
            }
        }

        private static async Task TestSetCookies(RestClient client)
        {
            var request = new RestRequest("set");
            request.AddQueryParameter("n1", "v1");
            var response = await client.Execute<ResponseData>(request);
            var clientCookies = client.CookieContainer.GetCookies(client.BaseUrl).Cast<Cookie>()
                .ToDictionary(x => x.Name);
            Assert.Equal(1, clientCookies.Count);
            Assert.Equal(1, response.Data.Cookies.Count);
            foreach (var expectedCookie in response.Data.Cookies)
            {
                var k = expectedCookie.Key;
                var v = expectedCookie.Value;
                Assert.True(clientCookies.ContainsKey(k));
                Assert.Equal(v, clientCookies[k].Value);
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class ResponseData
        {
            public Dictionary<string, string> Cookies { get; set; }
        }
    }
}
