using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

using RestSharp.Portable.TcpClient.Pooling;

namespace RestSharp.Portable.TcpClient
{
    public interface IProxyHandler
    {
        string CreateRequestLine(HttpMethod method, Version version, Uri requestUri);

        INativeTcpClient CreateConnection(INativeTcpClientFactory factory, NativeTcpClientConfiguration configuration);
    }
}
