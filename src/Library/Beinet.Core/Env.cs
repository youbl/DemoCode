using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Web;
using Beinet.Core.Reflection;
using Beinet.Core.Util;

namespace Beinet.Core
{
    /// <summary>
    /// 一些环境变更和上下文变量
    /// </summary>
    public class Env
    {
        /// <summary>
        /// 程序启动目录
        /// </summary>
        public static readonly string Dir = System.AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// 用于业务层BeginInvoke之类的异步，读取不到数据的处理
        /// </summary>
        private static readonly AsyncLocal<Dictionary<string, string>> asyncLocalTrace =
            new AsyncLocal<Dictionary<string, string>>();

        /// <summary>
        /// HttpContext.Current.Items["$__ENV"], KeyValue结构的上下文字典类
        /// </summary>
        public const string CONTEXT_CONFIG_NAME = "$__ENV";

        /// <summary>
        /// 返回当前应用程序名，统一在App.Config里配置
        /// </summary>
        public static string AppName { get; internal set; } = ConfigHelper.GetSetting("application.name");


        #region 读取jenkins推送的env.config

        private static readonly string _jenkinsEnvFile = Path.Combine(Dir, "App_Data/env.config");
        private static string _profile;

        /// <summary>
        /// 获取当前活动的配置文件, 配置取值示例：
        /// 海外： i18n-dev i18n-test i18n-preview uk-product us-product
        /// 国内： dev test preview product
        /// </summary>
        public static string ProfileActive
        {
            get
            {
                if (_profile == null)
                {
                    ArrEnvConfig.TryGetValue("profiles.active", out _profile);
                    if (_profile == null)
                    {
                        _profile = "";
                    }
                }
                return _profile;
            }
            set { _profile = value; }
        }

        private static Dictionary<string, string> _arrEnvConfig;

        private static Dictionary<string, string> ArrEnvConfig
        {
            get
            {
                if (_arrEnvConfig == null)
                {
                    var ret = new Dictionary<string, string>();
                    if (File.Exists(_jenkinsEnvFile))
                    {
                        var arrContent = FileHelper.Read(_jenkinsEnvFile).Split('\r', '\n');
                        foreach (var item in arrContent)
                        {
                            var idx = item.IndexOf('=');
                            if (idx <= 0 || idx >= item.Length - 1)
                                continue;
                            var key = item.Substring(0, idx).Trim();
                            if (key.Length == 0)
                                continue;
                            var val = item.Substring(idx + 1).Trim();
                            if (val.Length == 0)
                                continue;
                            ret[key] = val;
                        }

                    }
                    _arrEnvConfig = ret;
                }
                return _arrEnvConfig;
            }
        }
        #endregion



        #region 上下文相关

        private static IDictionary ContextArr => HttpContext.Current?.Items;

        /// <summary>
        /// 读取Http上下文里的配置数据
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetContext(string name, string defaultValue = "")
        {
            if (ContextArr != null)
            {
                if (!(ContextArr[CONTEXT_CONFIG_NAME] is Dictionary<string, string> arr))
                    return defaultValue;
                //lock (arr)
                {
                    if (arr.TryGetValue(name, out var ret))
                        return ret;
                }
            }

            if (asyncLocalTrace == null)
                return defaultValue;
            // 避免异步线程读取不到Context
            var arrAsync = asyncLocalTrace.Value;
            //lock (arr)
            {
                if (arrAsync.TryGetValue(name, out var ret))
                    return ret;
            }
            return defaultValue;
        }

        /// <summary>
        /// 读取Http上下文里的配置数据
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryGetContext(string name, out string value)
        {
            if (ContextArr != null)
            {
                if (!(ContextArr[CONTEXT_CONFIG_NAME] is Dictionary<string, string> arr))
                {
                    value = "";
                    return false;
                }
                //lock (arr)
                {
                    if (arr.TryGetValue(name, out value))
                    {
                        // value += "/context";
                        return true;
                    }
                }
            }

            if (asyncLocalTrace == null)
            {
                value = "";
                return false;
            }
            // 避免异步线程读取不到Context
            var arrAsync = asyncLocalTrace.Value;
            if (arrAsync == null)
            {
                value = "";
                return false;
            }
            //lock (arr)
            {
                if (arrAsync.TryGetValue(name, out value))
                {
                    // value += "/async";
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 设置Http上下文里的配置数据
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public static void SetContext(string name, string value)
        {
            CheckContext();
            lock (ContextArr)
            {
                if (!(ContextArr[CONTEXT_CONFIG_NAME] is Dictionary<string, string> arrContext))
                {
                    arrContext = new Dictionary<string, string>();
                    ContextArr[CONTEXT_CONFIG_NAME] = arrContext;
                }
                if (value == null)
                    arrContext.Remove(name);
                else
                    arrContext[name] = value;
            }
            var arr = asyncLocalTrace.Value;
            lock (arr)
            {
                if (value == null)
                    arr.Remove(name);
                else
                    arr[name] = value;
            }
        }

        static void CheckContext()
        {
            if (ContextArr == null)
                throw new Exception("Context相关方法只能在Web项目中使用");
            if (asyncLocalTrace.Value == null)
                asyncLocalTrace.Value = new Dictionary<string, string>();
        }

        #endregion


        #region IP环境相关


        /// <summary>
        /// 返回服务器IP列表, 多个IP以分号分隔
        /// </summary>
        public static string ServerIPList { get; } = IpHelper.GetServerIpList();

        /// <summary>
        /// 返回客户端真实IP，按如下顺序查找并返回其中1个：
        /// HTTP_X_FORWARDED_FOR，
        /// HTTP_X_REAL_IP，
        /// UserHostAddress
        /// </summary>
        public static string ClientIP => IpHelper.GetClientIp();

        /// <summary>
        /// 返回客户端IP列表，包含3个：UserHostAddress;HTTP_X_REAL_IP;HTTP_X_FORWARDED_FOR
        /// </summary>
        public static string ClientIPList => IpHelper.GetClientIpLst();

        #endregion



        #region 开发测试线上环境相关变量
        const string HostName = "BeinetToolHost";
        internal const string DEV = "dev";
        internal const string TEST = "test";
        internal const string PREVIEW = "preview";
        internal const string PRODUCT = "product";

        /// <summary>
        /// 去哪些目录下查找配置文件
        /// </summary>
        private static List<string> ConfigSubPathlist = new List<string>() { "App_Data\\", "" };


        /// <summary>
        /// 获取当前服务器的系统环境变量
        /// </summary>
        public static string Environ { get; } =
            Environment.GetEnvironmentVariable(HostName, EnvironmentVariableTarget.Machine) ?? "";


        /// <summary>
        /// 是否线上环境，系统环境变量为空表示线上环境
        /// </summary>
        public static bool IsOnline { get; } = string.IsNullOrEmpty(Environ);

        /// <summary>
        /// 是否预发环境
        /// </summary>
        public static bool IsPreview { get; } = Environ.Equals(PREVIEW, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// 是否测试环境
        /// </summary>
        public static bool IsTest { get; } = Environ.Equals(TEST, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// 是否开发环境
        /// </summary>
        public static bool IsDev { get; } = !IsOnline && !IsPreview && !IsTest;


        /// <summary>
        /// 根据当前环境，重新组装url返回.
        /// 注：只支持http和https协议
        /// </summary>
        /// <param name="onlineUrl"></param>
        /// <returns></returns>
        internal static string CombineEnvUrl(string onlineUrl)
        {
            onlineUrl = onlineUrl.Trim();
            if (IsOnline)
                return onlineUrl;

            var protocal = "https://";
            var urlNoProtocal = onlineUrl;
            if (onlineUrl.StartsWith(protocal, StringComparison.OrdinalIgnoreCase))
            {
                urlNoProtocal = onlineUrl.Substring(protocal.Length);
            }
            else
            {
                protocal = "http://";
                if (onlineUrl.StartsWith(protocal, StringComparison.OrdinalIgnoreCase))
                    urlNoProtocal = onlineUrl.Substring(protocal.Length);
                else
                    onlineUrl = protocal + onlineUrl;
            }

            // 已经是测试地址或开发地址了
            if (urlNoProtocal.StartsWith(TEST, StringComparison.OrdinalIgnoreCase))
                return onlineUrl;
            if (urlNoProtocal.StartsWith(DEV, StringComparison.OrdinalIgnoreCase))
                return onlineUrl;


            // 预发环境
            if (IsPreview)
            {
                if (urlNoProtocal.StartsWith(PREVIEW, StringComparison.OrdinalIgnoreCase))
                    return onlineUrl;
                return protocal + PREVIEW + urlNoProtocal;
            }
            // 测试环境
            if (IsTest)
            {
                return protocal + TEST + urlNoProtocal;
            }
            // 其它统一视为开发环境
            return protocal + DEV + urlNoProtocal;
        }


        /// <summary>
        /// 当前使用的配置文件名
        /// </summary>
        public static string ConfigFileName => GetConfigFilename();

        /// <summary>
        /// 当前使用的配置文件完整路径，为空表示未找到可用的配置文件。
        /// </summary>
        public static string ConfigFileFullName = GetConfigFilePath(GetConfigFilename());

        /// <summary>
        /// 查找本地配置文件实际存储的路径
        /// </summary>
        /// <param name="hostValue"></param>
        /// <returns></returns>
        private static string GetConfigFilePath(string hostValue)
        {
            string path = null;
            foreach (var subpath in ConfigSubPathlist)
            {
                var temppath = Path.Combine(Dir, subpath + hostValue);
                if (File.Exists(temppath))
                {
                    path = temppath;
                    break;
                }
            }

            return path;
        }

        /// <summary>
        /// 获取当前环境的配置文件路径
        /// </summary>
        /// <returns></returns>
        internal static string GetConfigFilename()
        {
            if (IsOnline)
                return PRODUCT + ".xml";
            // 预发环境
            if (IsPreview)
                return PREVIEW + ".xml";
            // 测试环境
            if (IsTest)
                return TEST + ".xml";
            // 其它统一视为开发环境
            return DEV + ".xml";
        }
        #endregion


        #region 其它环境变量
        /// <summary>
        /// 当前进程ID
        /// </summary>
        public static string ProcessId { get; } = Process.GetCurrentProcess().Id.ToString();

        /// <summary>
        /// 当前项目的App.Config所在路径。
        /// Web站点是D:\xxx\web.config.
        /// </summary>
        public static string AppConfigPath { get; } = Convert.ToString(AppDomain.CurrentDomain.GetData("APP_CONFIG_FILE"));
        private static bool? isWebApp;
        /// <summary>
        /// 当前项目是否web应用
        /// </summary>
        public static bool IsWebApp
        {
            get
            {
                // 通过是web.config还是app.config来判断；
                // 也可以通过 HostingEnvironment.IsHosted 判断
                if (isWebApp == null)
                {
                    var configFile = AppConfigPath;
                    if (string.IsNullOrEmpty(configFile))
                        isWebApp = false;
                    else
                    {
                        configFile = Path.GetFileName(configFile);
                        isWebApp = configFile.StartsWith("web", StringComparison.OrdinalIgnoreCase);
                    }
                }
                return isWebApp.Value;
            }
        }

        #endregion
    }
}
