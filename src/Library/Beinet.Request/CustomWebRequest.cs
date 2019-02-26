using System;
using System.Collections;
using System.Net;
using System.Reflection;

namespace Beinet.Request
{
    /// <summary>
    /// 本类库用于把Web项目中HttpWebRequest类替换为ExtendHttpWebRequest，
    /// 在发起请求时使用统一的UserAgent。
    /// 的无侵入
    /// </summary>
    public class CustomWebRequest
    {
        private static bool inited;
        private static object lockObj = new object();
        /// <summary>
        /// 初始化注入代码
        /// </summary>
        public static void Patch()
        {
            // 双重验证，避免多次注入
            if (inited)
                return;
            lock (lockObj)
            {
                if (inited)
                    return;
                inited = true;
            }
            // Console.WriteLine("开始注入");

            var property = typeof(WebRequest).GetProperty("PrefixList", BindingFlags.Static | BindingFlags.NonPublic);
            var type = typeof(WebRequest).Assembly.GetType("System.Net.WebRequestPrefixElement");
            var prefixField = type.GetField("Prefix");
            var elementConstructor = type.GetConstructor(new[] { typeof(string), typeof(Type) });
            if (property == null || prefixField == null || null == elementConstructor)
                return;

            var list = (ArrayList)property.GetValue(null, new object[0]);
            // 遍历并修改http请求的工厂类, 旧的工厂类是{System.Net.HttpRequestCreator}
            for (int i = 0; i < list.Count; i++)
            {
                var prefix = prefixField.GetValue(list[i]).ToString().ToLower();
                if (prefix.Equals("http:", StringComparison.Ordinal) || prefix.Equals("https:", StringComparison.Ordinal))
                {
                    // http请求使用自定义类
                    list[i] = elementConstructor.Invoke(new object[] { prefix, typeof(ExtendHttpRequestCreator) });
                }
            }
        }
    }


    
}
