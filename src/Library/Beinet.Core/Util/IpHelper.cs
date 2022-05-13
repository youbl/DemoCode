using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Beinet.Core.Util
{
    /// <summary>
    /// IP相关的操作方法
    /// </summary>
    public static class IpHelper
    {
        /// <summary>
        /// 是否ip的正则
        /// </summary>
        static Regex regIp = new Regex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$", RegexOptions.Compiled);

        /// <summary>
        /// 判断指定的字符串是否IP
        /// </summary>
        /// <param name="ipStr"></param>
        /// <returns></returns>
        public static bool IsIp(string ipStr)
        {
            return !string.IsNullOrWhiteSpace(ipStr) && regIp.IsMatch(ipStr);
        }

        /// <summary>
        /// 返回客户端真实IP，按如下顺序查找并返回其中1个：HTTP_X_FORWARDED_FOR，HTTP_X_REAL_IP，UserHostAddress
        /// </summary>
        /// <returns></returns>
        public static string GetClientIp()
        {
            try
            {
                if (HttpContext.Current == null)
                    return string.Empty;
                var request = HttpContext.Current.Request;

                string forwardip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (!string.IsNullOrWhiteSpace(forwardip))
                {
                    // 存在多个IP时，取第一个外网IP返回
                    var arrForWardIp = forwardip.Split(',');
                    var ipret = string.Empty;
                    foreach (var ip in arrForWardIp)
                    {
                        var iptmp = ip.Trim();
                        if (iptmp.Length == 0)
                        {
                            continue;
                        }

                        ipret = iptmp;
                        if (!iptmp.StartsWith("10.", StringComparison.Ordinal) &&
                            !iptmp.StartsWith("172.16.", StringComparison.Ordinal) &&
                            !iptmp.StartsWith("192.168.", StringComparison.Ordinal))
                        {
                            return iptmp;
                        }
                    }

                    // 没找到外网IP时，返回一个内网IP
                    if (ipret.Length > 0)
                    {
                        return ipret;
                    }
                }

                string realip = (request.ServerVariables["HTTP_X_REAL_IP"] ?? "").Trim();
                if (realip.Length > 0)
                {
                    return realip;
                }

                return request.UserHostAddress;
            }
            catch (Exception)
            {
                // 在站点的Application_Start里无法访问Request
                return string.Empty;
            }
        }

        /// <summary>
        /// 返回客户端IP列表，包含3个：UserHostAddress;HTTP_X_REAL_IP;HTTP_X_FORWARDED_FOR
        /// </summary>
        /// <returns></returns>
        public static string GetClientIpLst()
        {
            try
            {
                if (HttpContext.Current == null)
                    return string.Empty;
                var request = HttpContext.Current.Request;
                string realip = request.ServerVariables["HTTP_X_REAL_IP"];
                string forwardip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                return string.Format("{0};{1};{2}", request.UserHostAddress, realip, forwardip);
            }
            catch (Exception)
            {
                // 在站点的Application_Start里无法访问Request
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取服务器(本机)所有IPV4地址列表
        /// </summary>
        /// <returns>本机所有IPV4地址列表，以分号分隔</returns>
        public static string GetServerIpList()
        {
            try
            {
                StringBuilder ips = new StringBuilder();
                IPHostEntry IpEntry = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ipa in IpEntry.AddressList)
                {
                    if (ipa.AddressFamily == AddressFamily.InterNetwork)
                        ips.AppendFormat("{0};", ipa);
                }

                if (ips.Length > 1)
                    ips.Remove(ips.Length - 1, 1);
                return ips.ToString();
            }
            catch (Exception)
            {
                //LogHelper.WriteCustom("获取本地ip错误" + ex, @"zIP\", false);
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取服务器(本机)所有IPV4地址列表
        /// </summary>
        /// <returns>本机所有IPV4地址列表</returns>
        public static List<string> GetServerIpArray()
        {
            var ips = new List<string>();
            try
            {
                IPHostEntry IpEntry = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ipa in IpEntry.AddressList)
                {
                    if (ipa.AddressFamily == AddressFamily.InterNetwork)
                        ips.Add(ipa.ToString());
                }
            }
            catch (Exception)
            {
                //LogHelper.WriteCustom("获取本地ip错误" + ex, @"zIP\", false);
            }

            return ips;
        }

        /// <summary>
        /// 将IP地址转成长整型
        /// </summary>
        /// <param name="ip">待转换的IP地址</param>
        /// <returns></returns>
        public static long ConvertIPToLong(string ip)
        {
            //String[] ip = ipAddress.Split('.');
            //long a = int.Parse(ip[0]);
            //long b = int.Parse(ip[1]);
            //long c = int.Parse(ip[2]);
            //long d = int.Parse(ip[3]);

            //long ipNum = a * 256 * 256 * 256 + b * 256 * 256 + c * 256 + d;
            //return ipNum;
            try
            {
                string[] ipList = ip.Split(new char[] {'.'});
                string xIP = string.Empty;
                foreach (string ipStr in ipList)
                {
                    xIP += Convert.ToByte(ipStr).ToString("X").PadLeft(2, '0');
                }

                long ipResult = long.Parse(xIP, System.Globalization.NumberStyles.HexNumber);
                return ipResult;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 将长整型转成IP地址
        /// </summary>
        /// <param name="ipLong">长整型</param>
        /// <returns></returns>
        public static string ConvertLongToIP(long ipLong)
        {
            StringBuilder b = new StringBuilder();

            long tempLong = ipLong;
            long temp = tempLong / (256 * 256 * 256);
            tempLong = tempLong - (temp * 256 * 256 * 256);
            b.Append(Convert.ToString(temp)).Append(".");

            temp = tempLong / (256 * 256);
            tempLong = tempLong - (temp * 256 * 256);
            b.Append(Convert.ToString(temp)).Append(".");

            temp = tempLong / 256;
            tempLong = tempLong - (temp * 256);
            b.Append(Convert.ToString(temp)).Append(".");

            temp = tempLong;
            //tempLong = tempLong - temp;
            b.Append(Convert.ToString(temp));

            return b.ToString().ToLower();
        }


        #region 判断IP地址是否在内网IP地址所在范围

        /// <summary>
        /// 判断IP地址是否为内网IP地址
        /// </summary>
        /// <param name="ipAddress">IP地址字符串</param>
        /// <returns></returns>
        public static bool IsInnerIP(String ipAddress)
        {
            /*
               私有IP：A类  10.0.0.0-10.255.255.255
                       B类  172.16.0.0-172.31.255.255
                       C类  192.168.0.0-192.168.255.255
                       当然，还有127这个网段是环回地址   
              */
            if (ipAddress.Equals("127.0.0.1"))
            {
                return true;
            }

            long ipNum = ConvertIPToLong(ipAddress);
            long aBegin = ConvertIPToLong("10.0.0.0");
            long aEnd = ConvertIPToLong("10.255.255.255");
            long bBegin = ConvertIPToLong("172.16.0.0");
            long bEnd = ConvertIPToLong("172.31.255.255");
            long cBegin = ConvertIPToLong("192.168.0.0");
            long cEnd = ConvertIPToLong("192.168.255.255");
            bool isInnerIp = IsInner(ipNum, aBegin, aEnd) || IsInner(ipNum, bBegin, bEnd) ||
                             IsInner(ipNum, cBegin, cEnd);
            return isInnerIp;
        }

        /// <summary>
        /// 判断用户IP地址转换为Long型后是否在内网IP地址所在范围
        /// </summary>
        /// <param name="userIp"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private static bool IsInner(long userIp, long begin, long end)
        {
            return (userIp >= begin) && (userIp <= end);
        }

        #endregion
    }
}