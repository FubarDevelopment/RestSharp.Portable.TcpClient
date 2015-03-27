using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RestSharp.Portable.TcpClient.Pooling
{
    internal class TcpConnection : IPooledConnection
    {
        public static readonly TimeSpan SafeTimeoutMargin = TimeSpan.FromSeconds(0.5);

        private const int InfiniteUsageCount = -1;

        private static readonly TimeSpan s_infiniteLifetime = TimeSpan.FromMilliseconds(Timeout.Infinite);

        private readonly TcpClientMessageHandler _messageHandler;

        private readonly IProxyHandler _proxyHandler;

        public TcpConnection(TcpConnectionKey key, TcpClientMessageHandler messageHandler, INativeTcpClient client, IProxyHandler proxyHandler)
        {
            Key = key;
            _messageHandler = messageHandler;
            _proxyHandler = proxyHandler;
            Client = client;
            Lifetime = s_infiniteLifetime;
            MaxUsageCount = InfiniteUsageCount;
        }

        public TimeSpan Lifetime { get; private set; }

        public int MaxUsageCount { get; private set; }

        public DateTime LastUsage { get; private set; }

        public DateTime MaxValidTimestamp { get; private set; }

        public int UsageCount { get; private set; }

        public bool LimitsSpecified
        {
            get
            {
                return Lifetime != s_infiniteLifetime
                       || MaxUsageCount != -1;
            }
        }

        public TcpConnectionKey Key { get; private set; }

        public INativeTcpClient Client { get; private set; }

        public Stream Stream { get; private set; }

        public async Task<Stream> EnsureConnectionIsOpen(EndPoint destinationAddress, CancellationToken cancellationToken)
        {
            if (!IsValid(DateTime.UtcNow))
                Close();

            if (Stream != null && Client.IsConnected)
                return Stream;

            await Client.Connect(cancellationToken);
            var stream = Client.GetStream();
            if (Key.UseSsl)
                stream = await _proxyHandler.CreateSslStream(_messageHandler, stream, destinationAddress, cancellationToken);

            Stream = stream;
            return stream;
        }

        public void Dispose()
        {
            if (Stream != null)
                Stream.Dispose();
            Client.Dispose();
        }

        public void Update(System.Net.Http.HttpResponseMessage message)
        {
            Update(message, DateTime.UtcNow);
        }

        public void Update(System.Net.Http.HttpResponseMessage message, DateTime now)
        {
            var keepAlive = message.Version >= WellKnownHttpVersions.Version10 ||
                            message.Headers.Connection.Any(
                                x => x.IndexOf("Keep-Alive", 0, StringComparison.OrdinalIgnoreCase) != -1);

            if (!keepAlive)
            {
                MaxUsageCount = 1;
            }
            else
            {
                IEnumerable<string> keepAliveValues;
                if (message.Headers.TryGetValues("Keep-Alive", out keepAliveValues))
                {
                    var kaValues = GetKeepAliveValues(keepAliveValues);
                    string kaValue;
                    if (kaValues.TryGetValue("timeout", out kaValue))
                    {
                        Lifetime = TimeSpan.FromSeconds(int.Parse(kaValue));
                    }

                    if (kaValues.TryGetValue("max", out kaValue))
                    {
                        MaxUsageCount = int.Parse(kaValue);
                        UsageCount = 0;
                    }
                }

                if (!LimitsSpecified)
                {
                    Lifetime = TimeSpan.FromSeconds(5);
                }
            }

            UsageCount += 1;
            LastUsage = now;
            if (Lifetime != s_infiniteLifetime)
                MaxValidTimestamp = LastUsage + Lifetime - SafeTimeoutMargin;
        }

        public bool IsValid(DateTime now)
        {
            var timeoutExceeded = (Lifetime != s_infiniteLifetime) && (now >= MaxValidTimestamp);
            var usageCountExceeded = (MaxUsageCount != -1) && (UsageCount >= MaxUsageCount);
            return !timeoutExceeded && !usageCountExceeded;
        }

        private IDictionary<string, string> GetKeepAliveValues(IEnumerable<string> values)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var value in values)
            {
                var equalSignPos = value.IndexOf('=');
                var kaKey = ((equalSignPos == -1) ? value : value.Substring(0, equalSignPos)).Trim();
                var kaValue = (equalSignPos == -1) ? string.Empty : value.Substring(equalSignPos + 1).Trim();
                result[kaKey] = kaValue;
            }

            return result;
        }

        private void Close()
        {
            if (Stream != null)
            {
                Stream.Dispose();
                Stream = null;
            }

            if (Client.IsConnected)
                Client.Disconnect();
        }
    }
}
