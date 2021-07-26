using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Routing;

namespace DemoCodeWeb.App_Start
{
    class ActuatorInfoRouteHandler : IRouteHandler
    {
        private ActuatorInfoHttpHandler httpHandler = new ActuatorInfoHttpHandler();

        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return httpHandler;
        }
    }

    class ActuatorInfoHttpHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var libs = assemblies.Select(o => new
            {
                o.GetName().Name,
                Version = o.GetName().Version.ToString()
            }).OrderBy(o => o.Name).ToList();
            context.Response.ContentType = "application/json";

            var info = new
            {
                Server = GetServerInfo(context),
                Client = GetClientInfo(context),
                Settings = GetAppSettings(),
                Libs = libs
            };
            context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(new {info}));
        }

        public bool IsReusable { get; } = true;


        object GetClientInfo(HttpContext context)
        {
            try
            {
                var headers = new Dictionary<string, string>();
                foreach (string name in context.Request.Headers)
                {
                    if (name == "Cookie")
                        continue;
                    headers.Add(name, context.Request.Headers[name]);
                }

                Dictionary<string, string> cookies = null;
                if (context.Request.Cookies.Count > 0)
                {
                    cookies = new Dictionary<string, string>();
                    foreach (string name in context.Request.Cookies)
                    {
                        cookies.Add(name, context.Request.Cookies[name]?.Value);
                    }
                }

                return new
                {
                    IP = context.Request.ServerVariables["REMOTE_ADDR"],
                    RealIP = context.Request.ServerVariables["HTTP_X_REAL_IP"],
                    ViaIP = context.Request.ServerVariables["HTTP_VIA"],
                    ForwardIP = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"],
                    UserAgent = context.Request.UserAgent,
                    Uri = context.Request.Url,
                    Headers = headers,
                    Cookies = cookies
                };
            }
            catch (Exception exp)
            {
                return exp.ToString();
            }
        }

        object GetServerInfo(HttpContext context)
        {
            try
            {
                var process = Process.GetCurrentProcess();
                string processFile;
                try
                {
                    processFile = process.MainModule?.FileName;
                }
                catch (Exception exp)
                {
                    processFile = exp.Message;
                }

                var now = DateTime.Now;
                var startTime = process.StartTime;
                var runningTime = ToTxt(now - startTime);
                return new
                {
                    ServerTime = now,
                    StartTime = startTime,
                    RunningTime = runningTime,
                    IP = context.Request.ServerVariables["Local_Addr"],
                    CLR_Version = Environment.Version.ToString(),
                    ProcessID = process.Id,
                    Mem = (process.WorkingSet64 / 1024 / 1024).ToString() + "MB",
                    Threads = process.Threads.Count,
                    Command = processFile,
                    User = Environment.UserName
                };
            }
            catch (Exception exp)
            {
                return exp.ToString();
            }
        }

        object GetAppSettings()
        {
            try
            {
                var ret = new Dictionary<string, string>();
                foreach (string setting in ConfigurationManager.AppSettings)
                {
                    ret.Add(setting, Convert.ToString(ConfigurationManager.AppSettings[setting]));
                }

                return ret;
            }
            catch (Exception exp)
            {
                return exp.ToString();
            }
        }

        static string ToTxt(TimeSpan ts)
        {
            var seconds = (int) ts.TotalSeconds;
            return ToTxt(seconds);
        }

        static string ToTxt(int seconds)
        {
            var days = seconds / (3600 * 24);

            var daySecond = 3600 * 24 * days;
            seconds -= daySecond;
            var hours = seconds / 3600;

            var hourSecond = hours * 3600;
            seconds -= hourSecond;

            var minutes = seconds / 60;
            seconds -= minutes * 60;

            if (days > 0)
                return string.Format("{0}天{1}时{2}分{3}秒", days.ToString(), hours.ToString(), minutes.ToString(),
                    seconds.ToString());
            if (hours > 0)
                return string.Format("{0}时{1}分{2}秒", hours.ToString(), minutes.ToString(), seconds.ToString());
            if (minutes > 0)
                return string.Format("{0}分{1}秒", minutes.ToString(), seconds.ToString());
            return string.Format("{0}秒", seconds.ToString());
        }
    }
}