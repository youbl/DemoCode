using System;
using System.Configuration;

namespace Beinet.MQ
{
    /// <summary>
    /// RabbitMQ连接池设置类
    /// </summary>
    public class RabbitMQConfigSection : ConfigurationSection
    {
        /// <summary>
        /// 获取或设置服务器IP
        /// </summary>
        [ConfigurationProperty("ServerIp", DefaultValue = "10.2.0.174", IsRequired = true)]
        public string ServerIp
        {
            get
            {
                return base["ServerIp"].ToString();
            }
            set
            {
                base["ServerIp"] = value;
            }
        }

        /// <summary>
        /// 获取或设置服务器端口号
        /// </summary>
        [ConfigurationProperty("ServerPort", DefaultValue = 5672, IsRequired = true)]
        public int ServerPort
        {
            get
            {
                return Convert.ToInt32(base["ServerPort"]);
            }
            set
            {
                base["ServerPort"] = value;
            }
        }

        /// <summary>
        /// 获取或设置连接的帐号
        /// </summary>
        [ConfigurationProperty("UserName", DefaultValue = "guest", IsRequired = true)]
        public string UserName
        {
            get
            {
                return base["UserName"].ToString();
            }
            set
            {
                base["UserName"] = value;
            }
        }

        /// <summary>
        /// 获取或设置连接的密码
        /// </summary>
        [ConfigurationProperty("Password", DefaultValue = "", IsRequired = true)]
        public string Password
        {
            get
            {
                return base["Password"].ToString();
            }
            set
            {
                base["Password"] = value;
            }
        }

        /// <summary>
        /// 获取或设置连接池中的连接心跳检测间隔时间，默认10秒
        /// </summary>
        [ConfigurationProperty("HeartBeatSecond", DefaultValue = 10, IsRequired = false)]
        public int HeartBeatSecond
        {
            get
            {
                return Convert.ToInt32(base["HeartBeatSecond"]);
            }
            set
            {
                base["HeartBeatSecond"] = value;
            }
        }

        /// <summary>
        /// 获取或设置连接命名
        /// </summary>
        [ConfigurationProperty("ConnectionName", DefaultValue = "", IsRequired = false)]
        public string ConnectionName
        {
            get
            {
                return Convert.ToString(base["ConnectionName"]);
            }
            set
            {
                base["ConnectionName"] = value;
            }
        }
    }
}
