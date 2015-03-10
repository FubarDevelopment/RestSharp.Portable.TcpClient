using System;

namespace RestSharp.Portable.TcpClient
{
    /// <summary>
    /// The end point type
    /// </summary>
    public enum EndPointType
    {
        /// <summary>
        /// IPv4 address
        /// </summary>
        // ReSharper disable once InconsistentNaming
        IPv4,

        /// <summary>
        /// IPv6 address
        /// </summary>
        // ReSharper disable once InconsistentNaming
        IPv6,

        /// <summary>
        /// Host name that has to be resolved yet.
        /// </summary>
        HostName,
    }
}
