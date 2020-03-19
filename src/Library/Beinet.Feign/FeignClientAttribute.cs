using System;

namespace Beinet.Feign
{
    [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public class FeignClientAttribute : Attribute
    {
        public FeignClientAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 要调用的微服务名
        /// </summary>
        public virtual string Name { get; }

        private string _url;

        /// <summary>
        /// 要调用的服务URL前缀，如 https://app.baidu.com/cc/
        /// </summary>
        public virtual string Url
        {
            get => _url;
            set
            {
                value = value == null ? "" : value.Trim();
                if (value.Length <= 0)
                    throw new Exception("Feign Url不能为空");
                _url = value;
            }
        }
        

        private Type _configuration;

        /// <summary>
        /// 请求拦截或编码解码接口
        /// </summary>
        public virtual Type Configuration
        {
            get => _configuration;
            set
            {
                if (value == null)
                    return;
                if (!typeof(IFeignConfig).IsAssignableFrom(value))
                    throw new Exception("配置类必须实现IFeignConfig");

                _configuration = value;
            }
        }
    }
}