﻿using System;
using System.IO;

namespace RestSharp.Portable.TcpClient
{
    internal class NonDisposableStream : Stream
    {
        public NonDisposableStream(Stream baseStream)
        {
            BaseStream = baseStream;
        }

        public Stream BaseStream { get; private set; }

        public override bool CanRead
        {
            get { return BaseStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return BaseStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return BaseStream.CanWrite; }
        }

        public override long Length
        {
            get { return BaseStream.Length; }
        }

        public override long Position
        {
            get { return BaseStream.Position; }
            set { BaseStream.Position = value; }
        }

        public override void Flush()
        {
            BaseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return BaseStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return BaseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            BaseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            BaseStream.Write(buffer, offset, count);
        }

#if !WINRT && !PCL
        public override void Close()
        {
        }
#endif

        protected override void Dispose(bool disposing)
        {
        }
    }
}
