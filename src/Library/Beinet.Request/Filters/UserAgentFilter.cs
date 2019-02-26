using System;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace Beinet.Request.Filters
{
    /// <summary>
    /// 编辑HttpWebRequest的UserAgent
    /// </summary>
    sealed class UserAgentFilter : HttpWebRequestFilter
    {
        private static string userAgent;

        public UserAgentFilter(int sort = 0)
        {
            Sort = sort;
        }

        public override void BeforeGetResponse(HttpWebRequest request)
        {
            // 使用统一的UserAgent
            request.UserAgent = GetUserAgent();
            // 避免小写METHOD（HTTP协议不支持小写）
            request.Method = (request.Method ?? "GET").ToUpper();
        }

        private string GetUserAgent()
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                var sb = new StringBuilder("Beinet 1.0");

                // WebRequest一般用于内部请求，所以加上目录，方便问题诊断
                var dir = AppDomain.CurrentDomain.BaseDirectory;
                if (dir.EndsWith(@"\bin\Debug"))
                    dir = dir.Substring(0, dir.Length - @"\bin\Debug".Length);
                if (dir.EndsWith(@"\bin\Release"))
                    dir = dir.Substring(0, dir.Length - @"\bin\Release".Length);
                if (dir.EndsWith(@"\bin"))
                    dir = dir.Substring(0, dir.Length - @"\bin".Length);

                sb.AppendFormat(",{0},{1},{2},Process:{3}", 
                    Environment.OSVersion.VersionString, 
                    Environment.Version, 
                    dir,
                    Process.GetCurrentProcess().Id.ToString());
                userAgent = sb.ToString();
            }
            return userAgent;
        }
    }
}
