using System;
using System.Net;
#if WINRT
using Windows.Networking;
#endif

namespace RestSharp.Portable.TcpClient
{
    public sealed class EndPoint : IComparable<EndPoint>, IComparable, IEquatable<EndPoint>
    {
        public EndPoint(Uri uri)
            : this(uri.GetHostNameType(), uri.Host, uri.Port)
        {
        }

        public EndPoint(string host, int port)
            : this(EndPointUtilities.GetHostNameType(host), host, port)
        {
        }

#if WINRT
        public EndPoint(HostName address, int port)
        {
            Port = port;
            Host = address.ToString();
            HostNameType = address.GetHostNameType();
        }
#elif !PCL
        public EndPoint(IPAddress address, int port)
        {
            Port = port;
            Host = address.ToString();
            HostNameType = address.GetHostNameType();
        }
#endif

#if !WINRT && !PCL
        public EndPoint(IPEndPoint endPoint)
        {
            Port = endPoint.Port;
            Host = endPoint.Address.ToString();
            HostNameType = endPoint.Address.GetHostNameType();
        }
#endif

        internal EndPoint(EndPointType hostNameType, string host, int port)
        {
            Host = host;
            Port = port;
            HostNameType = hostNameType;
        }

        public EndPointType HostNameType { get; private set; }

        public string Host { get; private set; }

        public int Port { get; private set; }

        public Uri ToUri()
        {
            return new Uri(string.Format("tcp://{0}:{1}", Host, Port));
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}", Host, Port);
        }

        public int CompareTo(EndPoint other)
        {
            return EndPointComparer.Default.Compare(this, other);
        }

        int IComparable.CompareTo(object obj)
        {
            return CompareTo((EndPoint)obj);
        }

        public bool Equals(EndPoint other)
        {
            return EndPointComparer.Default.Equals(this, other);
        }

        public override bool Equals(object obj)
        {
            return EndPointComparer.Default.Equals(this, (EndPoint)obj);
        }

        public override int GetHashCode()
        {
            return EndPointComparer.Default.GetHashCode(this);
        }
    }
}
