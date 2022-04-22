﻿using System;
using System.Diagnostics;
using System.Management;
using System.Text;

namespace Beinet.Core.Util
{
    public static class SystemHelper
    {
        // 用于计算cpu使用率，注意，一定要先调用一次 cpuUsage.NextValue();
        private static PerformanceCounter cpuUsage =
            new PerformanceCounter("Processor", "% Processor Time", "_Total", true);

        // 用于计算可用内存
        private static PerformanceCounter ramCounter =
            new PerformanceCounter("Memory", "Available MBytes", true);

        static SystemHelper()
        {
            cpuUsage.NextValue();
            ramCounter.NextValue();
        }

        /// <summary>
        /// 获取当前CPU使用率
        /// </summary>
        /// <returns></returns>
        public static string GetCpuUsage()
        {
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
                if (result.Length > 0)
                {
                    result.AppendLine();
                }

                result.AppendFormat("{0}*{1}",
                    record["CurrentHorizontalResolution"],
                    record["CurrentVerticalResolution"]);
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

        private static string ByteToStr(object byteSize)
        {
            if (byteSize == null)
                return "0";
            long size = long.Parse(byteSize.ToString().Trim());
            return ByteToStr(size);
        }

        private static string ByteToStr(long byteSize)
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
            ManagementObjectQuery(mos, record =>
            {
                if (result.Length > 0)
                {
                    result.AppendLine();
                }

                result.AppendFormat("{0}", (record["ProcessorId"] ?? "").ToString().Trim());
            }, 1);

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
            ManagementObjectQuery(mos, record =>
            {
                if (result.Length > 0)
                {
                    result.AppendLine();
                }

                result.AppendFormat("{0}", (record["Name"] ?? "").ToString().Trim());
            }, 1);

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
            ManagementObjectQuery(mos, record =>
            {
                if (result.Length > 0)
                {
                    result.AppendLine();
                }

                result.AppendFormat("{0}", (record["SerialNumber"] ?? "").ToString().Trim()); // Model
            }, 1);

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
                if (result.Length > 0)
                {
                    result.AppendLine();
                }

                result.AppendFormat("{0}", (record["SerialNumber"] ?? "").ToString().Trim());
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
                if (result.Length > 0)
                {
                    result.AppendLine();
                }

                if ((bool) record["IPEnabled"])
                    result.AppendFormat("{0}", (record["MacAddress"] ?? "").ToString().Trim());
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
                if (result.Length > 0)
                {
                    result.AppendLine();
                }

                result.AppendFormat("{0} {1}/{2}", record["Name"], record["FreeSpace"], record["Size"]);
            });

            return result.ToString().Trim();
        }
    }
}