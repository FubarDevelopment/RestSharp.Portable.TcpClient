using System;
using System.Net;

namespace RestSharp.Portable.TcpClient
{
    public interface IProxyAuthenticationModuleFactory
    {
        string ModuleName { get; }

        IProxyAuthenticationModule CreateModule(string moduleData, NetworkCredential credential);
    }
}
