using System;
using System.Collections.Specialized;
using System.Configuration;

namespace Beinet.Core.Util
{
	/// <summary>
	/// 获取AppSettings中的配置
	/// </summary>
	public static class ConfigHelper
	{
        #region 应用程序配置节点集合属性
        /// <summary>
        /// 应用程序配置节点集合属性
        /// </summary>
        /// <returns></returns>
        public static NameValueCollection SettingsCollection => ConfigurationManager.AppSettings;

        /// <summary>
        /// 应用程序连接配置集合属性
        /// </summary>
        /// <returns></returns>
        public static ConnectionStringSettingsCollection ConnectionCollection => ConfigurationManager.ConnectionStrings;

	    #endregion

        #region 获取appSettings节点值
        /// <summary>
        /// 获取appSettings节点值
        /// </summary>
        /// <param name="key">节点名称</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>节点值</returns>
        public static string GetSetting(string key, string defaultValue = "")
		{
			try
			{
                if (SettingsCollection == null)
                    return defaultValue;
                return SettingsCollection[key] ?? defaultValue;
			}
			catch
			{
				return defaultValue;
			}
		}

        /// <summary>
        /// 获取appSettings节点值，并转换为bool值
        /// </summary>
        /// <param name="key">节点名称</param>
        /// <param name="defaultValue">节点不存在时的默认值</param>
        /// <returns></returns>
        public static bool GetBoolean(string key, bool defaultValue = false)
        {
            string tmp = GetSetting(key);
            if (string.IsNullOrEmpty(tmp))
                return defaultValue;
            return tmp.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取appSettings节点值，并转换为int值
        /// </summary>
        /// <param name="key">节点名称</param>
        /// <param name="defaultValue">节点不存在或不是数值时的默认值</param>
        /// <returns></returns>
        public static int GetInt32(string key, int defaultValue = 0)
        {
            string tmp = GetSetting(key);
            if (string.IsNullOrEmpty(tmp))
                return defaultValue;
            if (int.TryParse(tmp, out var ret))
                return ret;
            else
                return defaultValue;
        }

        /// <summary>
        /// 获取appSettings节点值，并转换为long值
        /// </summary>
        /// <param name="key">节点名称</param>
        /// <param name="defaultValue">节点不存在或不是数值时的默认值</param>
        /// <returns></returns>
        public static long GetInt64(string key, long defaultValue = 0)
        {
            string tmp = GetSetting(key);
            if (string.IsNullOrEmpty(tmp))
                return defaultValue;
            if (long.TryParse(tmp, out long ret))
                return ret;
            else
                return defaultValue;
        }

        /// <summary>
        /// 获取appSettings节点值，并转换为DateTime值
        /// </summary>
        /// <param name="key">节点名称</param>
        /// <param name="defaultValue">节点不存在或不是时间时的默认值</param>
        /// <returns></returns>
        public static DateTime GetDateTime(string key, DateTime? defaultValue = null)
        {
            string tmp = GetSetting(key);
            if (!string.IsNullOrEmpty(tmp))
            {
                if (DateTime.TryParse(tmp, out var ret))
                    return ret;
            }
            if (defaultValue == null)
                return DateTime.MinValue;
            else
                return defaultValue.Value;
        }

        #endregion

        #region 获取指定名称的连接字符串
        /// <summary>
        /// 获取指定名称的连接字符串
        /// </summary>
        /// <param name="connName">连接串节点名称</param>
        /// <param name="defaultConn">默认连接串</param>
        /// <returns>连接串</returns>
        public static string GetConnectionString(string connName, string defaultConn = "")
		{
		    try
		    {
		        if (ConnectionCollection == null)
		            return defaultConn;
		        var conObj = ConnectionCollection[connName];
		        if (conObj == null)
		            return defaultConn;
                return conObj.ConnectionString ?? defaultConn;
		    }
		    catch
		    {
		        return defaultConn;
		    }
        }
		#endregion
	}
}
