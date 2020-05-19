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
        static ConcurrentDictionary<Type, List<Attribute>> _typeAtts =
            new ConcurrentDictionary<Type, List<Attribute>>();

//        static ConcurrentDictionary<MethodInfo, RequestMappingAttribute> _methodRequestAtts =
//            new ConcurrentDictionary<MethodInfo, RequestMappingAttribute>();
//
//        static ConcurrentDictionary<MethodInfo, Dictionary<ParameterInfo, RequestArgAttribute>> _methodArgs =
//            new ConcurrentDictionary<MethodInfo, Dictionary<ParameterInfo, RequestArgAttribute>>();

        /// <summary>
        /// 缓存并返回指定类的所有特性列表
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<Attribute> GetAttributes(Type type)
        {
            var ret = _typeAtts.GetOrAdd(type, typeInner =>
            {
                var result = typeInner.GetCustomAttributes(false);
                return result.Select(item => (Attribute) item).ToList();
            });
            return ret;
        }

        /// <summary>
        /// 获取类型的指定特性列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<T> GetCustomAttributes<T>(Type type) where T : Attribute
        {
            var result = GetAttributes(type);

            var ret = new List<T>();
            foreach (var attribute in result)
            {
                if (attribute is T att)
                    ret.Add(att);
            }

            return ret;
        }
        
 

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