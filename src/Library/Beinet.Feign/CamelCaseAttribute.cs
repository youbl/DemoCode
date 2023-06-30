using System;

namespace Beinet.Feign
{
    /// <summary>
    /// 定义在类上，表示这个类要使用驼峰进行序列化和反序列化
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class CamelCaseAttribute : Attribute
    {
    }
}