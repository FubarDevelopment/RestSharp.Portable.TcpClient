using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RestSharp.Portable.TcpClient
{
    public class NativeTcpClient : INativeTcpClient
    {
        private readonly System.Net.Sockets.TcpClient _client;

        private readonly EndPoint _endPoint;

        private readonly TimeSpan? _timeout;

        private bool _disposed;

        public NativeTcpClient(NativeTcpClientConfiguration configuration)
        {
            _endPoint = configuration.EndPoint;
            _timeout = configuration.Timeout;
            _client = new System.Net.Sockets.TcpClient();

            if (configuration.ReadWriteTimeout != null)
            {
                _client.ReceiveTimeout =
                    _client.SendTimeout = (int)configuration.ReadWriteTimeout.Value.TotalMilliseconds;
            }
        }

        public bool IsConnected
        {
            get { return _client.Connected; }
        }

        public async Task Connect(CancellationToken token)
        {
            using (var cts = new CancellationTokenSource())
            {
                // Set connection timeout if given.
                if (_timeout != null)
                    cts.CancelAfter(_timeout.Value);

                try
                {
                    await _client.ConnectAsync(_endPoint.Host, _endPoint.Port).HandleCancellation(token, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Rethrow if cancelled by given token
                    if (token.IsCancellationRequested)
                        throw;

                    // Switch to TimeoutException when our own CancellationToken has a requested cancellation.
                    if (cts.IsCancellationRequested)
                        throw new TimeoutException();

                    // Rethrow in every other unknown situation (which should never happen)!
                    throw;
                }
            }
        }

        public void Disconnect()
        {
            _client.Close();
        }

        public Stream GetStream()
        {
            return _client.GetStream();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _client.Close();
        }
    }
}
