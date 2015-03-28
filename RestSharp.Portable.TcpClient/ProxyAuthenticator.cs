using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using RestSharp.Portable.Authenticators;
using RestSharp.Portable.TcpClient.ProxyAuthFactories;

namespace RestSharp.Portable.TcpClient
{
    public class ProxyAuthenticator : IRoundTripAuthenticator
    {
        private static readonly char[] _whiteSpaceCharacters = { ' ', '\t' };

        private static readonly IList<IProxyAuthenticationModuleFactory> _proxyAuthenticationModuleFactories =
            new List<IProxyAuthenticationModuleFactory>()
            {
                new NoneProxyAuthFactory(),
                new HttpBasicProxyAuthFactory(),
                new HttpDigestProxyAuthFactory(),
            };

        private static readonly IDictionary<string, int> _proxyNames =
            _proxyAuthenticationModuleFactories
                .Select((mod, index) => new { mod.ModuleName, index })
                .ToDictionary(x => x.ModuleName, x => x.index, StringComparer.OrdinalIgnoreCase);

        private readonly ICredentials _credentials;

        private readonly IEnumerable<HttpStatusCode> _statusCodes = new List<HttpStatusCode>
        {
            HttpStatusCode.ProxyAuthenticationRequired,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyAuthenticator" /> class.
        /// </summary>
        /// <param name="credentials">Credentials to be queried by the authenticators</param>
        public ProxyAuthenticator(ICredentials credentials)
        {
            _credentials = credentials;
        }

        /// <summary>
        /// Gets all the status codes where a round trip is allowed
        /// </summary>
        public IEnumerable<HttpStatusCode> StatusCodes
        {
            get { return _statusCodes; }
        }

        protected string Method { get; private set; }

        protected string MethodData { get; private set; }

        protected IProxyAuthenticationModule Authenticator { get; set; }

        /// <summary>
        /// Modifies the request to ensure that the authentication requirements are met.
        /// </summary>
        /// <param name="client">Client executing this request</param>
        /// <param name="request">Request to authenticate</param>
        public void Authenticate(IRestClient client, IRestRequest request)
        {
            if (Authenticator == null)
                return;
            Authenticator.Authenticate(client, request);
        }

        public virtual void AuthenticationFailed(IRestClient client, IRestRequest request, IRestResponse response)
        {
            Authenticator = null;

            if (client.Proxy == null)
                return;

            var requestUri = client.BuildUri(request);
            var proxyUri = client.Proxy.GetProxy(requestUri);
            if (proxyUri == null)
                return;

            var authMethods = response
                .Headers.GetValues("Proxy-Authenticate")
                .Select(x => new AuthMethodInfo(x))
                .ToList();

            if (authMethods.Count == 0)
                return;

            var bestAuthMethodItem =
                (from authMethod in authMethods
                 where _proxyNames.ContainsKey(authMethod.Name)
                 select new { Index = _proxyNames[authMethod.Name], Info = authMethod })
                    .OrderByDescending(x => x.Index)
                    .First();

            Method = bestAuthMethodItem.Info.Name;
            MethodData = bestAuthMethodItem.Info.Info;

            var factory = _proxyAuthenticationModuleFactories[bestAuthMethodItem.Index];
            if (factory == null)
                return;

            var credential = _credentials.GetCredential(proxyUri, Method);

            Authenticator = factory.CreateModule(MethodData, credential);
        }

        private class AuthMethodInfo
        {
            public AuthMethodInfo(string headerValue)
            {
                var firstWhitespace = headerValue.IndexOfAny(_whiteSpaceCharacters);
                if (firstWhitespace == -1)
                {
                    Name = headerValue;
                    Info = null;
                }
                else
                {
                    Name = headerValue.Substring(0, firstWhitespace);
                    Info = headerValue.Substring(firstWhitespace).TrimStart();
                }
            }

            public string Name { get; set; }

            public string Info { get; set; }
        }
    }
}
