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

        private static readonly IDictionary<string, IProxyAuthenticationModuleFactory> _proxyAuthenticationModuleFactories =
            new List<IProxyAuthenticationModuleFactory>()
            {
                new NoneProxyAuthFactory(),
                new HttpBasicProxyAuthFactory(),
                new HttpDigestProxyAuthFactory(),
            }.ToDictionary(x => x.Module, StringComparer.OrdinalIgnoreCase);

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

            var header = response.Headers.GetValues("Proxy-Authenticate").FirstOrDefault();
            if (string.IsNullOrWhiteSpace(header))
                return;

            ParseAuthenticateHeader(header.Trim());

            var requestUri = client.BuildUri(request);
            var proxyUri = client.Proxy.GetProxy(requestUri);
            if (proxyUri == null)
                return;

            var credential = _credentials.GetCredential(proxyUri, Method);
            Authenticator = CreateAuthenticationModule(credential);
        }

        protected virtual IProxyAuthenticationModule CreateAuthenticationModule(NetworkCredential credential)
        {
            IProxyAuthenticationModuleFactory factory;
            if (!_proxyAuthenticationModuleFactories.TryGetValue(Method, out factory))
                return null;
            return factory.CreateModule(MethodData, credential);
        }

        private void ParseAuthenticateHeader(string header)
        {
            var firstWhitespace = header.IndexOfAny(_whiteSpaceCharacters);
            if (firstWhitespace == -1)
            {
                Method = header;
                MethodData = null;
            }
            else
            {
                Method = header.Substring(0, firstWhitespace);
                MethodData = header.Substring(firstWhitespace).TrimStart();
            }
        }
    }
}
