using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using System.Text;

namespace Beinet.Request
{
    /// <summary>
    /// 用于复制响应数据流的类
    /// </summary>
    public class ExtendHttpWebResponse : HttpWebResponse
    {
        private static readonly StreamingContext _context = new StreamingContext(StreamingContextStates.Clone);
        private static readonly MethodInfo _method =
            typeof(HttpWebResponse).GetMethod("GetObjectData", BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly HttpWebResponse _baseHttpWebResponse;
        private MemoryStream _memoryStream;
        

        public ExtendHttpWebResponse(HttpWebResponse baseHttpWebResponse) :
#pragma warning disable 618
            base(GetSerializationInfo(baseHttpWebResponse), _context)
#pragma warning restore 618
        {
            _baseHttpWebResponse = baseHttpWebResponse;
        }

        static SerializationInfo GetSerializationInfo(HttpWebResponse response)
        {
            var serializationInfo = new SerializationInfo(typeof(HttpWebResponse), new FormatterConverter());
            _method?.Invoke(response, new object[] { serializationInfo, _context });
            return serializationInfo;
        }

        public override bool IsFromCache => this._baseHttpWebResponse.IsFromCache;

        public override bool IsMutuallyAuthenticated => this._baseHttpWebResponse.IsMutuallyAuthenticated;

        public override long ContentLength
        {
            get => this._baseHttpWebResponse.ContentLength;
            set => this._baseHttpWebResponse.ContentLength = value;
        }

        public override string ContentType
        {
            get => this._baseHttpWebResponse.ContentType;
            set => this._baseHttpWebResponse.ContentType = value;
        }

        public override Uri ResponseUri => this._baseHttpWebResponse.ResponseUri;

        public override WebHeaderCollection Headers => this._baseHttpWebResponse.Headers;

        public override bool SupportsHeaders => this._baseHttpWebResponse.SupportsHeaders;

        /// <summary>
        /// 返回可重复读的流
        /// </summary>
        /// <returns></returns>
        public override Stream GetResponseStream()
        {
            if (_memoryStream == null)
            {
                using (var baseStream = this._baseHttpWebResponse.GetResponseStream())
                {
                    if (baseStream == null)
                        return null;
                    _memoryStream = new MemoryStream();
                    // 复制出来后关闭源流
                    baseStream.CopyTo(_memoryStream);
                }
            }
            _memoryStream.Position = 0;
            return _memoryStream;
        }

        public virtual byte[] GetResponseData()
        {
            var stream = GetResponseStream();
            if (stream == null)
                return null;
            var len = (int)stream.Length;
            var arr = new byte[len];
            stream.Read(arr, 0, len);
            return arr;
        }
        /// <summary>
        /// 读取Request BODY的字符串内容
        /// </summary>
        /// <returns></returns>
        public virtual string GetResponseStr()
        {
            var arr = GetResponseData();
            if (arr == null)
                return null;
            return Encoding.UTF8.GetString(arr);
        }


        public override void Close()
        {
            _memoryStream?.Close();
            this._baseHttpWebResponse.Close();
        }

        public override ObjRef CreateObjRef(Type requestedType)
        {
            return this._baseHttpWebResponse.CreateObjRef(requestedType);
        }

        public override bool Equals(object obj)
        {
            return this._baseHttpWebResponse.Equals(obj);
        }

        public override int GetHashCode()
        {
            return this._baseHttpWebResponse.GetHashCode();
        }

        protected override void Dispose(bool disposing)
        {
            _memoryStream?.Dispose();
            this._baseHttpWebResponse.Dispose();
            base.Dispose(disposing);
        }

        public override object InitializeLifetimeService()
        {
            return this._baseHttpWebResponse.InitializeLifetimeService();
        }

        public override string ToString()
        {
            return this._baseHttpWebResponse.ToString();
        }
    }
}
