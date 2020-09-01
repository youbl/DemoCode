using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Beinet.Core.Reflection;

namespace Beinet.Core.Cron
{
    /// <summary>
    /// 扫描所有dll，查找有ScheduledAttribute定义的方法，并进行定时处理
    /// </summary>
    public static class ScheduledWorker
    {
        private static object _lockObj = new object();
        private static bool _runed;
        private static Timer _timer;

        static ScheduledWorker()
        {
            LoadNLogger();
        }

        /// <summary>
        /// 扫描计划任务，并启动定时执行
        /// </summary>
        public static int StartAllScheduled()
        {
            lock (_lockObj)
            {
                if (_runed)
                {
                    return 0;
                }

                _runed = true;
            }

            Info("Begin search scheduled method...");
            var allMethod = ScanAllAssembly();
            var cnt = allMethod.Count;
            Info($"End search scheduled method, count: {cnt.ToString()}.");

            if (cnt > 0)
            {
//                foreach (var item in allMethod)
//                {
//                    Info(item.Scheduled.Cron);
//                }
                _timer = new Timer(ScheduledRun, allMethod, 0, 1000);
                Info($"Timer inited ok: {_timer}");
            }

            return cnt;
        }

        /// <summary>
        /// 扫描所有程序集，查找有ScheduledAttribute标记的方法，并返回
        /// </summary>
        /// <returns>计划任务清单</returns>
        private static List<ScheduledMethod> ScanAllAssembly()
        {
            var arrAssembly = TypeHelper.Assemblys;

            var ret = new List<ScheduledMethod>();
            foreach (var assembly in arrAssembly.Values)
            {
                var types = TypeHelper.GetLoadableTypes(assembly);
                foreach (var exportedType in types)
                {
                    object typeObj = null;
                    foreach (var methodInfo in exportedType.GetMethods(
                        BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        var att = methodInfo.GetCustomAttribute<ScheduledAttribute>();
                        if (att == null || !att.Run)
                        {
                            continue;
                        }

                        if (methodInfo.GetParameters().Length > 0)
                        {
                            throw new Exception($"Scheduled job can't have parameter: {methodInfo.Name}");
                        }

                        if (!methodInfo.IsStatic && typeObj == null)
                        {
                            try
                            {
                                typeObj = Activator.CreateInstance(exportedType);
                            }
                            catch (Exception exp)
                            {
                                Error($"{exportedType.FullName} : {exp}");
                                continue;
                            }
                        }

                        var scheduledMethod = new ScheduledMethod
                        {
                            Method = methodInfo,
                            Scheduled = att,
                            MethodObj = typeObj
                        };
                        ret.Add(scheduledMethod);
                    }
                }
            }

            return ret;
        }

        private static void ScheduledRun(object state)
        {
            if (!(state is List<ScheduledMethod> arrMethods) || arrMethods.Count <= 0)
            {
                return;
            }

            var now = DateTime.Now;
            foreach (var method in arrMethods)
            {
                if (method.Scheduled.IsRunTime(now))
                {
                    ThreadPool.UnsafeQueueUserWorkItem(state2 =>
                    {
                        try
                        {
                            method.Run();
                        }
                        catch (Exception exp)
                        {
                            var msg = $"method:{method.Method.Name}: {exp}";
                            Error(msg);
                        }
                    }, null);
                }
            }
        }

        /// <summary>
        /// 定时任务类
        /// </summary>
        private class ScheduledMethod
        {
            /// <summary>
            /// 定时任务要执行的方法
            /// </summary>
            public MethodInfo Method { get; set; }
            /// <summary>
            /// 方法为非静态时，所依附的对象
            /// </summary>
            public object MethodObj { get; set; }
            /// <summary>
            /// 定时任务配置
            /// </summary>
            public ScheduledAttribute Scheduled { get; set; }

            /// <summary>
            /// 执行方法
            /// </summary>
            public void Run()
            {
                Method.Invoke(MethodObj, null);
            }
        }

        #region 日志相关方法

        private static object _logger;
        private static MethodInfo _infoMethod;
        private static MethodInfo _errorMethod;

        // 记录Info日志
        private static void Info(string msg)
        {
            if (_infoMethod == null)
                return;
            _infoMethod.Invoke(_logger, new object[] { ExtendMsg(msg) });
        }
        // 记录Error日志
        private static void Error(string msg)
        {
            if (_errorMethod == null)
                return;
            _errorMethod.Invoke(_logger, new object[] { ExtendMsg(msg) });
        }

        private static string ExtendMsg(string msg)
        {
            return msg;
        }

        /// <summary>
        /// 执行 NLog.LogManager.GetCurrentClassLogger(typeof(ScheduledSearch))，
        /// 以返回日志对象，用于在类库未添加NLog引用，但是可执行项目有
        /// </summary>
        /// <returns></returns>
        private static void LoadNLogger()
        {
            try
            {
                var ass = Assembly.Load("NLog");
                if (ass == null)
                {
                    return;
                }

                var logManagerType = ass.GetType("NLog.LogManager");
                if (logManagerType == null)
                {
                    return;
                }

                var method = logManagerType.GetMethod("GetCurrentClassLogger", new Type[] { typeof(Type) });
                if (method == null)
                {
                    return;
                }

                _logger = method.Invoke(null, new object[] { typeof(ScheduledWorker) });
                if (_logger == null)
                {
                    return;
                }
                var type = _logger.GetType();
                foreach (var info in type.GetMethods())
                {
                    if (!info.IsGenericMethod || info.GetParameters().Length != 1)
                    {
                        continue;
                    }
                    if (info.Name == "Info")
                    {
                        // 直接用 _infoMethod = info; 会报错： 不能对ContainsGenericParameters为true的类型或方法执行后期绑定操作
                        _infoMethod = info.MakeGenericMethod(typeof(string));
                    }
                    else if (info.Name == "Error")
                    {
                        _errorMethod = info.MakeGenericMethod(typeof(string));
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        #endregion
    }
}