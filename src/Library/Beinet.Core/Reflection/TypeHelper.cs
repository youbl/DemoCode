using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Beinet.Core.Util;

namespace Beinet.Core.Reflection
{
    /// <summary>
    /// 反射类型相关的辅助类
    /// </summary>
    public static class TypeHelper
    {
        /// <summary>
        /// 扫描dll时，除了当前目录，还要扫描其它哪些目录？
        /// </summary>
        public static List<string> AdditionalAssemblyDir { get; } = new List<string>();

        /// <summary>
        /// 配置里写的扫描目录
        /// </summary>
        public const string ConfigDirName = "BeinetCore.AssemblyDirs";
        /// <summary>
        /// 最新版本目录配置占位符
        /// </summary>
        public const string ConfigDirNewlySymbol = "{NewVersionDir}";

        #region 静态字段，用于缓存收集到的数据

        /// <summary>
        /// 当前执行的程序集
        /// </summary>
        private static Assembly nowAssembly = Assembly.GetExecutingAssembly();

        private static Dictionary<string, Assembly> _arrAssemblys;

        /// <summary>
        /// 当前项目的所有程序集
        /// </summary>
        public static Dictionary<string, Assembly> Assemblys => ScanAssembly();

        /// <summary>
        /// 缓存收集到的反射类型，避免重复反射
        /// </summary>
        private static readonly Dictionary<string, Type> _arrTypes = new Dictionary<string, Type>();

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
                result = nowAssembly.GetType(arrInfo[0]);
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
        /// parentType可以是未指定泛型的接口类型，如 Foo&lt;&gt;
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
            if (parentType.IsAssignableFrom(subType)) // 这个支持interface和class
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

        /// <summary>
        /// 从程序集里，安全的返回类型清单
        /// </summary>
        /// <param name="assembly">The <see cref="System.Reflection.Assembly"/> from which to load types.</param>
        /// <returns>
        /// The set of types from the <paramref name="assembly" />, or the subset
        /// of types that could be loaded if there was any error.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="assembly" /> is <see langword="null" />.
        /// </exception>
        public static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            // Algorithm from StackOverflow answer here:
            // https://stackoverflow.com/questions/7889228/how-to-prevent-reflectiontypeloadexception-when-calling-assembly-gettypes
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            try
            {
                return assembly.DefinedTypes.Select(t => t.AsType());
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }

        #endregion


        #region 程序集相关

        /// <summary>
        /// 加载全部程序集
        /// </summary>
        /// <returns></returns>
        static Dictionary<string, Assembly> ScanAssembly()
        {
            if (_arrAssemblys != null)
                return _arrAssemblys;

            _arrAssemblys = new Dictionary<string, Assembly>();

            // 加入当前入口程序集
            var nowAss = Assembly.GetEntryAssembly();
            if (nowAss != null)
            {
                var key = nowAss.GetName().Name;
                if (!_arrAssemblys.ContainsKey(key))
                {
                    _arrAssemblys.Add(key, nowAss);
                }
            }

            // 加载当前目录 和 外部指定目录的扫描列表
            foreach (var otherDir in GetScanDirs())
            {
                if (string.IsNullOrEmpty(otherDir) || !Directory.Exists(otherDir))
                    continue;

                var tmp = ScanAssembly(otherDir);
                foreach (var item in tmp)
                {
                    if (!_arrAssemblys.ContainsKey(item.Key))
                    {
                        _arrAssemblys.Add(item.Key, item.Value);
                    }
                }
            }

            return _arrAssemblys;
        }

        static Dictionary<string, Assembly> ScanAssembly(string dir, string regex = null)
        {
            var arrAssembly = new Dictionary<string, Assembly>();

            if (!Directory.Exists(dir))
            {
                return arrAssembly;
            }

            Regex regObj = null;
            if (!string.IsNullOrEmpty(regex))
                regObj = new Regex(regex);
            var files = Directory.GetFiles(dir, "*.dll", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                LoadAssembly(file, regObj, arrAssembly);
            }

            return arrAssembly;
        }

        private static void LoadAssembly(string file, Regex regObj, Dictionary<string, Assembly> arrAssembly)
        {
            var assemblyString = Path.GetFileNameWithoutExtension(file);
            if (string.IsNullOrEmpty(assemblyString) || (regObj != null && !regObj.IsMatch(assemblyString)))
                return;

            if (arrAssembly.ContainsKey(assemblyString))
                return;

            try
            {
                var ass = Assembly.Load(assemblyString);
                if (ass == null)
                {
                    return;
                }

                arrAssembly.Add(assemblyString, ass);
            }
            catch
            {
                // ignored
            }
        }


        /// <summary>
        /// 根据名称查找组件
        /// </summary>
        /// <param name="name">举例：Beinet.Tool.Core</param>
        /// <returns></returns>
        public static Assembly GetAssembly(string name)
        {
            Assembly ret = null;
            if (!string.IsNullOrEmpty(name))
            {
                var strName = name;
                while (ret == null && strName.Length > 0)
                {
                    if (!Assemblys.TryGetValue(strName, out ret))
                    {
                        var idx = strName.LastIndexOf('.');
                        strName = idx > 0 ? strName.Substring(0, idx) : "";
                    }
                }
            }

            return ret;
        }

        #endregion


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
            lock (_arrNameSpace)
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
            string assName = null;
            if (strs.Length == 1)
            {
                //尝试从运行中的程序集获取
                var ret = GetLoadableTypes(nowAssembly).Where(x => x.Namespace == strs[0]).ToList();
                if (ret.Count > 0)
                    return ret;

                assName = strs[0];
            }

            if (strs.Length == 2)
            {
                assName = strs[1];
            }

            if (!string.IsNullOrEmpty(assName))
            {
                var ass = GetAssembly(assName);
                if (ass == null)
                    throw new Exception("指定的程序集未载到：" + assName);
                return GetLoadableTypes(ass).Where(x => x.Namespace == strs[0]).ToList();
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
                    var len = (int) stream.Length;
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
                var bytes = (byte[]) ResManager.GetObject(dllName);
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

        private static HashSet<string> GetScanDirs()
        {
            var ret = new HashSet<string>();

            {
                // 添加当前目录；如果是web, 添加bin目录
                var dir = Env.Dir;
                if (Env.IsWebApp)
                    dir = Path.Combine(Env.Dir, "bin");
                dir = Path.GetFullPath(dir);
                ret.Add(dir);
            }

            // 添加外部指定的其它目录
            foreach (var subDir in AdditionalAssemblyDir)
            {
                var tmpDir = Path.GetFullPath(subDir);
                ret.Add(tmpDir);
            }

            // 添加配置的目录
            foreach (var configDir in GetConfigDirs())
            {
                var tmpDir = Path.GetFullPath(configDir);
                ret.Add(tmpDir);
            }

            return ret;
        }

        private static List<string> GetConfigDirs()
        {
            var configDirs = ConfigHelper.GetSetting(ConfigDirName);
            if (string.IsNullOrEmpty(configDirs))
            {
                return new List<string>();
            }

            var ret = new List<string>();
            foreach (var item in configDirs.Split(',', ';'))
            {
                var configDir = item.Trim();
                if (configDir.Length == 0)
                    continue;

                // 查找最新版本目录
                if (configDir == ConfigDirNewlySymbol)
                {
                    var newlyDir = FindNewlyDir(Env.Dir);
                    if (!string.IsNullOrEmpty(newlyDir))
                        ret.Add(newlyDir);
                    continue;
                }

                if (configDir.Contains(':'))
                {
                    // 有冒号表示绝对路径
                    ret.Add(configDir);
                    continue;
                }

                ret.Add(Path.Combine(Env.Dir, configDir));
            }

            return ret;
        }

        /// <summary>
        /// 在指定目录下，查找版本号最新的一个目录
        /// </summary>
        /// <param name="topDir"></param>
        /// <returns></returns>
        static string FindNewlyDir(string topDir)
        {
            var dir = topDir;
            var subdirs = Directory.GetDirectories(dir);
            // Array.Sort(subdirs, CompareTo);

            // 只收集版本号目录，如1.2.3.4这种目录名
            var versionDirs = new List<string>();
            foreach (var item in subdirs)
            {
                var subdirName = Path.GetFileName(item);
                if (!string.IsNullOrEmpty(subdirName) && Regex.IsMatch(subdirName, @"^(\d+\.)*\d+$"))
                {
                    versionDirs.Add(item);
                }
            }

            if (versionDirs.Count <= 0)
            {
                return "";
            }

            // 按目录名正序排序
            versionDirs.Sort(CompareTo);

            // 然后返回最后一个目录
            var ret = versionDirs[versionDirs.Count - 1];
            return ret;
        }

        private static int CompareTo(string version1, string version2)
        {
            if (version1 == null || version2 == null)
                throw new Exception("对比数据不能为空");

            var sourceArr = version1.Split('.');
            var targetArr = version2.Split('.');

            // 4段版本数字
            for (var i = 0; i < 4; i++)
            {
                var source = GetSegVer(sourceArr, i);
                var target = GetSegVer(targetArr, i);
                if (source != target)
                    return source - target;
            }

            return 0;
        }

        private static int GetSegVer(string[] verArr, int idx)
        {
            if (verArr.Length > idx && int.TryParse(verArr[idx], out var ret))
                return ret;
            return 0;
        }
    }
}