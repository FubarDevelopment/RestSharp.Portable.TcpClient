using System;
using System.Net;

namespace RestSharp.Portable.TcpClient
{
    public interface IProxyAuthenticationModuleFactory
    {
        string Module { get; }

        IProxyAuthenticationModule CreateModule(string moduleData, NetworkCredential credential);
    }
}
