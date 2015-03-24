using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace RestSharp.Portable.TcpClient.ProxyHandlers
{
    public class NoProxyHandler : IProxyHandler
    {
        public string CreateRequestLine(System.Net.Http.HttpMethod method, Version version, Uri requestUri)
        {
            var pathAndQuery = requestUri.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped);
            return string.Format("{1} {2} HTTP/{0}", version, method.Method, pathAndQuery);
        }

        public INativeTcpClient CreateConnection(INativeTcpClientFactory factory, NativeTcpClientConfiguration configuration)
        {
            return factory.CreateClient(configuration);
        }
    }
}
