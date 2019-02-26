using System.IO;

namespace Beinet.Request
{
    /// <summary>
    /// 用于复制Request请求BODY的Stream类
    /// </summary>
    internal class ExtendHttpRequestStream : Stream
    {
        private readonly Stream baseStream;
        public virtual MemoryStream CopyedStream { get; } = new MemoryStream();

        public ExtendHttpRequestStream(Stream baseStream)
        {
            this.baseStream = baseStream;
        }


        /// <summary>
        /// 写入BODY里，双写到copyStream
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            CopyedStream.Write(buffer, offset, count);
            baseStream.Write(buffer, offset, count);
        }



        public override void Flush()
        {
            this.baseStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.baseStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.baseStream.Read(buffer, offset, count);
        }


        public override bool CanRead => this.baseStream.CanRead;
        public override bool CanSeek => this.baseStream.CanSeek;
        public override bool CanWrite => this.baseStream.CanWrite;
        public override long Length => this.baseStream.Length;

        public override long Position
        {
            get => this.baseStream.Position;
            set => this.baseStream.Position = value;
        }


        protected override void Dispose(bool disposing)
        {
            baseStream?.Dispose();
            CopyedStream?.Dispose();
            base.Dispose(disposing);
        }
    }
}
