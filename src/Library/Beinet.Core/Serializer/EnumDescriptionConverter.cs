using System;
using Beinet.Core.EnumExt;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Beinet.Core.Serializer
{
    /// <summary>
    /// 枚举类型，使用JSON序列化时，可以序列化为[Description("xxx")]里的字符串，
    /// 而不是数值。
    /// 使用方法： 在enum类上添加特性：
    /// [JsonConverter(typeof(EnumDescriptionConverter))]
    /// </summary>
    public class EnumDescriptionConverter : StringEnumConverter
    {
        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (value != null && value is Enum enumValue)
            {
                writer.WriteValue(enumValue.GetDesc());
                return;
            }

            base.WriteJson(writer, value, serializer);
        }
    }
}