using System;
using System.Net;

using RestSharp.Portable.TcpClient.ProxyAuthenticators;

namespace RestSharp.Portable.TcpClient.ProxyAuthFactories
{
    public class HttpDigestProxyAuthFactory : IProxyAuthenticationModuleFactory
    {
        public string Module
        {
            get { return "Digest"; }
        }

        public IProxyAuthenticationModule CreateModule(string moduleData, NetworkCredential credential)
        {
            if (credential == null)
                return null;
            return new HttpDigestProxyAuthenticator(credential, moduleData);
        }
    }
}
