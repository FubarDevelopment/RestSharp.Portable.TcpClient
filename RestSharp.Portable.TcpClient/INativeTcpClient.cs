using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RestSharp.Portable.TcpClient
{
    public interface INativeTcpClient : IDisposable
    {
        bool IsConnected { get; }

        Task Connect(CancellationToken token);

        void Disconnect();

        Stream GetStream();
    }
}
