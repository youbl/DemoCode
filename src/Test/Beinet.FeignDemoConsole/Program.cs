using System;
using System.Threading;
using Beinet.Feign;
using Newtonsoft.Json;

namespace Beinet.FeignDemoConsole
{
    class Program
    {
        static void Main()
        {
            WriteMsg("程序启动");

            TestAloneProcess();

            TestQuick();
            TestPlace();
            TestHeader();
            TestConfig();
            TestURI();
            TestArgType();

            Console.Read();
        }

        // 常规调用
        static void TestQuick()
        {
            FeignTestQuick feign = ProxyLoader.GetProxy<FeignTestQuick>();

            feign.Get();

            int ret1 = feign.GetMs();
            WriteMsg(ret1);

            int ret2 = feign.GetAdd(12, 34);
            WriteMsg(ret2);

            int ret3 = feign.PostAdd(56, 78);
            WriteMsg(ret3);

            string json = feign.GetDtoStr();
            WriteMsg(json);

            FeignDtoDemo dto1 = feign.GetDtoObj();
            WriteMsg(JsonConvert.SerializeObject(dto1));

            FeignDtoDemo dto2 = feign.PostDtoObj(11, "fankuai");
            WriteMsg(JsonConvert.SerializeObject(dto2));

            FeignDtoDemo dto3 = feign.PostDtoObj(dto2, "xxx");
            WriteMsg(JsonConvert.SerializeObject(dto3));

            object obj = feign.GetObj();
            WriteMsg($"返回类型：{dto3.GetType()}");
            WriteMsg(JsonConvert.SerializeObject(obj));
        }

        // 配置占位符读取
        static void TestPlace()
        {
            FeignTestPlace feign = ProxyLoader.GetProxy<FeignTestPlace>();

            try
            {
                // 如下代码发起的HTTP请求，最终的url是： https://47.107.125.247/prod/cc/test/api.aspx?n1=12&n2=45&securekey=123456
                FeignDtoDemo dto1 = feign.GetDtoObj(12, 45);
                WriteMsg(JsonConvert.SerializeObject(dto1));
            }
            catch { }
        }

        // 添加Header测试
        static void TestHeader()
        {
            FeignTestHeader feign = ProxyLoader.GetProxy<FeignTestHeader>();

            // http调用前，会添加header："User-Agent":"beinet feign1234", "headerName":"headerValue"
            FeignDtoDemo dto1 = feign.GetDtoObj();
            WriteMsg(JsonConvert.SerializeObject(dto1));

            // http调用前，会添加header："headerName":"header1","RealHeaderName":"header2"
            FeignDtoDemo dto2 = feign.GetDtoObj("header1", "header2");
            WriteMsg(JsonConvert.SerializeObject(dto2));
        }

        // 测试 使用自定义配置类
        static void TestConfig()
        {
            FeignTestConfig feign = ProxyLoader.GetProxy<FeignTestConfig>();
            // 可以看到调用前后会输出日志，和请求耗时
            FeignDtoDemo dto = feign.GetDtoObj();
            WriteMsg(JsonConvert.SerializeObject(dto));

            try
            {
                feign.GetErr();// 可以看到调用后会输出错误信息
            }
            catch { }
        }

        // 参数中存在URI类型，且不为空时，会忽略FeignClient的Url配置
        static void TestURI()
        {
            FeignTestURI feign = ProxyLoader.GetProxy<FeignTestURI>();
            Uri uri = new Uri("https://47.107.125.247/");

            // 请求为 GET https://47.107.125.247/test/api.aspx 
            FeignDtoDemo dto1 = feign.GetDtoObj(uri);
            WriteMsg(JsonConvert.SerializeObject(dto1));

            // 请求为 POST https://47.107.125.247/test/api.aspx Stream为abc
            FeignDtoDemo dto2 = feign.GetDtoObj("abc", uri);
            WriteMsg(JsonConvert.SerializeObject(dto2));

            // uri参数传空，使用类定义的url，即 GET https://47.107.125.247/test/api.aspx
            FeignDtoDemo dto3 = feign.GetDtoObj(null);
            WriteMsg(JsonConvert.SerializeObject(dto3));
        }

        static void TestArgType()
        {
            FeignTestArgType feign = ProxyLoader.GetProxy<FeignTestArgType>();
            Type type = typeof(FeignDtoDemo);

            object dto1 = feign.GetDtoObj(type);
            WriteMsg($"返回类型：{dto1.GetType()}");
            WriteMsg(JsonConvert.SerializeObject(dto1));

            object dto2 = feign.GetDtoObj("123", type);
            WriteMsg($"返回类型：{dto2.GetType()}");
            WriteMsg(JsonConvert.SerializeObject(dto2));

            object dto3 = feign.GetDtoObj(null);
            WriteMsg($"返回类型：{dto3.GetType()}");
            WriteMsg(JsonConvert.SerializeObject(dto3));

            try
            {
                feign.GetErr(typeof(object));
            }
            catch (Exception exp)
            {
                WriteMsg(exp);
            }
        }

        static void TestAloneProcess()
        {
            var map = new GetMappingAttribute("test/api.aspx");
            var process = new FeignProcess();
            process.Url = "https://47.107.125.247";
            var result = process.Process<string>(map, null);
            Console.WriteLine(result);
        }


        private static int _idx = 0;

        public static void WriteMsg(object msg)
        {
            var ret = Interlocked.Increment(ref _idx);
            Console.WriteLine($"{ret.ToString()}: {msg}\r\n");
        }

    }
}
