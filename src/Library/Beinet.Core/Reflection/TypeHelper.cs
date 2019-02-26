using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Beinet.Core.Reflection
{
    /// <summary>
    /// 反射类型相关的辅助类
    /// </summary>
    public static class TypeHelper
    {
        #region 静态字段，用于缓存收集到的数据
        
        /// <summary>
        /// 当前执行的程序集
        /// </summary>
        private static Assembly nowAssembly = Assembly.GetExecutingAssembly();

        /// <summary>
        /// 缓存收集到的反射类型，避免重复反射
        /// </summary>
        private static readonly Dictionary<string, Type> _arrTypes = new Dictionary<string, Type>();
        /// <summary>
        /// 缓存收集到的反射方法
        /// </summary>
        private static readonly Dictionary<string, object[]> _arrMethods = new Dictionary<string, object[]>();
        /// <summary>
        /// 缓存收集到的反射属性或字段，避免重复反射
        /// </summary>
        private static readonly Dictionary<string, object> _arrAtts = new Dictionary<string, object>();
        /// <summary>
        /// 缓存收集到的命名空间,避免重复反射
        /// </summary>
        private static readonly Dictionary<string, List<Type>> _arrNameSpace = new Dictionary<string, List<Type>>();
     
        #endregion

        #region 类型相关

        /// <summary>
        /// 根据字符串，获取对应的类型返回.
        /// </summary>
        /// <param name="typeFullName">举例：Beinet.Core.IRunable, Beinet.Core</param>
        /// <returns></returns>
        public static Type GetType(string typeFullName)
        {
            var type = (typeFullName ?? "").Trim();
            if (type.Length == 0)
            {
                return null;
            }
            Type ret;
            lock (_arrTypes)
            {
                if (!_arrTypes.TryGetValue(type, out ret))
                {
                    ret = GetRealType(type);
                    _arrTypes[type] = ret;
                }
            }
            return ret;
        }

        /// <summary>
        /// 根据字符串，获取对应的类型返回.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private static Type GetRealType(string info)
        {
            var arrInfo = info.Split(',').Select(x => x.Trim()).ToArray();
            string assName = null;
            Type result = null;
            if (arrInfo.Length == 1)
            {
                //尝试从运行中的程序集获取
                result = nowAssembly.GetType(info.Trim());
                if (result != null)
                {
                    return result;
                }
                var idx = info.LastIndexOf('.');
                if (idx > 0)
                {
                    assName = info.Substring(0, idx);
                }
            }
            else if (arrInfo.Length == 2)
            {
                assName = arrInfo[1];
            }
            if (!string.IsNullOrEmpty(assName))
            {
                var ass = GetAssembly(assName);
                if (ass == null)
                {
                    return null;
                }
                result = ass.GetType(arrInfo[0]);
            }
            return result;
        }


        /// <summary>
        /// 创建指定类型的实例
        /// </summary>
        /// <param name="typeFullName">举例：Beinet.Core.IRunable, Beinet.Core</param>
        /// <param name="args">实例构造函数的参数</param>
        /// <returns></returns>
        public static object CreateInstance(string typeFullName, params object[] args)
        {
            var objType = GetType(typeFullName);
            return CreateInstance(objType, args);
        }

        /// <summary>
        /// 创建指定类型的实例
        /// </summary>
        /// <param name="type"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object CreateInstance(Type type, params object[] args)
        {
            if (type == null)
            {
                return null;
            }
            var obj = type.Assembly.CreateInstance(type.FullName ?? "", false,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, args, null, null);
            return obj;
        }

        /// <summary>
        /// 判断subType是否从parentType继承。
        /// parentType可以是未指定泛型接口类型，如 Foo&lt;&gt;
        /// </summary>
        /// <param name="subType"></param>
        /// <param name="parentType"></param>
        /// <returns></returns>
        public static bool IsSubType(Type subType, Type parentType)
        {
            if (subType == null || parentType == null)
            {
                throw new ArgumentException("Argument can't be null.");
            }
            if (subType == parentType)
            {
                return false;
            }
            var typeObj = typeof(Object);
            if (typeObj == parentType)
            {
                return true;
            }

            // 检查这种类型 class subType : parentType
            if (parentType.IsAssignableFrom(subType))// 这个支持interface和class
            // if(subType.IsSubclassOf(parentType))  // 这个只支持 class
            {
                return true;
            }
            // 如果是 typeof(xxx<>)时
            if (parentType.IsGenericTypeDefinition)
            {
                if (parentType.IsInterface)
                {
                    // 检查这种实现接口的 class xxx : Iyyy<zzz>
                    if (subType.GetInterfaces().Any(tp =>
                        tp.IsGenericType && tp.GetGenericTypeDefinition() == parentType))
                    {
                        return true;
                    }
                }
                else
                {
                    // 检查这种实现父类的 class xxx : yyy<zzz>
                    while (subType != null && subType != typeObj)
                    {
                        if (subType.IsGenericType && subType.GetGenericTypeDefinition() == parentType)
                        {
                            return true;
                        }
                        subType = subType.BaseType;
                    }
                }
            }
            return false;
        }
        #endregion



        #region 属性字段相关
        /// <summary>
        /// 获取类的静态属性或字段值
        /// </summary>
        /// <param name="type"></param>
        /// <param name="attName"></param>
        /// <returns></returns>
        public static object GetAtt(string type, string attName)
        {
            var objtype = GetType(type);
            if (objtype == null)
            {
                return null;
            }
            return GetAtt(objtype, attName);
        }

        /// <summary>
        /// 获取类的静态属性或字段值
        /// </summary>
        /// <param name="type"></param>
        /// <param name="attName"></param>
        /// <returns></returns>
        public static object GetAtt(Type type, string attName)
        {
            var key = type.FullName + "-" + attName;
            object attInfo;
            lock(_arrAtts)
                if (!_arrAtts.TryGetValue(key, out attInfo))
                {
                    attInfo = GetRealAtt(type, attName);
                    _arrAtts[key] = attInfo;
                }
            if (attInfo == null)
            {
                return null;
            }
            var prop = attInfo as PropertyInfo;
            if (prop != null)
            {
                return prop.GetValue(null);
            }
            var field = attInfo as FieldInfo;
            if (field != null)
            {
                return field.GetValue(null);
            }
            return null;
        }

        /// <summary>
        /// 获取实例的属性或字段值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="attName"></param>
        /// <returns></returns>
        public static object GetAtt(object obj, string attName)
        {
            var type = obj.GetType();
            var key = type + "-" + attName;
            object attInfo;
            lock (_arrAtts)
                if (!_arrAtts.TryGetValue(key, out attInfo))
                {
                    attInfo = GetRealAtt(type, attName);
                    _arrAtts[key] = attInfo;
                }

            if (attInfo == null)
            {
                return null;
            }
            var prop = attInfo as PropertyInfo;
            if (prop != null)
            {
                return prop.GetValue(obj);
            }
            var field = attInfo as FieldInfo;
            if (field != null)
            {
                return field.GetValue(obj);
            }
            return null;
        }

        /// <summary>
        /// 获取类的静态属性或字段, 返回 PropertyInfo 或 FieldInfo
        /// </summary>
        /// <param name="type"></param>
        /// <param name="attName"></param>
        /// <returns></returns>
        private static object GetRealAtt(Type type, string attName)
        {
            BindingFlags flags = BindingFlags.Static | BindingFlags.Instance | 
                BindingFlags.Public | BindingFlags.NonPublic;
            object att = type.GetProperty(attName, flags | BindingFlags.GetProperty);
            if (att == null)
            {
                att = type.GetField(attName, flags | BindingFlags.GetField);
            }
            return att;
        }


        #endregion



        #region 方法相关


        /// <summary>
        /// 返回数组3个项：MethodInfo、Args Type[]、ParameterInfo[]
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object[] GetMethod(string type, string methodName, params object[] args)
        {
            var objtype = GetType(type);
            if (objtype == null)
            {
                return null;
            }
            return GetMethod(objtype, methodName, args);
        }


        /// <summary>
        /// 返回数组3个项：MethodInfo、Args Type[]、ParameterInfo[]
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object[] GetMethod(Type type, string methodName, params object[] args)
        {
            var key = type + "-" + methodName + "-" + (args?.Length.ToString());
            object[] methodAndType;
            lock (_arrMethods)
                if (!_arrMethods.TryGetValue(key, out methodAndType))
                {
                    methodAndType = GetRealMethod(type, methodName, args);
                    _arrMethods[key] = methodAndType;
                }
            return methodAndType;
        }


        /// <summary>
        /// 返回数组3个项：MethodInfo、Args Type[]、ParameterInfo[]
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static object[] GetRealMethod(Type type, string methodName, params object[] args)
        {
            Type[] types;
            if (args == null)
            {
                types = new Type[0];
            }
            else
            {
                // 定义参数类型，避免重载方法调用导致：发现不明确的匹配
                types = new Type[args.Length];
                int i = 0;
                foreach (object arg in args)
                {
                    types[i] = arg.GetType();
                    i++;
                }
            }

            BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Instance | 
                BindingFlags.Public | BindingFlags.NonPublic;
            var methodTmp = type.GetMethod(methodName, flags, null, types, null);
            ParameterInfo[] arrPara = null;
            if (methodTmp != null)
            {
                arrPara = methodTmp.GetParameters();
            }
            return new object[] { methodTmp, types, arrPara };
        }


        /// <summary>
        /// 执行静态方法
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object ExecuteStaticMethod(string type, string methodName, params object[] args)
        {
            var objtype = GetType(type);
            if (objtype == null)
            {
                return null;
            }
            return ExecuteStaticMethod(objtype, methodName, args);
        }

        /// <summary>
        /// 执行静态方法
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object ExecuteStaticMethod(Type type, string methodName, params object[] args)
        {
            var methodInfo = GetMethod(type, methodName, args);
            if (methodInfo == null)
            {
                return null;
            }
            var method = methodInfo[0] as MethodInfo;
            if (method == null)
            {
                return null;
            }
            // 如果没有传递参数,要根据方法的参数个数，传递null
            if (args == null)
            {
                ParameterInfo[] arrPara = methodInfo[2] as ParameterInfo[];
                if (arrPara != null && arrPara.Length > 0)
                {
                    args = new object[arrPara.Length];
                }
            }
            return method.Invoke(null, args);
        }

        /// <summary>
        /// 执行实例方法
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object ExecuteMethod(object obj, string methodName, params object[] args)
        {
            var methodInfo = GetMethod(obj.GetType(), methodName, args);
            if (methodInfo == null)
            {
                return null;
            }
            var method = methodInfo[0] as MethodInfo;
            if (method == null)
            {
                return null;
            }
            // 如果没有传递参数,要根据方法的参数个数，传递null
            if (args == null)
            {
                ParameterInfo[] arrPara = methodInfo[2] as ParameterInfo[];
                if (arrPara != null && arrPara.Length > 0)
                {
                    args = new object[arrPara.Length];
                }
            }
            return method.Invoke(obj, args);
        }

        #endregion


        /// <summary>
        /// 根据名称查找组件
        /// </summary>
        /// <param name="name">举例：Beinet.Core</param>
        /// <returns></returns>
        public static Assembly GetAssembly(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            var strName = name;
            while (strName.Length > 0)
            {
                try
                {
                    return Assembly.Load(strName);
                }
                catch
                {
                    var idx = strName.LastIndexOf('.');
                    if (idx <= 0)
                    {
                        return null;
                    }
                    strName = strName.Substring(0, idx);
                }
            }
            return null;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <returns></returns>
        public static List<Type> GetNameSpace(string namespaceName)
        {
            namespaceName = (namespaceName ?? "").Trim();
            if (namespaceName.Length == 0)
            {
                return null;
            }
            List<Type> temp;
            lock(_arrNameSpace)
                if (!_arrNameSpace.TryGetValue(namespaceName, out temp))
                {
                    temp = GetRealNameSpace(namespaceName);
                    _arrNameSpace[namespaceName] = temp;
                }
            return temp;
        }

        private static List<Type> GetRealNameSpace(string info)
        {
            var strs = info.Split(',').Select(x => x.Trim()).ToArray();
            string assName;
            if (strs.Length == 1)
            {
                //尝试从运行中的程序集获取
                return nowAssembly.GetTypes().Where(x=>x.Namespace == info.Trim()).ToList();
            }
            else if (strs.Length == 2)
            {
                assName = strs[1];
                var objAssName = new AssemblyName(assName);
                var ass = GetAssembly(assName);
                if (ass == null)
                {
                    return null;
                }
                return ass.GetTypes().Where(x => x.Namespace == strs[0]).ToList();
            }
            return null;
        }


        #region 从exe嵌入资源里加载dll的方法, Web项目不要使用

        private static Assembly exeAss = Assembly.GetEntryAssembly();
        private static System.Resources.ResourceManager resManager;
        private static System.Resources.ResourceManager ResManager
        {
            get
            {
                if (resManager == null)
                {
                    var entryNamespace = exeAss.GetTypes()[0].Namespace;
                    resManager = new System.Resources.ResourceManager(entryNamespace + ".Properties.Resources",
                        Assembly.GetExecutingAssembly());
                }
                return resManager;
            }
        }

        /// <summary>
        /// 从嵌入的资源文件里加载dll,调用方法：在Main函数里执行：
        /// AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // System.Reflection.RuntimeAssembly exeAss;  exeAss.GetManifestResourceNames()
            var idx = args.Name.IndexOf(',');
            string dllName = idx > 0 ? args.Name.Substring(0, idx) : args.Name.Replace(".dll", "");
            if (dllName.EndsWith(".resources"))
            {
                return null;
            }

            // 读取那些 生成操作为“嵌入的资源”的数据
            string resourceName = $"{exeAss.GetTypes()[0].Namespace}.{dllName}.dll";
            using (var stream = exeAss.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    var len = (int)stream.Length;
                    var assemblyData = new byte[len];
                    if (stream.Read(assemblyData, 0, len) == len)
                    {
                        return Assembly.Load(assemblyData);
                    }
                }
            }

            // 读取那些 嵌入在“Resources.resx”里的数据
            dllName = dllName.Replace(".", "_");
            try
            {
                var bytes = (byte[])ResManager.GetObject(dllName);
                if (bytes == null)
                {
                    return null;
                }
                return Assembly.Load(bytes);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

    }
}
