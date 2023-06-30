using System;
using System.Text;
using System.Threading;
using Beinet.Core;
using Microsoft.WindowsAPICodePack.Shell;

namespace DemoCodeConsole.FileAttDemo
{
    class FileInfoGet : IRunable
    {
        public void Run()
        {
            var files = new string[]
            {
                @"E:\BaiduNetdiskDownload\乔新亮的CTO成长复盘\001.开篇词丨削弱运气的价值.m4a",
                @"E:\BaiduNetdiskDownload\乔新亮的CTO成长复盘\003.02丨到底该怎么理解工作与薪资的关系？.m4a",
                @"E:\BaiduNetdiskDownload\乔新亮的CTO成长复盘\016.15-需求做不完，应该怎么办？（高级管理者篇）_For_group_share.mp3"
            };
            foreach (var item in files)
            {
                using (ShellObject obj = ShellObject.FromParsingName(item))
                {
                    // 获取 文件的“详细信息”里的数据（右键，属性，详细信息），比如mp3，mp4，excel之类都有的特殊属性
                    var props = obj.Properties.System;
                    var title = props.Title.Value; // 标题
                    var author = GetStr(props.Author.Value); // 参与创作的艺术家
                    var comment = props.Comment.Value;      // 备注

                    var computerName = props.ComputerName.Value; // 当前PC的名称
                    var contentType = props.ContentType.Value;   // 音频类型，比如 audio/mp4
                    var createDate = props.DateAcquired.Value;   // 创建媒体日期
                    var sizeAlloc = props.FileAllocationSize.Value;  // 分配文件大小（跟FileInfo不一致）
                    var size = props.Size.Value;                    // 实际文件大小（跟FileInfo不一致）
                    var owner = props.FileOwner.Value;              // 所有者   
                    

                    Console.WriteLine(props);

//                    foreach (var property in obj.Properties)
//                    {
//                        Console.WriteLine(property);
//                    }
                }
            }
        }

        static string GetStr(string[] arr)
        {
            if (arr == null || arr.Length <= 0)
                return "";
            var sb = new StringBuilder();
            foreach (var item in arr)
            {
                if (string.IsNullOrEmpty(item))
                    continue;
                if (sb.Length > 0)
                    sb.Append(',');
                sb.Append(item);
            }

            return sb.ToString();
        }

        static void OutMsg(string msg)
        {
            var tid = Thread.CurrentThread.ManagedThreadId;
            var now = DateTime.Now.ToString("HH:mm:ss.fff");
            Console.WriteLine($"{now} {tid} {msg}");
        }
    }
}