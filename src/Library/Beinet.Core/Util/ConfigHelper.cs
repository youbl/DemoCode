using System;
using System.Collections.Specialized;
using System.Configuration;

namespace Beinet.Core.Util
{
	/// <summary>
	/// ��ȡAppSettings�е�����
	/// </summary>
	public static class ConfigHelper
	{
        #region Ӧ�ó������ýڵ㼯������
        /// <summary>
        /// Ӧ�ó������ýڵ㼯������
        /// </summary>
        /// <returns></returns>
        public static NameValueCollection SettingsCollection => ConfigurationManager.AppSettings;

        /// <summary>
        /// Ӧ�ó����������ü�������
        /// </summary>
        /// <returns></returns>
        public static ConnectionStringSettingsCollection ConnectionCollection => ConfigurationManager.ConnectionStrings;

	    #endregion

        #region ��ȡappSettings�ڵ�ֵ
        /// <summary>
        /// ��ȡappSettings�ڵ�ֵ
        /// </summary>
        /// <param name="key">�ڵ�����</param>
        /// <param name="defaultValue">Ĭ��ֵ</param>
        /// <returns>�ڵ�ֵ</returns>
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
        /// ��ȡappSettings�ڵ�ֵ����ת��Ϊboolֵ
        /// </summary>
        /// <param name="key">�ڵ�����</param>
        /// <param name="defaultValue">�ڵ㲻����ʱ��Ĭ��ֵ</param>
        /// <returns></returns>
        public static bool GetBoolean(string key, bool defaultValue = false)
        {
            string tmp = GetSetting(key);
            if (string.IsNullOrEmpty(tmp))
                return defaultValue;
            return tmp.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// ��ȡappSettings�ڵ�ֵ����ת��Ϊintֵ
        /// </summary>
        /// <param name="key">�ڵ�����</param>
        /// <param name="defaultValue">�ڵ㲻���ڻ�����ֵʱ��Ĭ��ֵ</param>
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
        /// ��ȡappSettings�ڵ�ֵ����ת��Ϊlongֵ
        /// </summary>
        /// <param name="key">�ڵ�����</param>
        /// <param name="defaultValue">�ڵ㲻���ڻ�����ֵʱ��Ĭ��ֵ</param>
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
        /// ��ȡappSettings�ڵ�ֵ����ת��ΪDateTimeֵ
        /// </summary>
        /// <param name="key">�ڵ�����</param>
        /// <param name="defaultValue">�ڵ㲻���ڻ���ʱ��ʱ��Ĭ��ֵ</param>
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

        #region ��ȡָ�����Ƶ������ַ���
        /// <summary>
        /// ��ȡָ�����Ƶ������ַ���
        /// </summary>
        /// <param name="connName">���Ӵ��ڵ�����</param>
        /// <param name="defaultConn">Ĭ�����Ӵ�</param>
        /// <returns>���Ӵ�</returns>
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
