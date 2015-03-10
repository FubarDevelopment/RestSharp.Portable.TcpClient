using System;
using System.Collections;
using System.Collections.Generic;

namespace RestSharp.Portable.TcpClient
{
    public sealed class EndPointComparer : IComparer<EndPoint>, IEqualityComparer<EndPoint>, IComparer, IEqualityComparer
    {
        public static readonly EndPointComparer Default = new EndPointComparer(StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase);

        private readonly IComparer<string> _hostNameComparer;

        private readonly IEqualityComparer<string> _hostNameEqualityComparer;

        public EndPointComparer(IComparer<string> hostNameComparer, IEqualityComparer<string> hostNameEqualityComparer)
        {
            _hostNameComparer = hostNameComparer;
            _hostNameEqualityComparer = hostNameEqualityComparer;
        }

        public int Compare(EndPoint x, EndPoint y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (ReferenceEquals(x, null))
                return -1;
            if (ReferenceEquals(y, null))
                return 1;
            var result = x.HostNameType.CompareTo(y.HostNameType);
            if (result != 0)
                return result;
            result = _hostNameComparer.Compare(x.Host, y.Host);
            if (result != 0)
                return result;
            return x.Port.CompareTo(y.Port);
        }

        public bool Equals(EndPoint x, EndPoint y)
        {
            return Compare(x, y) == 0;
        }

        public int GetHashCode(EndPoint obj)
        {
            var hashCode = 0;
            if (ReferenceEquals(obj, null))
                return hashCode;
            hashCode ^= obj.HostNameType.GetHashCode();
            hashCode ^= _hostNameEqualityComparer.GetHashCode(obj.Host);
            hashCode ^= obj.Port.GetHashCode();
            return hashCode;
        }

        int IComparer.Compare(object x, object y)
        {
            return Compare((EndPoint)x, (EndPoint)y);
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            return Equals((EndPoint)x, (EndPoint)y);
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            return GetHashCode((EndPoint)obj);
        }
    }
}
