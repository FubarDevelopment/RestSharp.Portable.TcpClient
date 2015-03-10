using System;

using Xunit;

namespace RestSharp.Portable.TcpClient.Tests
{
    public sealed class SocksProxyAvailableTheoryAttribute : TheoryAttribute
    {
        public static readonly Uri SocksUri = new Uri("socks://localhost:9150");
        private static bool? _socksProxyFound;
        private static Exception _socksProxyDetectionError;

        public SocksProxyAvailableTheoryAttribute()
        {
            if (_socksProxyFound == null)
            {
                try
                {
                    var socksAddress = new EndPoint(SocksUri);
                    var tcpClient = new System.Net.Sockets.TcpClient { ReceiveTimeout = 5000 };
                    var result = tcpClient.BeginConnect(socksAddress.Host, socksAddress.Port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                    if (!success)
                        throw new Exception("Connect failed");
                    try
                    {
                        tcpClient.EndConnect(result);
                        _socksProxyFound = true;
                    }
                    finally
                    {
                        tcpClient.Close();
                    }
                }
                catch (Exception ex)
                {
                    _socksProxyDetectionError = ex;
                    _socksProxyFound = false;
                }
            }

            if (!_socksProxyFound.Value)
            {
                Skip = string.Format("SOCKS proxy not found ({0})", _socksProxyDetectionError.Message);
            }
        }
    }
}
