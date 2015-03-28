using System;
using System.Linq;
using System.Net;
using System.Text;

namespace RestSharp.Portable.TcpClient.ProxyAuthenticators
{
    public class HttpBasicProxyAuthenticator : IProxyAuthenticationModule
    {
        private readonly string _authHeaderValue;

        private readonly string _authHeaderName;

        /// <summary>Initializes a new instance of the <see cref="HttpBasicProxyAuthenticator"/> class.</summary>
        /// <param name="credential">Network credentials</param>
        public HttpBasicProxyAuthenticator(NetworkCredential credential)
            : this(credential, "Proxy-Authorization")
        {
        }

        /// <summary>Initializes a new instance of the <see cref="HttpBasicProxyAuthenticator"/> class.</summary>
        /// <param name="credential">Network credentials</param>
        /// <param name="headerEntryName">The header entry name that will be used to store the authentication value</param>
        public HttpBasicProxyAuthenticator(NetworkCredential credential, string headerEntryName)
        {
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", credential.UserName, credential.Password)));
            _authHeaderValue = string.Format("Basic {0}", token);
            _authHeaderName = headerEntryName;
        }

        /// <summary>Modifies the request to ensure that the authentication requirements are met.</summary>
        /// <param name="client">Client executing this request</param>
        /// <param name="request">Request to authenticate</param>
        public void Authenticate(IRestClient client, IRestRequest request)
        {
            // only add the Authorization parameter if it hasn't been added by a previous Execute
            if (request.Parameters.Any(p => p.Name.Equals(_authHeaderName, StringComparison.OrdinalIgnoreCase)))
                return;
            request.AddParameter(_authHeaderName, _authHeaderValue, ParameterType.HttpHeader);
        }
    }
}
