using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RestSharp.Portable.TcpClient
{
    public static class NetworkConverter
    {
        public static bool IsLoopBackForIPv4(string ipv4)
        {
            var data = GetBytesForIPv4(ipv4);
            return data[3] == 1 && data[0] == 127 && data[1] == 0 && data[2] == 0;
        }

        public static bool IsLoopBackForIPv6(string ipv6)
        {
            var data = GetWordsForIPv6(ipv6);
            return IsLoopBackForIPv6(data);
        }

#if WINRT
        public static bool IsLoopBack(this HostName hostName)
        {
            switch (hostName.Type)
            {
                case HostNameType.Ipv4:
                    return IsLoopBackForIPv4(hostName.CanonicalName);
                case HostNameType.Ipv6:
                    return IsLoopBackForIPv6(hostName.CanonicalName);
            }

            throw new NotSupportedException();
        }
#endif

        public static bool IsLoopBack(string ip)
        {
            switch (EndPointUtilities.GetHostNameType(ip))
            {
                case EndPointType.IPv4:
                    return IsLoopBackForIPv4(ip);
                case EndPointType.IPv6:
                    return IsLoopBackForIPv6(ip);
            }

            throw new NotSupportedException();
        }

        public static bool IsLoopBackForIPv6(ushort[] data)
        {
            for (var i = 0; i != 5; ++i)
                if (data[i] != 0)
                    return false;
            if (data[5] == 0)
                return data[6] == 0 && data[7] == 1;
            if (data[5] != 0xFFFF)
                return false;
            return data[6] == 0x7F00 && data[7] == 1;
        }

        public static byte[] GetPortBytes(int port)
        {
            var portBytes = BitConverter.GetBytes((ushort)port);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(portBytes);
            return portBytes;
        }

        public static int ToPort(byte[] data)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }

        public static byte[] GetPortBytes(this EndPoint address)
        {
            return GetPortBytes(address.Port);
        }

#if WINRT
        public static byte[] GetAddressBytes(this HostName hostName)
        {
            switch (hostName.Type)
            {
                case HostNameType.Ipv4:
                    return GetBytesForIPv4(hostName.CanonicalName);
                case HostNameType.Ipv6:
                    return GetBytesForIPv6(hostName.CanonicalName);
            }
            throw new NotSupportedException();
        }
#endif

        public static byte[] GetAddressBytes(this EndPoint address)
        {
            switch (address.HostNameType)
            {
                case EndPointType.IPv4:
                    return GetBytesForIPv4(address.Host);
                case EndPointType.IPv6:
                    return GetBytesForIPv6(address.Host);
            }

            throw new NotSupportedException();
        }

        public static byte[] GetBytesForIPv6(string ipv6)
        {
            var result = new byte[16];
            var idxDst = 0;
            var words = GetWordsForIPv6(ipv6);
            for (var idxSrc = 0; idxSrc != words.Length; ++idxSrc)
            {
                var v = words[idxSrc];
                result[idxDst++] = (byte)((v >> 8) & 0xFF);
                result[idxDst++] = (byte)(v & 0xFF);
            }

            return result;
        }

        public static byte[] GetBytesForIPv4(string ipv4)
        {
            var result = ipv4.Split('.').Select(byte.Parse)
                .ToArray();
            return result;
        }

        public static ushort[] GetWordsForIPv6(string ipv6)
        {
            var data = new ushort[8];
            ipv6 = ipv6.Replace(" ", string.Empty);
            if (ipv6.StartsWith("::ffff:"))
            {
                data[5] = 0xFFFF;
                var ipv4 = ipv6.Substring(7);
                if (ipv4.IndexOf(':') != -1)
                {
                    var parts = ipv4.Split(':')
                        .Select(x => ushort.Parse(x, System.Globalization.NumberStyles.HexNumber))
                        .ToArray();
                    data[6] = parts[0];
                    data[7] = parts[1];
                }
                else
                {
                    var d = GetBytesForIPv4(ipv4);
                    data[6] = (ushort)((d[0] << 8) + d[1]);
                    data[7] = (ushort)((d[2] << 8) + d[3]);
                }
            }
            else
            {
                var parts = ipv6.Split(':')
                    .Select(x => string.IsNullOrWhiteSpace(x) ? -1 : int.Parse(x, System.Globalization.NumberStyles.HexNumber))
                    .ToArray();
                var prefixSize = Array.IndexOf(parts, -1);
                if (prefixSize == -1)
                {
                    if (parts.Length != 8)
                        throw new ArgumentOutOfRangeException();
                    data = parts.Select(x => (ushort)x).ToArray();
                }
                else
                {
                    var nonEmptyIndex = prefixSize;
                    while (nonEmptyIndex < (parts.Length - 1) && parts[nonEmptyIndex + 1] == -1)
                        nonEmptyIndex += 1;
                    var suffixSize = parts.Length - nonEmptyIndex - 1;
                    for (var i = 0; i != prefixSize; ++i)
                        data[i] = (ushort)parts[i];
                    var suffixIndexSrc = parts.Length - suffixSize;
                    var suffixIndexDst = data.Length - suffixSize;
                    for (var i = 0; i != suffixSize; ++i)
                        data[suffixIndexDst++] = (ushort)parts[suffixIndexSrc++];
                }
            }

            return data;
        }

        public static string ToIPv4(byte[] data)
        {
            return string.Join(".", data.Reverse().Select(x => x.ToString()));
        }

        public static string ToIPv6(byte[] data)
        {
            var words = new ushort[8];
            var idxDst = 0;
            for (var idxSrc = 0; idxSrc != data.Length; idxSrc += 2)
                words[idxDst++] = (ushort)((data[idxSrc] << 8) + data[idxSrc + 1]);
            return ToIPv6(words);
        }

        public static string ToIPv6(ushort[] data)
        {
            var zeroRanges = new List<Tuple<int, int>>();
            var startIndex = -1;
            var indexCount = 0;
            for (var i = 0; i != 8; ++i)
            {
                var v = data[i];
                if (v == 0)
                {
                    if (startIndex == -1)
                    {
                        startIndex = i;
                        indexCount = 1;
                    }
                    else
                        indexCount += 1;
                }
                else if (v != 0 && startIndex != -1)
                {
                    zeroRanges.Add(Tuple.Create(startIndex, indexCount));
                    startIndex = -1;
                }
            }

            if (startIndex != -1)
                zeroRanges.Add(Tuple.Create(startIndex, indexCount));

            if (zeroRanges.Count != 0)
            {
                var largestRange = zeroRanges.OrderByDescending(x => x.Item2).First();
                startIndex = largestRange.Item1;
                indexCount = largestRange.Item2;
            }

            ushort[] wordsPrefix, wordsSuffix;
            if (startIndex != -1)
            {
                wordsPrefix = data.Take(startIndex).ToArray();
                wordsSuffix = data.Skip(startIndex + indexCount).ToArray();
            }
            else
            {
                wordsPrefix = data;
                wordsSuffix = null;
            }

            var result = new StringBuilder();
            if (wordsPrefix.Length != 0)
                result.Append(string.Join(":", wordsPrefix.Select(x => x.ToString("x"))));
            if (wordsSuffix != null)
                result
                    .Append("::")
                    .Append(string.Join(":", wordsSuffix.Select(x => x.ToString("x"))));
            return result.ToString();
        }
    }
}
