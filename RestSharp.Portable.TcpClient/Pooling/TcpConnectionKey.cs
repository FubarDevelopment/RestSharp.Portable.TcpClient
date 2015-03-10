using System;

namespace RestSharp.Portable.TcpClient.Pooling
{
    internal class TcpConnectionKey : IComparable<TcpConnectionKey>, IEquatable<TcpConnectionKey>, IComparable
    {
        public TcpConnectionKey(EndPoint address, bool useSsl)
        {
            Address = address;
            UseSsl = useSsl;
        }

        public EndPoint Address { get; private set; }

        public bool UseSsl { get; private set; }

        public bool Equals(TcpConnectionKey other)
        {
            return CompareTo(other) == 0;
        }

        public int CompareTo(TcpConnectionKey other)
        {
            if (ReferenceEquals(this, other))
                return 0;
            if (other == null)
                return 1;
            var result = Address.CompareTo(other.Address);
            if (result != 0)
                return result;
            result = UseSsl.CompareTo(other.UseSsl);
            return result;
        }

        int IComparable.CompareTo(object obj)
        {
            return CompareTo((TcpConnectionKey)obj);
        }

        public override bool Equals(object obj)
        {
            return Equals((TcpConnectionKey)obj);
        }

        public override int GetHashCode()
        {
            return Address.GetHashCode() ^ UseSsl.GetHashCode();
        }
    }
}
