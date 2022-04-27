using System;
using System.ComponentModel;
using System.Threading;
using Beinet.Core;
using Beinet.Core.EnumExt;
using Beinet.Core.FileExt;
using Beinet.Core.Util;

namespace DemoCodeConsole.CoreTest
{
    class EnumHelperTest : IRunable
    {
        public void Run()
        {
            while (true)
            {
                Console.WriteLine(SystemHelper.GetCpuUsage());
                Console.WriteLine(SystemHelper.GetMemoryTotal());
                Console.WriteLine(SystemHelper.GetMemoryAvail());
                Thread.Sleep(1200);
            }

            var aa = TestEnum.Aaa.GetDesc();
            var bb = TestEnum.Bbb.GetDesc();
            var cc = TestEnum.Ccc.GetDesc();
        }
    }

    enum TestEnum
    {
        Aaa,
        [Description("I'm xxx")] Bbb,

        [Description] Ccc
    }
}