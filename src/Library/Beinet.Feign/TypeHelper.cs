using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LinFu.DynamicProxy;

namespace Beinet.Feign
{
    /// <summary>
    /// 类型的辅助方法
    /// </summary>
    internal static class TypeHelper
    {
        static ConcurrentDictionary<Type, List<Attribute>> _typeAtts =
            new ConcurrentDictionary<Type, List<Attribute>>();

        static ConcurrentDictionary<MethodInfo, List<Attribute>> _methodAtts =
            new ConcurrentDictionary<MethodInfo, List<Attribute>>();

        static ConcurrentDictionary<MethodInfo, RequestMappingAttribute> _methodRequestAtts =
            new ConcurrentDictionary<MethodInfo, RequestMappingAttribute>();

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
        /// 缓存并返回指定类的所有特性列表
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static List<Attribute> GetAttributes(MethodInfo method)
        {
            var ret = _methodAtts.GetOrAdd(method, methodInner =>
            {
                var result = methodInner.GetCustomAttributes(false);
                return result.Select(item => (Attribute)item).ToList();
            });
            return ret;
        }

        /// <summary>
        /// 获取类型的指定特性列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        /// <returns></returns>
        public static List<T> GetCustomAttributes<T>(MethodInfo method) where T : Attribute
        {
            var result = GetAttributes(method);

            var ret = new List<T>();
            foreach (var attribute in result)
            {
                if (attribute is T att)
                    ret.Add(att);
            }

            return ret;
        }

        /// <summary>
        /// 返回指定方法的Request请求映射特性对象
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static RequestMappingAttribute GetRequestMappingAttribute(MethodInfo method)
        {
            var ret = _methodRequestAtts.GetOrAdd(method, GetMappingAttribute);
            return ret;
        }

        internal static RequestMappingAttribute GetMappingAttribute(MethodInfo method)
        {
            var methodAtts = GetAttributes(method);
            foreach (var attribute in methodAtts)
            {
                if (attribute is RequestMappingAttribute ret)
                    return ret;

                var type = attribute.GetType();
                var typeName = type.FullName;
                if (typeName == "AttributeRouting.Web.Http.GETAttribute" ||
                    typeName == "AttributeRouting.Web.Http.POSTAttribute" ||
                    typeName == "AttributeRouting.Web.Http.PUTAttribute" ||
                    typeName == "AttributeRouting.Web.Http.DELETEAttribute" ||
                    typeName == "AttributeRouting.Web.Http.HttpRouteAttribute")
                {
                    var prop = type.GetProperty("RouteUrl");
                    var route = Convert.ToString(prop?.GetValue(attribute, null));

                    string httpMethod = "";
                    prop = type.GetProperty("HttpMethods");
                    var arr = prop?.GetValue(attribute, null);
                    if (arr != null && arr is string[] mArr && mArr.Length > 0)
                        httpMethod = mArr[0];
                    return new RequestMappingAttribute(route, httpMethod);
                }
            }

            return new RequestMappingAttribute("");
        }

        /// <summary>
        /// 返回指定类型的默认值
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static object GetDefaultValue(Type t)
        {
            if (t.IsValueType)
                return Activator.CreateInstance(t);

            return null;
        }
    }
}