using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RestSharp.Portable.TcpClient.Pooling
{
    internal class TcpConnectionKeyComparer : IComparer<TcpConnectionKey>, IEqualityComparer<TcpConnectionKey>, IComparer, IEqualityComparer
    {
        public static readonly TcpConnectionKeyComparer Default = new TcpConnectionKeyComparer(EndPointComparer.Default);

        private readonly EndPointComparer _endPointComparer;

        public TcpConnectionKeyComparer(EndPointComparer endPointComparer)
        {
            _endPointComparer = endPointComparer;
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            return Equals((TcpConnectionKey)x, (TcpConnectionKey)y);
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            return GetHashCode((TcpConnectionKey)obj);
        }

        int IComparer.Compare(object x, object y)
        {
            return Compare((TcpConnectionKey)x, (TcpConnectionKey)y);
        }

        public int Compare(TcpConnectionKey x, TcpConnectionKey y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (ReferenceEquals(x, null))
                return -1;
            if (ReferenceEquals(y, null))
                return 1;
            var result = _endPointComparer.Compare(x.Address, y.Address);
            if (result != 0)
                return result;
            result = x.UseSsl.CompareTo(y.UseSsl);
            return result;
        }

        public bool Equals(TcpConnectionKey x, TcpConnectionKey y)
        {
            return Compare(x, y) == 0;
        }

        public int GetHashCode(TcpConnectionKey obj)
        {
            var hashCode = 0;
            if (!ReferenceEquals(obj, null))
                hashCode = _endPointComparer.GetHashCode(obj.Address) ^ obj.UseSsl.GetHashCode();
            return hashCode;
        }
    }
}
