using System;
using System.Linq;
using System.Net;
using System.Text;

namespace RestSharp.Portable.TcpClient.ProxyAuthenticators
{
    public class HttpBasicProxyAuthenticator : IProxyAuthenticationModule
    {
        private readonly string _authHeader;

        /// <summary>Initializes a new instance of the <see cref="HttpBasicProxyAuthenticator"/> class.</summary>
        /// <param name="credential">Network credentials</param>
        public HttpBasicProxyAuthenticator(NetworkCredential credential)
        {
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", credential.UserName, credential.Password)));
            _authHeader = string.Format("Basic {0}", token);
        }

        /// <summary>Modifies the request to ensure that the authentication requirements are met.</summary>
        /// <param name="client">Client executing this request</param>
        /// <param name="request">Request to authenticate</param>
        public void Authenticate(IRestClient client, IRestRequest request)
        {
            // only add the Authorization parameter if it hasn't been added by a previous Execute
            if (request.Parameters.Any(p => p.Name.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase)))
                return;
            request.AddParameter("Proxy-Authorization", _authHeader, ParameterType.HttpHeader);
        }
    }
}
