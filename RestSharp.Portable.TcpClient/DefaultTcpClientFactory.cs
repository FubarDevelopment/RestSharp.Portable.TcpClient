using System;
using System.Net;
using System.Net.Http;

using RestSharp.Portable.HttpClientImpl;

namespace RestSharp.Portable.TcpClient
{
    public class DefaultTcpClientFactory : DefaultHttpClientFactory
    {
        private readonly INativeTcpClientFactory _tcpClientFactory;

        public DefaultTcpClientFactory(INativeTcpClientFactory tcpClientFactory)
        {
            _tcpClientFactory = tcpClientFactory;
        }

        public IWebProxy Proxy { get; set; }

        public bool ResolveHost { get; set; }

        public bool AllowRedirect { get; set; }

        protected override HttpMessageHandler CreateMessageHandler(IRestClient client, IRestRequest request)
        {
            var httpClientHandler = new DefaultTcpClientMessageHandler(this)
            {
                ResolveHost = ResolveHost,
                AllowRedirect = AllowRedirect,
                Proxy = Proxy,
            };

            var cookies = GetCookies(client, request);
            if (cookies != null)
            {
                httpClientHandler.UseCookies = true;
                httpClientHandler.CookieContainer = cookies;
            }

            return httpClientHandler;
        }

        private class DefaultTcpClientMessageHandler : TcpClientMessageHandler
        {
            private readonly DefaultTcpClientFactory _factory;

            internal DefaultTcpClientMessageHandler(DefaultTcpClientFactory factory)
            {
                _factory = factory;
            }

            public IWebProxy Proxy { get; set; }

            protected override AddressCompatibility AddressCompatibility
            {
                get
                {
                    return AddressCompatibility.SupportsHost
                           | AddressCompatibility.SupportsIPv4
                           | AddressCompatibility.SupportsIPv6;
                }
            }

            protected override INativeTcpClientFactory NativeTcpClientFactory
            {
                get { return _factory._tcpClientFactory; }
            }
        }
    }
}
