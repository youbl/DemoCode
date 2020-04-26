using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Beinet.Core.Cron;

namespace Beinet.CronDemoConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // 主调语句，检索当前程序所有有Scheduled定义的方法，并按Cron表达式执行
            ScheduledWorker.StartAllScheduled();

            Console.Read();
        }

//        [Scheduled("* * * * * *")]
        void aaa()
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fffff") + " 每秒输出一次");
        }

        [Scheduled("1,3,7,11 * * * * *")]
        void bbb()
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fffff") + " 每分钟的1秒，3秒，7秒，11秒输出，其它时间不输出");
        }


        [Scheduled("20,40 40 * * * *")]
        void ccc()
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fffff") + " 每个小时40分的20秒，40秒输出，其它时间不输出");
        }


        [Scheduled("33,45 22 13 * * *")]
        void ddd()
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fffff") + " 每天13点22分的33秒，45秒输出，其它时间不输出");
        }


        [Scheduled("*/10 1 * * * *")]
        void eee()
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fffff") + " 每个小时的1分的时候，每10秒输出，即0秒，10秒，20秒，30秒，40秒，50秒，其它时间不输出");
        }

        [Scheduled("3-10 1 * * * *")]
        void fff()
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fffff") + " 每个小时的1分的时候，第3到第10秒输出，其它时间不输出");
        }

        [Scheduled("32-43/3 * * * * *")]
        void ggg()
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fffff") + " 每分钟，第32到第43秒,每3秒输出，即，其它时间不输出");
        }

        [Scheduled("1,6 58 22 1 * *")]
        void hhh()
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fffff") + " 每月1号的22点58分1秒 和 6秒，各运行1次");
        }


        [Scheduled("0 0 9 1 * 0,6")]
        void iii()
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fffff") + " 每月1号9点整，如果是周末，执行1次");
        }

        [Scheduled("0 0 9 1 * * 2022")]
        void jjj()
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fffff") + " 2022年每月1号9点整，执行，全年共12次");
        }
    }
}
