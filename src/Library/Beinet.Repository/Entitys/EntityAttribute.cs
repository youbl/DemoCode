using System;

namespace Beinet.Repository.Entitys
{
    /// <summary>
    /// 实体类标记
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class EntityAttribute : Attribute
    {
        /// <summary>
        /// 用于JQL的表名，而不是物理表名，暂时不支持
        /// </summary>
        public string Name { get; set; }
    }
}
