using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DemoCodeWeb
{
    /// <summary>
    /// 支持jsonp的类
    /// </summary>
    public class JsonpMediaTypeFormatter : JsonMediaTypeFormatter
    {
        /// <summary>
        /// 
        /// </summary>
        public string Callback { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback"></param>

        public JsonpMediaTypeFormatter(string callback = null)
        {
            this.Callback = callback;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="writeStream"></param>
        /// <param name="content"></param>
        /// <param name="transportContext"></param>
        /// <returns></returns>
        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content,
            TransportContext transportContext)
        {
            if (string.IsNullOrEmpty(this.Callback))
            {
                return base.WriteToStreamAsync(type, value, writeStream, content, transportContext);
            }
            try
            {
                this.WriteToStream(value, writeStream);
                return Task.FromResult(new AsyncVoid());
            }
            catch (Exception exception)
            {
                var source = new TaskCompletionSource<AsyncVoid>();
                source.SetException(exception);
                return source.Task;
            }
        }

        private void WriteToStream(object value, Stream writeStream)
        {
            var serializer = JsonSerializer.Create(this.SerializerSettings);
            using (var streamWriter = new StreamWriter(writeStream, this.SupportedEncodings.First()))
            using (var jsonTextWriter = new JsonTextWriter(streamWriter)
            {
                CloseOutput = false
            })
            {
                jsonTextWriter.WriteRaw(this.Callback + "(");
                serializer.Serialize(jsonTextWriter, value);
                jsonTextWriter.WriteRaw(")");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="request"></param>
        /// <param name="mediaType"></param>
        /// <returns></returns>
        public override MediaTypeFormatter GetPerRequestFormatterInstance(Type type, HttpRequestMessage request,
            MediaTypeHeaderValue mediaType)
        {
            if (request.Method != HttpMethod.Get)
            {
                return this;
            }

            // var callback = request.GetQueryNameValuePairs().FirstOrDefault(item => item.Key == "callback");
            if (request.GetQueryNameValuePairs().Distinct().ToDictionary(pair => pair.Key,
                pair => pair.Value).TryGetValue("callback", out var callback))
            {
                return new JsonpMediaTypeFormatter(callback);
            }
            return this;
        }

        [StructLayout(LayoutKind.Sequential, Size = 1)]
        private struct AsyncVoid
        {
        }
    }
}