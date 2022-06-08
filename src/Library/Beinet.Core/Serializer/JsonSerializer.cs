using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Beinet.Core.CryptExt;
using Beinet.Core.FileExt;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Beinet.Core.Serializer
{
    /// <summary>
    /// 序列化与反序列化JSON实现
    /// </summary>
    public class JsonSerializer : ISerializer
    {
        public static JsonSerializer DEFAULT = new JsonSerializer();

        private static readonly Encoding Utf8 = FileHelper.UTF8_NoBom;

        /// <summary>
        /// 序列化用的属性
        /// </summary>
        static JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = new List<JsonConverter>()
            {
                new IsoDateTimeConverter {DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff"}
            }
        };

        /// <summary>
        /// 驼峰序列化属性
        /// </summary>
        static JsonSerializerSettings _camelSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = new List<JsonConverter>()
            {
                new IsoDateTimeConverter {DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff"}
            },
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        /// <summary>
        /// 反序列化用的属性
        /// </summary>
        static JsonSerializerSettings _deSerializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        /// <summary>
        /// 将一个对象序列化成字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="camel">是否驼峰</param>
        /// <returns></returns>
        public string SerializToStr<T>(T data, bool camel = false)
        {
            // 微软原生用： new System.Web.Extensions.JavaScriptSerializer();
            if (data == null)
            {
                return string.Empty;
            }

            var type = typeof(T);
            if (type == typeof(string))
            {
                return data as string;
            }

            if (type == typeof(Guid))
            {
                return data.ToString();
            }

            if (type == typeof(byte[]))
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                return Convert.ToBase64String(data as byte[]);
            }

            if (camel)
                return JsonConvert.SerializeObject(data, _camelSettings);
            return JsonConvert.SerializeObject(data, _serializerSettings);
        }

        /// <summary>
        /// 将一个对象序列化成字节数组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public byte[] SerializToBytes<T>(T data)
        {
            var result = SerializToStr(data);
            if (string.IsNullOrEmpty(result))
            {
                return null;
            }

            return Utf8.GetBytes(result);
        }


        /// <summary>
        /// 将字符串反序列化为对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"></param>
        /// <returns></returns>
        public T DeSerializFromStr<T>(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return default(T);
            }

            var type = typeof(T);
            if (type == typeof(string))
            {
                return (T) (object) str;
            }

            if (type == typeof(Guid))
            {
                Guid.TryParse(str, out var ret);
                return (T) (object) ret;
            }

            if (type == typeof(byte[]))
            {
                return (T) (object) Convert.FromBase64String(str);
            }

            return JsonConvert.DeserializeObject<T>(str, _deSerializerSettings);
        }

        /// <summary>
        /// 将字节数组反序列化成对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public T DeSerializFromBytes<T>(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return default(T);
            }

            return DeSerializFromStr<T>(Utf8.GetString(data));
        }


        #region 非接口方法

        /// <summary>
        /// 把对象序列化到文件,并返回成功后的文件md5
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filename"></param>
        /// <param name="data"></param>
        public string SerializToFile<T>(string filename, T data)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException(nameof(filename), "filename can't be empty.");
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "data can't be empty.");
            }

            var tmpfile = filename + ".tmp";
            var dir = Path.GetDirectoryName(tmpfile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(tmpfile))
            {
                File.Delete(tmpfile);
            }

            Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
            using (var fs = new FileStream(tmpfile, FileMode.Create, FileAccess.ReadWrite, FileShare.None,
                       1024, FileOptions.WriteThrough))
            using (var stream = new StreamWriter(fs, Utf8))
                //using (JsonTextWriter writer = new JsonTextWriter(file))
            {
                serializer.Serialize(stream, data);
            }

            string md5 = CryptoHelper.GetMD5HashFromFile(tmpfile);
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            File.Move(tmpfile, filename);
            return md5;
        }

        /// <summary>
        /// 从文件反序列对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filename"></param>
        /// <returns></returns>
        public T DeSerializFromFile<T>(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException(nameof(filename), "filename can't be empty.");
            }

            if (!File.Exists(filename))
            {
                throw new ArgumentNullException(nameof(filename), "filename doesn't exists.");
            }

            Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
            using (var file = new StreamReader(filename, Utf8))
            using (var reader = new JsonTextReader(file))
            {
                return serializer.Deserialize<T>(reader);
            }
        }

        /// <summary>
        /// json对象转Dict返回
        /// </summary>
        /// <param name="obj">json对象</param>
        /// <returns></returns>
        public Dictionary<string, string> ToDict(object obj)
        {
            if (obj is string)
            {
                return DeSerializFromStr<Dictionary<string, string>>(Convert.ToString(obj));
            }

            if (obj is IDictionary dictTmp)
            {
                var dict = new Dictionary<string, string>();
                foreach (DictionaryEntry kv in dictTmp)
                {
                    dict[Convert.ToString(kv.Key)] = Convert.ToString(kv.Value);
                }

                return dict;
            }

            if (obj is JToken token && token.Type == JTokenType.Object)
            {
                var jobj = (JObject) obj;
                var dict = new Dictionary<string, string>();
                foreach (var kv in jobj)
                {
                    dict[kv.Key] = Convert.ToString(kv.Value);
                }

                return dict;
            }

            return null;
        }

        #endregion
    }
}