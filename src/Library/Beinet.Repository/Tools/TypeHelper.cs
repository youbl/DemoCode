using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Beinet.Repository.Tools
{
    /// <summary>
    /// 类型的辅助方法
    /// </summary>
    internal static class TypeHelper
    {
        /// <summary>
        /// 返回指定类型的默认值
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static object GetDefaultValue(Type t)
        {
            if (t != typeof(void) && t.IsValueType)
                return Activator.CreateInstance(t);

            return null;
        }


    }
}