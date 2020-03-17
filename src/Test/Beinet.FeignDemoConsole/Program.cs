using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Beinet.Feign;

namespace Beinet.FeignDemoConsole
{
    class Program
    {
        static void Main()
        {
            WriteMsg("程序启动");

            CallDefaultConfigTest();

            CallCustomConfigTest();

            Console.Read();
        }

        /// <summary>
        /// 使用默认配置的Feign测试
        /// </summary>
        static void CallDefaultConfigTest()
        {
            var feignDemo = ProxyLoader.GetProxy<FeignApiTest>();

            // GET返回数值
            var intRet = feignDemo.GetMs();
            WriteMsg(intRet.ToString());
            intRet = feignDemo.GetAdd(12, 23);
            WriteMsg(intRet.ToString());

            // POST返回数值
            intRet = feignDemo.PostAdd(45, 23);
            WriteMsg(intRet.ToString());


            // 无参的GET调用
            var str = feignDemo.GetUserStr();
            WriteMsg(str);

            // 无参的GET调用，返回对象
            var user = feignDemo.GetUser();
            WriteMsg(user.ToString());

            // 有参的GET调用，返回对象
            user = feignDemo.GetUser(12, "游北亮");
            WriteMsg(user.ToString());

            // 无参的POST调用，返回对象
            user = feignDemo.PostUser();
            WriteMsg(user.ToString());

            // 有参的POST调用，返回对象
            user = feignDemo.PostUser(12, "游北亮");
            WriteMsg(user.ToString());

            // 有参的POST调用，返回对象
            user = feignDemo.PostUser(user);
            WriteMsg(user.ToString());

            // 有参的POST调用，返回对象
            user = feignDemo.PostUser(user, 357);
            WriteMsg(user.ToString());
        }

        static void CallCustomConfigTest()
        {
            var feign = ProxyLoader.GetProxy<FeignApiTestWithConfig>();

            var user = feign.PostUser(11, "ddddd");
            WriteMsg(user.ToString());

            user = feign.PostUser(user);


            WriteMsg(user.ToString());
        }

        private static int _idx = 0;

        public static void WriteMsg(string msg)
        {
            var ret = Interlocked.Increment(ref _idx);
            Console.WriteLine($"{ret.ToString()}: {msg}\r\n");
        }

    }
}
