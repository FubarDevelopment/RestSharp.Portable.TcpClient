using System;

namespace RestSharp.Portable.TcpClient
{
    [Flags]
    public enum AddressCompatibility
    {
        /// <summary>
        /// Is an IPv4 address supported?
        /// </summary>
        SupportsIPv4 = 0,

        /// <summary>
        /// Is a host name supported?
        /// </summary>
        SupportsHost = 1,

        /// <summary>
        /// Is an IPv6 address supported?
        /// </summary>
        SupportsIPv6 = 2,
    }
}
