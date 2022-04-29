using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Beinet.Core.Reflection
{
    /// <summary>
    /// 扫描工具类
    /// </summary>
    public static class Scanner
    {
        /// <summary>
        /// 根据父类型（class或interface），查找所有子类.
        /// </summary>
        /// <param name="parentType">父类型（class或interface）</param>
        /// <returns>子类列表(不含接口和虚拟类)</returns>
        public static List<Type> ScanByParentType(Type parentType)
        {
            var ret = new List<Type>();

            var arrAssembly = TypeHelper.Assemblys;
            foreach (var assembly in arrAssembly.Values)
            {
                var types = TypeHelper.GetLoadableTypes(assembly);
                var subTypes = types.Where(item =>
                    parentType != item &&
                    parentType.IsAssignableFrom(item) &&
                    !item.IsInterface &&
                    !item.IsAbstract);

                ret.AddRange(subTypes);
            }

            return ret;
        }

        /// <summary>
        /// 根据父类型（class或interface），查找所有子类，并创建实例返回.
        /// 注：必须具体无参构造函数
        /// </summary>
        /// <param name="parentType">父类型（class或interface）</param>
        /// <returns>子类实例列表</returns>
        public static List<object> ScanInstanceByParentType(Type parentType)
        {
            var ret = new List<object>();
            var constructParams = new Type[] { };

            var subTypes = ScanByParentType(parentType);
            foreach (var type in subTypes)
            {
                if (type.GetConstructor(constructParams) == null)
                {
                    continue;
                }

                ret.Add(Activator.CreateInstance(type));
            }

            return ret;
        }

        /// <summary>
        /// 根据特性，查找所有加了该特性的class列表.
        /// 不含接口和虚拟类
        /// </summary>
        /// <param name="attributeType">Attribute类型</param>
        /// <returns></returns>
        public static List<AttriteResult> ScanClassByAttribute(Type attributeType)
        {
            var ret = new List<AttriteResult>();

            var arrAssembly = TypeHelper.Assemblys;
            foreach (var assembly in arrAssembly.Values)
            {
                var types = TypeHelper.GetLoadableTypes(assembly);
                foreach (var type in types)
                {
                    if (type.IsInterface || type.IsAbstract)
                        continue;

                    var att = type.GetCustomAttribute(attributeType);
                    if (att == null)
                        continue;

                    var item = new AttriteResult
                    {
                        Attr = att,
                        Type = type,
                    };
                    ret.Add(item);
                }
            }

            return ret;
        }

        /// <summary>
        /// 根据特性，查找所有加了该特性的Method列表
        /// </summary>
        /// <param name="attributeType">Attribute类型</param>
        /// <returns></returns>
        public static List<AttriteResult> ScanMethodByAttribute(Type attributeType)
        {
            var ret = new List<AttriteResult>();

            var arrAssembly = TypeHelper.Assemblys;
            foreach (var assembly in arrAssembly.Values)
            {
                var types = TypeHelper.GetLoadableTypes(assembly);
                foreach (var type in types)
                {
                    if (type.IsInterface || type.IsAbstract)
                        continue;

                    var methods = type.GetMethods(BindingFlags.Static |
                                                  BindingFlags.Instance |
                                                  BindingFlags.Public |
                                                  BindingFlags.NonPublic);
                    foreach (var method in methods)
                    {
                        var att = method.GetCustomAttribute(attributeType);
                        if (att == null)
                            continue;

                        var item = new AttriteResult
                        {
                            Attr = att,
                            Type = type,
                            Method = method,
                        };
                        ret.Add(item);
                    }
                }
            }

            return ret;
        }
    }

    /// <summary>
    /// 根据Attribute扫描到的结果
    /// </summary>
    public class AttriteResult
    {
        /// <summary>
        /// 扫描到的注解对象
        /// </summary>
        public Attribute Attr { get; set; }

        /// <summary>
        /// 扫描到的类型信息
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// 扫描到的方法信息
        /// </summary>
        public MethodInfo Method { get; set; }
    }
}