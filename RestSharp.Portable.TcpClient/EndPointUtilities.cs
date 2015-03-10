using System;
using System.Linq;
using System.Net;
#if !PCL
using System.Net.Sockets;
#endif
using System.Text;
using System.Threading.Tasks;

#if WINRT
using Windows.Networking;
using Windows.Networking.Sockets;
#endif

namespace RestSharp.Portable.TcpClient
{
    internal static class EndPointUtilities
    {
#if !SILVERLIGHT
        private static readonly Random _addressRng = new Random();
#endif

#if PCL
        private static readonly System.Text.RegularExpressions.Regex _ipv4RegEx = new System.Text.RegularExpressions.Regex(@"^\s*\d{1,3}\s*\.\s*\d{1,3}\s*\.\s*\d{1,3}\s*\.\s*\d{1,3}\s*$");
#endif

        private static readonly Encoding _encoding = new UTF8Encoding(false);

        // ReSharper disable once InconsistentNaming
        public enum IPv4SupportLevel
        {
            RequiresIPv4,
            NoPreference,
            RequiresIPv6,
        }

        public static Encoding DefaultEncoding
        {
            get { return _encoding; }
        }

#if WINRT
        public static EndPointType GetHostNameType(this HostName address)
        {
            switch (address.Type)
            {
                case HostNameType.Ipv4:
                    return EndPointType.IPv4;
                case HostNameType.Ipv6:
                    return EndPointType.IPv6;
                case HostNameType.DomainName:
                    return EndPointType.HostName;
            }
            throw new NotSupportedException();
        }
#elif !PCL
        public static EndPointType GetHostNameType(this IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
                return EndPointType.IPv4;
            return EndPointType.IPv6;
        }
#endif

        public static EndPointType GetHostNameType(this Uri address)
        {
#if SILVERLIGHT || PCL
            return GetHostNameType(address.Host);
#else
            switch (address.HostNameType)
            {
                case UriHostNameType.IPv4:
                    return EndPointType.IPv4;
                case UriHostNameType.IPv6:
                    return EndPointType.IPv6;
                default:
                    return EndPointType.HostName;
            }
#endif
        }

        public static EndPointType GetHostNameType(string host)
        {
#if WINRT
            return GetHostNameType(new HostName(host));
#elif PCL
            if (host.Contains(":"))
                return EndPointType.IPv6;
            if (_ipv4RegEx.IsMatch(host))
                return EndPointType.IPv4;
            return EndPointType.HostName;
#else
            IPAddress address;
            if (!IPAddress.TryParse(host, out address))
                return EndPointType.HostName;
            return GetHostNameType(address);
#endif
        }

        public static bool IsLoopBack(string host)
        {
#if WINRT
            var allAddressesTask = DatagramSocket.GetEndpointPairsAsync(new HostName(host), "0").AsTask();
            allAddressesTask.Wait();
            var allAddresses = allAddressesTask.Result
                .Where(x => x != null && x.RemoteHostName != null && (x.RemoteHostName.Type == HostNameType.Ipv4 || x.RemoteHostName.Type == HostNameType.Ipv6))
                .Select(x => x.RemoteHostName)
                .ToList();
            return allAddresses.Any(x => x.IsLoopBack());
#elif SILVERLIGHT || PCL
            return false;
#else
            return Dns.GetHostAddresses(host).Any(IPAddress.IsLoopback);
#endif
        }

        public static async Task<string> ResolveHost(string host, IPv4SupportLevel supportLevel)
        {
#if WINRT
            var allAddresses = (await DatagramSocket.GetEndpointPairsAsync(new HostName(host), "0"))
                .Where(x => x != null && x.RemoteHostName != null)
                .Select(x => x.RemoteHostName)
                .ToList();
            var addressesIPv4 = allAddresses.Where(x => x.Type == HostNameType.Ipv4)
                .ToList();
            HostName addr;
            if (supportLevel == IPv4SupportLevel.RequiresIPv4)
            {
                if (addressesIPv4.Count != 0)
                    addr = addressesIPv4[_addressRng.Next(0, addressesIPv4.Count)];
                else
                    addr = null;
            }
            else
            {
                var addressesIPv6 = allAddresses.Where(x => x.Type == HostNameType.Ipv6)
                    .ToList();
                if (supportLevel == IPv4SupportLevel.NoPreference)
                    addressesIPv6.AddRange(addressesIPv4);
                if (addressesIPv6.Count != 0)
                    addr = addressesIPv6[_addressRng.Next(0, addressesIPv6.Count)];
                else
                    addr = null;
            }
            if (addr == null)
                return null;
            return addr.CanonicalName;
#elif SILVERLIGHT || PCL
            return await Task.Factory.StartNew<string>(() =>
            {
                throw new NotSupportedException();
            });
#else
            var allAddresses = (await Task.Factory.FromAsync<string, IPAddress[]>(Dns.BeginGetHostAddresses, Dns.EndGetHostAddresses, host, null))
                .ToList();
            var addressesIPv4 = allAddresses.Where(x => x.AddressFamily == AddressFamily.InterNetwork)
                .ToList();
            IPAddress addr;
            if (supportLevel == IPv4SupportLevel.RequiresIPv4)
            {
                if (addressesIPv4.Count != 0)
                    addr = addressesIPv4[_addressRng.Next(0, addressesIPv4.Count)];
                else
                    addr = null;
            }
            else
            {
                var addressesIPv6 = allAddresses.Where(x => x.AddressFamily == AddressFamily.InterNetworkV6)
                    .ToList();
                if (supportLevel == IPv4SupportLevel.NoPreference)
                    addressesIPv6.AddRange(addressesIPv4);
                if (addressesIPv6.Count != 0)
                    addr = addressesIPv6[_addressRng.Next(0, addressesIPv6.Count)];
                else
                    addr = null;
            }

            if (addr == null)
                return null;
            return addr.ToString();
#endif
        }

        public static async Task<EndPoint> ResolveHost(EndPoint address, AddressCompatibility addressCompatibility)
        {
            if (address.HostNameType != EndPointType.HostName)
                return address;

            var supportLevel = (addressCompatibility & AddressCompatibility.SupportsIPv6) == AddressCompatibility.SupportsIPv6
                ? IPv4SupportLevel.NoPreference
                : IPv4SupportLevel.RequiresIPv4;

            // Try resolve host name
            var resolvedHost = await ResolveHost(address.Host, supportLevel);
            if (string.IsNullOrEmpty(resolvedHost))
                return null;

            return new EndPoint(resolvedHost, address.Port);
        }
    }
}
