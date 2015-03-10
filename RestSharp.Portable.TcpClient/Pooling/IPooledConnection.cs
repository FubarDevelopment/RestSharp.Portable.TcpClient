using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RestSharp.Portable.TcpClient.Pooling
{
    internal interface IPooledConnection : IDisposable
    {
        TcpConnectionKey Key { get; }

        INativeTcpClient Client { get; }

        Task<Stream> EnsureConnectionIsOpen(EndPoint destinationAddress, CancellationToken cancellationToken);

        void Update(HttpResponseMessage message);

        void Update(HttpResponseMessage message, DateTime now);
    }
}
