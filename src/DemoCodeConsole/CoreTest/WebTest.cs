using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Beinet.Core;
using Beinet.Core.Util;

namespace DemoCodeConsole.CoreTest
{
    class WebTest : IRunable
    {
        public void Run()
        {
            var url = "https://www.baidu.com/";
            Console.WriteLine(WebHelper.GetPage(url));

            WebHelper.GetPage(url, "a=1&b=2", "POST");
        }
    }
}
