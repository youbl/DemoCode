using System;
using System.Diagnostics;
using System.Management;
using System.Text;
//using Microsoft.Win32;
using NLog;

namespace Beinet.Core.Util
{
    public static class SystemHelper
    {
        private static ILogger log = LogManager.GetCurrentClassLogger();

        // 用于计算cpu使用率，注意，一定要先调用一次 cpuUsage.NextValue();
        private static PerformanceCounter cpuUsage;

        // 用于计算可用内存
        private static PerformanceCounter ramCounter;

        static SystemHelper()
        {
            /*
             * 如果报错：无法加载计数器名称数据，因为从注册表读取的索引“”无效
             * 说明用户机器缺少某个计数器，可以尝试修复：
             * 按开始=》运行，输入命令: perfmon
             * 可以打开性能监视器，如果出错
             * cmd 输入:  lodctr /r 即可修复
             */
            try
            {
                cpuUsage =
                    new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                cpuUsage.NextValue();
            }
            catch (Exception exp)
            {
                log.Error("cpuUsage初始化出错:{0}", exp.Message);
            }

            try
            {
                ramCounter =
                    new PerformanceCounter("Memory", "Available MBytes", true);
                ramCounter.NextValue();
            }
            catch (Exception exp)
            {
                log.Error("ramCounter初始化出错:{0}", exp.Message);
            }
        }

        /// <summary>
        /// 获取开机启动时间
        /// </summary>
        /// <returns></returns>
        public static string GetStartTime()
        {
            var runMiis = Environment.TickCount;
            return DateTime.Now.AddMilliseconds(-runMiis).ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        /// <summary>
        /// 获取当前CPU使用率
        /// </summary>
        /// <returns></returns>
        public static string GetCpuUsage()
        {
            if (cpuUsage == null)
                return "";
            return Math.Round(cpuUsage.NextValue(), 2) + "%";
        }

        /// <summary>
        /// 获取当前物理内存数
        /// </summary>
        /// <returns></returns>
        public static long GetMemoryTotal()
        {
            var mos = "SELECT TotalPhysicalMemory FROM Win32_ComputerSystem";
            var result = 0L;
            ManagementObjectQuery(mos,
                record => { result = long.Parse(record["TotalPhysicalMemory"].ToString().Trim()); });

            return result;
        }

        /// <summary>
        /// 获取当前已用内存数
        /// </summary>
        /// <returns></returns>
        public static long GetMemoryAvail()
        {
            if (ramCounter == null)
                return 0;
            return ((long) ramCounter.NextValue()) * 1024 * 1024;
        }

        /// <summary>
        /// 获取当前系统分辨率
        /// </summary>
        /// <returns></returns>
        public static string GetResolution()
        {
            var mos = "SELECT CurrentHorizontalResolution,CurrentVerticalResolution FROM Win32_VideoController";
            var result = new StringBuilder();
            ManagementObjectQuery(mos, record =>
            {
                var row = string.Format("{0}*{1}",
                    record["CurrentHorizontalResolution"],
                    record["CurrentVerticalResolution"]);
                Append(result, row);
            });

            return result.ToString();
        }

        private static void ManagementObjectQuery(string mosSql, Action<ManagementBaseObject> action,
            int top = Int32.MaxValue)
        {
            //var sb = new StringBuilder();
            using (var mydisplayResolution = new ManagementObjectSearcher(mosSql))
            {
                var i = 0;
                foreach (var record in mydisplayResolution.Get())
                {
                    i++;
                    if (i > top)
                        break;
                    // foreach (var property in record.Properties)
                    // {
                    //     sb.AppendFormat("{0} : {1}\n", property.Name, property.Value);
                    // }
                    //
                    // foreach (var property in record.SystemProperties)
                    // {
                    //     sb.AppendFormat("{0} : {1}\n", property.Name, property.Value);
                    // }
                    // Console.WriteLine(sb + "\n");

                    action(record);
                }
            }
        }

        public static string ByteToStr(object byteSize)
        {
            if (byteSize == null)
                return "0";
            long size = long.Parse(byteSize.ToString().Trim());
            return ByteToStr(size);
        }

        public static string ByteToStr(long byteSize)
        {
            if (byteSize < 1024)
                return byteSize.ToString() + "字节";
            byteSize = (long) Math.Ceiling(byteSize / 1024.0);
            if (byteSize < 1024)
                return byteSize.ToString() + "KB";
            byteSize = (long) Math.Ceiling(byteSize / 1024.0);
            if (byteSize < 1024)
                return byteSize.ToString() + "MB";
            byteSize = (long) Math.Ceiling(byteSize / 1024.0);
            if (byteSize < 1024)
                return byteSize.ToString() + "GB";

            byteSize = (long) Math.Ceiling(byteSize / 1024.0);
            return byteSize.ToString() + "TB";
        }

        /// <summary>
        /// 返回CPU序列号
        /// </summary>
        /// <returns></returns>
        public static string GetCpuId()
        {
            var mos = "SELECT ProcessorId FROM Win32_Processor";
            var result = new StringBuilder();
            ManagementObjectQuery(mos, record => { Append(result, record["ProcessorId"]); }, 1);

            return result.ToString();
        }

        /// <summary>
        /// 返回CPU描述
        /// </summary>
        /// <returns></returns>
        public static string GetCpuName()
        {
            var mos = "SELECT Name FROM Win32_Processor";
            var result = new StringBuilder();
            ManagementObjectQuery(mos, record => { Append(result, record["Name"]); }, 1);

            return result.ToString();
        }

        /// <summary>
        /// 返回硬盘序列号
        /// </summary>
        /// <returns></returns>
        public static string GetDiskId()
        {
            var mos = "SELECT SerialNumber FROM Win32_DiskDrive";
            var result = new StringBuilder();
            ManagementObjectQuery(mos, record => { Append(result, record["SerialNumber"]); }, 1);

            return result.ToString();
        }

        /// <summary>
        /// 返回主板序列号
        /// </summary>
        /// <returns></returns>
        public static string GetBoardId()
        {
            var mos = "SELECT SerialNumber FROM Win32_BaseBoard";
            var result = new StringBuilder();
            ManagementObjectQuery(mos, record =>
            {
                Append(result, record["SerialNumber"]);
            }, 1);

            return result.ToString();
        }

        /// <summary>
        /// 返回网卡MAC
        /// </summary>
        /// <returns></returns>
        public static string GetMac()
        {
            var mos = "SELECT IPEnabled,MacAddress FROM Win32_NetworkAdapterConfiguration";
            var result = new StringBuilder();
            ManagementObjectQuery(mos, record =>
            {
                if (!(bool) record["IPEnabled"])
                    return;

                Append(result, record["MacAddress"]);
            });

            return result.ToString().Trim();
        }


        /// <summary>
        /// 返回磁盘信息
        /// </summary>
        /// <returns></returns>
        public static string GetDiskInfo()
        {
            var mos = "SELECT Name,Size,FreeSpace FROM Win32_LogicalDisk";
            var result = new StringBuilder();
            ManagementObjectQuery(mos, record =>
            {
                var row = string.Format("{0} {1}/{2}", record["Name"], record["FreeSpace"], record["Size"]);
                Append(result, row);
            });

            return result.ToString().Trim();
        }

        private static void Append(StringBuilder sb, object obj, string split = ";")
        {
            if (obj == null)
                return;
            var newVal = obj.ToString().Trim();
            if (newVal.Length <= 0)
                return;
            if (sb.Length > 0)
                sb.Append(split);
            sb.Append(newVal);
        }

        /// <summary>
        /// 禁用或启用防火墙
        /// </summary>
        /// <param name="enable"></param>
        /// <returns></returns>
        public static bool FirewallOperate(bool enable)
        {
            var sw = enable ? "on" : "off";
            var cmd = "/C netsh advfirewall set allprofiles state " + sw;
            using (Process process = new Process())
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.FileName = @"C:\Windows\System32\cmd.exe";
                startInfo.Arguments = cmd;
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }

            return true;

            // 注册表方式，验证不成功
            // var val = enable ? 1 : 0;
            // RegistryKey key = Registry.LocalMachine;
            // string path =
            //     "HKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet001\\Services\\SharedAccess\\Defaults\\FirewallPolicy";
            // RegistryKey firewall = key.OpenSubKey(path, true);
            // if (firewall == null)
            //     return false;
            //
            // RegistryKey domainProfile = firewall.OpenSubKey("DomainProfile", true);
            // RegistryKey publicProfile = firewall.OpenSubKey("PublicProfile", true);
            // RegistryKey standardProfile = firewall.OpenSubKey("StandardProfile", true);
            // if (domainProfile != null)
            //     domainProfile.SetValue("EnableFirewall", val, RegistryValueKind.DWord);
            // if (publicProfile != null)
            //     publicProfile.SetValue("EnableFirewall", val, RegistryValueKind.DWord);
            // if (standardProfile != null)
            //     standardProfile.SetValue("EnableFirewall", val, RegistryValueKind.DWord);
            // return true;
        }
    }
}