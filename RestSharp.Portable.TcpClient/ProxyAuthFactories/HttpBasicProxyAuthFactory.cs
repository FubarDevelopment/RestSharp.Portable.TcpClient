using System;
using System.Net;

using RestSharp.Portable.TcpClient.ProxyAuthenticators;

namespace RestSharp.Portable.TcpClient.ProxyAuthFactories
{
    public class HttpBasicProxyAuthFactory : IProxyAuthenticationModuleFactory
    {
        public string ModuleName
        {
            get { return "Basic"; }
        }

        public IProxyAuthenticationModule CreateModule(string moduleData, NetworkCredential credential)
        {
            if (credential == null)
                return null;
            return new HttpBasicProxyAuthenticator(credential);
        }
    }
}
