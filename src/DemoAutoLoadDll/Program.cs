using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
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
            var str = StringHelper.GetRndStrWithScope(123);
            log.Info(str);
            Console.WriteLine("Main结果:" + str);
            Console.Read();
        }

        private static ConcurrentDictionary<string, Assembly> _assemblyCache =
            new ConcurrentDictionary<string, Assembly>();

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
                string file = AppDomain.CurrentDomain.BaseDirectory + "\\lib1\\" + dllName + ".dll";
                if (!File.Exists(file))
                {
                    Console.WriteLine(file + "不存在");
                    return null;
                }

                byte[] buff = File.ReadAllBytes(file);
                return Assembly.Load(buff);
            });
        }
    }
}