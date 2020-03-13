using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinFu.DynamicProxy;

namespace DemoForDynamicProxy
{
    /// <summary>
    /// 这个Demo用于演示，如何创建一个没有编写实现的接口实例
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // 创建指定接口的实例代理
            var obj = ProxyLoader.GetProxy<DemoInf>();
            obj.NoReturn();
            obj.NoReturn("abcd", 123);

            // 创建指定接口和指定类的实例代理
            obj = ProxyLoader.GetProxy<DemoInf>(typeof(DeomClass));
            obj.NoReturn();
            obj.NoReturn("abcd", 123);
           
            var tmp = ((DeomClass) obj).DemoMethod(456, "daf");
            Console.WriteLine(tmp);

            Console.Read();
        }
    }

    /// <summary>
    /// 定义一个接口，但是没有具体实现
    /// </summary>
    public interface DemoInf
    {
        void NoReturn();

        void NoReturn(string para, int code);

        string ReturnStr();

        string ReturnStr(string para);

    }

    public class DeomClass
    {
        public string DemoMethod(int code, string para)
        {
            return $"haha:{code.ToString()}-{para}";
        }
    }
}
