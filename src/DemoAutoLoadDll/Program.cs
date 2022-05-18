using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Beinet.Core.StringExt;
using NLog;

namespace DemoAutoLoadDll
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 注意，在Main方法里不能使用任何外部dll的方法，否则不会执行 CurrentDomain_AssemblyResolve
            // 因为程序会在执行方法前，就去加载该方法依赖的所有dll（不包含子方法）
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            RealMain();
        }

        static void RealMain()
        {
            ILogger log = LogManager.GetCurrentClassLogger();
            var str = StringHelper.GetRndStrWithScope(50);
            log.Info(str);
            Console.WriteLine();
            Console.WriteLine("Main结果:" + str);
            Console.Read();
        }

        private static ConcurrentDictionary<string, Assembly> _assemblyCache =
            new ConcurrentDictionary<string, Assembly>();

        // 注：只有exe所在目录下，找不到dll，才会执行这个方法
        // 所以，要先删除exe所在目录下的dll
        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Console.WriteLine("准备加载:" + args.Name);

            var idx = args.Name.IndexOf(',');
            string dllName = idx > 0 ? args.Name.Substring(0, idx) : args.Name.Replace(".dll", "");
            if (dllName.EndsWith(".resources"))
            {
                return null;
            }

            // 加入缓存
            return _assemblyCache.GetOrAdd(dllName, key =>
            {
                // 这里可以遍历所有目录，找到最新版本的目录，以达到重启程序，自动升级的目的
                var baseDir = FindNewlyDir(AppDomain.CurrentDomain.BaseDirectory);
                if (string.IsNullOrEmpty(baseDir))
                {
                    Console.WriteLine(baseDir + "目录没找到");
                    return null;
                }

                string file = Path.Combine(baseDir, dllName + ".dll");
                if (!File.Exists(file))
                {
                    Console.WriteLine(file + "不存在");
                    return null;
                }

                byte[] buff = File.ReadAllBytes(file);
                Console.WriteLine(file + " 加载成功");
                return Assembly.Load(buff);
            });
        }

        static string FindNewlyDir(string dir)
        {
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
                return "";

            // 按目录名正序排序
            versionDirs.Sort(CompareTo);

            // 然后返回最后一个目录
            return versionDirs[versionDirs.Count - 1];
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