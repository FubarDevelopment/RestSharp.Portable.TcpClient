using System;
using System.Net;

namespace RestSharp.Portable.TcpClient.ProxyAuthFactories
{
    public class NoneProxyAuthFactory : IProxyAuthenticationModuleFactory
    {
        public string Module
        {
            get { return "None"; }
        }

        public IProxyAuthenticationModule CreateModule(string moduleData, NetworkCredential credential)
        {
            return null;
        }
    }
}
