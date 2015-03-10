using System;
using System.Collections.Generic;
using System.Text;

namespace RestSharp.Portable.TcpClient
{
    public class NativeTcpClientConfiguration
    {
        public NativeTcpClientConfiguration(EndPoint endPoint)
        {
            EndPoint = endPoint;
        }

        public EndPoint EndPoint { get; private set; }

        public TimeSpan? Timeout { get; set; }

        public TimeSpan? ReadWriteTimeout { get; set; }
    }
}
