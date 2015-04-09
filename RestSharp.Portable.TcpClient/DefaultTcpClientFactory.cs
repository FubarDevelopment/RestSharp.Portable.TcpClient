using System;
using System.Net.Http;

using RestSharp.Portable.Authenticators;
using RestSharp.Portable.HttpClientImpl;

namespace RestSharp.Portable.TcpClient
{
    public class DefaultTcpClientFactory : DefaultHttpClientFactory
    {
        private readonly AuthenticationChallengeHandler _defaultProxyAuthenticator = new AuthenticationChallengeHandler(AuthHeader.Proxy);

        private readonly INativeTcpClientFactory _tcpClientFactory;

        public DefaultTcpClientFactory(INativeTcpClientFactory tcpClientFactory)
        {
            _defaultProxyAuthenticator.Register(HttpBasicAuthenticator.AuthenticationMethod, new HttpBasicAuthenticator(AuthHeader.Proxy), -1000);
            _defaultProxyAuthenticator.Register(HttpDigestAuthenticator.AuthenticationMethod, new HttpDigestAuthenticator(AuthHeader.Proxy), 1000);

            _tcpClientFactory = tcpClientFactory;
        }

        public bool ResolveHost { get; set; }

        public bool AllowRedirect { get; set; }

        public IAuthenticator ProxyAuthenticator { get; set; }

        protected override HttpMessageHandler CreateMessageHandler(IRestClient client, IRestRequest request)
        {
            var httpClientHandler = new DefaultTcpClientMessageHandler(this)
            {
                ResolveHost = ResolveHost,
                AllowRedirect = AllowRedirect,
                Proxy = client.Proxy,
                ProxyAuthenticator = ProxyAuthenticator ?? _defaultProxyAuthenticator,
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

            public override INativeTcpClientFactory NativeTcpClientFactory
            {
                get { return _factory._tcpClientFactory; }
            }

            protected override AddressCompatibility AddressCompatibility
            {
                get
                {
                    return AddressCompatibility.SupportsHost
                           | AddressCompatibility.SupportsIPv4
                           | AddressCompatibility.SupportsIPv6;
                }
            }
        }
    }
}
