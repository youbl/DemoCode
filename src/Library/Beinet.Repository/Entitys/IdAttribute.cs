using System;

namespace Beinet.Repository.Entitys
{
    /// <summary>
    /// 实体类的主键标记
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class IdAttribute : Attribute
    {

    }
}
