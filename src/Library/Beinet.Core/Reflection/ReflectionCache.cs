using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Beinet.Core.Reflection
{
    /// <summary>
    /// 反射方法相关的缓存类类
    /// </summary>
    internal static class ReflectionCache
    {
        private static ConcurrentDictionary<string, ISetter> SetterCache { get; } = new ConcurrentDictionary<string, ISetter>();
        private static ConcurrentDictionary<string, IGetter> GetterCache { get; } = new ConcurrentDictionary<string, IGetter>();

        private static ConcurrentDictionary<string, FastMethodInfo> DelegateCache { get; } = new ConcurrentDictionary<string, FastMethodInfo>();

        private static ConcurrentDictionary<string, MethodInfo> MethodInfoCache { get; } = new ConcurrentDictionary<string, MethodInfo>();

        public static string GetCacheKey(Type type, params object[] args)
        {
            var sb = new StringBuilder(type.ToString());
            if (args != null)
            {
                foreach (var arg in args)
                {
                    if (arg == null)
                    {
                        sb.Append("-null");
                        continue;
                    }
                    if (!(arg is string) && arg is IEnumerable arrTmp)
                    {
                        foreach (var item in arrTmp)
                            sb.AppendFormat("_{0}", item);
                    }
                    else
                        sb.AppendFormat("-{0}", arg);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取指定属性中字段的Get委托
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propOrFieldName"></param>
        /// <returns></returns>
        public static IGetter GetGetter(Type type, string propOrFieldName)
        {
            var key = GetCacheKey(type, propOrFieldName);
            if (!GetterCache.TryGetValue(key, out var ret))
            {
                BindingFlags flags = BindingFlags.Static | BindingFlags.Instance |
                                     BindingFlags.Public | BindingFlags.NonPublic;
                var prop = type.GetProperty(propOrFieldName, flags | BindingFlags.GetProperty);
                if (prop != null)
                {
                    ret = PropertyGetSetHelper.CreateGetter(prop);
                }
                else
                {
                    var att = type.GetField(propOrFieldName, flags | BindingFlags.GetField);
                    if (att != null)
                    {
                        ret = PropertyGetSetHelper.CreateGetter(att);
                    }
                }

                if (ret == null)
                    throw new KeyNotFoundException("Property或Field未找到: " + propOrFieldName);

                GetterCache.TryAdd(key, ret);
            }

            return ret;
        }

        /// <summary>
        /// 获取指定属性中字段的Set委托
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propOrFieldName"></param>
        /// <returns></returns>
        public static ISetter GetSetter(Type type, string propOrFieldName)
        {
            var key = GetCacheKey(type, propOrFieldName);
            if (!SetterCache.TryGetValue(key, out var ret))
            {
                BindingFlags flags = BindingFlags.Static | BindingFlags.Instance |
                                     BindingFlags.Public | BindingFlags.NonPublic;
                var prop = type.GetProperty(propOrFieldName, flags | BindingFlags.GetProperty);
                if (prop != null)
                {
                    ret = PropertyGetSetHelper.CreateSetter(prop);
                }
                else
                {
                    var att = type.GetField(propOrFieldName, flags | BindingFlags.GetField);
                    if (att != null)
                    {
                        ret = PropertyGetSetHelper.CreateSetter(att);
                    }
                }

                if (ret == null)
                    throw new KeyNotFoundException("Property或Field未找到: " + propOrFieldName);

                SetterCache.TryAdd(key, ret);
            }

            return ret;
        }

        public static FastMethodInfo GetDelegate(Type type, string methodName, params Type[] paraTypes)
        {
            var arr = new object[] { methodName };
            if (paraTypes != null)
                arr = arr.Concat(paraTypes).ToArray();
            var key = GetCacheKey(type, arr);
            if (!DelegateCache.TryGetValue(key, out var ret))
            {
                ret = MethodHelper.GetMethodDelegate(type, methodName, paraTypes);
                DelegateCache.TryAdd(key, ret);
            }

            return ret;
        }


        public static MethodInfo GetMethodInfo(Type type, string methodName, params Type[] paraTypes)
        {
            var arr = new object[] {methodName};
            if (paraTypes != null)
                arr = arr.Concat(paraTypes).ToArray();
            var key = GetCacheKey(type, arr);
            if (!MethodInfoCache.TryGetValue(key, out var ret))
            {
                ret = MethodHelper.GetMethod(type, methodName, paraTypes);
                MethodInfoCache.TryAdd(key, ret);
            }

            return ret;
        }
    }
}
