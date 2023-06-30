using System.Collections.Generic;

namespace Beinet.EnumList
{
    /// <summary>
    /// 枚举的类的信息
    /// </summary>
    public class EnumClassRecord
    {
        /// <summary>
        /// 枚举类的描述信息
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 枚举项列表, key为枚举字符串
        /// </summary>
        public Dictionary<string, EnumRecord> Enums { get; set; } = new Dictionary<string, EnumRecord>();
    }

    /// <summary>
    /// 枚举的项的信息
    /// </summary>
    public class EnumRecord
    {
        /// <summary>
        /// 枚举的值
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// 枚举项的描述信息
        /// </summary>
        public string Description { get; set; }
    }
}