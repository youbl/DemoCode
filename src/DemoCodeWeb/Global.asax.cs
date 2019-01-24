using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Web;
using System.Web.Http;
using Beinet.Core.Logging;

namespace DemoCodeWeb
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        #region Global里的标准事件

        protected void Application_Start()
        {
            // 启动时记录日志
            var idmsg = "当前进程/线程ID：" + Process.GetCurrentProcess().Id.ToString() + "/" +
                        Thread.CurrentThread.ManagedThreadId.ToString();
            LoggerDefault.Default.Custom("AppStartEnd\\", "App_Start Begin\r\n  " + idmsg);

            GlobalConfiguration.Configure(WebApiConfig.Register);
            // GlobalConfiguration.Configuration.MessageHandlers.Add(null);

            var formatter = GlobalConfiguration.Configuration.Formatters;
            if (formatter != null)
            {
                // Consoller输出 增加jsonp配置的支持
                formatter.Insert(0, new JsonpMediaTypeFormatter());
                // Consoller输出 关闭XML支持
                formatter.XmlFormatter?.SupportedMediaTypes?.Clear();
                // 把long类型数据序列化输出为字符串，因为js对long型数据会丢失精度
                formatter.JsonFormatter?.SerializerSettings.Converters.Add(new JsonLongToString());
                // 敏感词过滤
                // formatter.JsonFormatter?.SerializerSettings.Converters.Add(new BadWordConverter());
            }

            // 初始化完成记录日志
            ThreadPool.GetMinThreads(out var minworkthreads, out var miniocpthreads);
            idmsg += "最小工作线程数/IO线程数:" + minworkthreads.ToString() + "/" + miniocpthreads.ToString();
            LoggerDefault.Default.Custom("AppStartEnd\\", "App_Start End\r\n  " + idmsg);
        }


        /// <summary>
        /// 站点退出，记录日志
        /// </summary>
        protected virtual void Application_End()
        {
            // 程序池停止日志，并收集停止原因
            string message = string.Empty;
            HttpRuntime runtime = (HttpRuntime)typeof(HttpRuntime).InvokeMember("_theRuntime",
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField, null, null, null);
            if (runtime != null)
            {
                Type type = runtime.GetType();
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField;
                string shutDownMessage = (string)type.InvokeMember("_shutDownMessage", flags, null, runtime, null);
                string shutDownStack = (string)type.InvokeMember("_shutDownStack", flags, null, runtime, null);
                message = $"\r\nshutDownMessage:{shutDownMessage}\r\nshutDownStack:\r\n:{shutDownStack}";
            }
            LoggerDefault.Default.Custom("AppStartEnd\\",
                "Application_End " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + message);
        }

        /// <summary>
        /// 每个用户请求时的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void Application_BeginRequest(object sender, EventArgs e)
        {
        }


        /// <summary>
        /// 每个用户请求结束时的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void Application_EndRequest(object sender, EventArgs e)
        {
            //释放保存在HttpContext中的某些IDisponse对象
            DisponseContextData();
            DateTime now = DateTime.Now;

            #region 超过1秒时且不是凌晨5点，记录请求结束时间

            double usingTime = (now - HttpContext.Current.Timestamp).TotalMilliseconds;
            if (usingTime > 1000 && now.Hour != 5)
            {
                string time2 = $@"End:{now.ToString("HH:mm:ss.fff")} 
use time:{usingTime.ToString("N0")}ms, Post len:{Convert.ToString(Request.Form).Length.ToString()}
";
                LoggerDefault.Default.Custom("All\\LongTime", time2);
            }

            #endregion


            string url = Request.Url.ToString().ToLower();
            var isMonitor = url.IndexOf("iswebmon=", StringComparison.Ordinal) >= 0;
            if (isMonitor)
            {
                // 如果是监控请求，输出本次响应的完整耗时，以供问题排查
                Response.Write("耗时:" + usingTime.ToString("N0") + "毫秒");
            }

            //// 记录活动时间，用于判断站点是否被用户使用中（这些判断代码注意要屏蔽测试页面）
            //if (!isMonitor && // 站点监控程序访问，不作为用户
            //    url.IndexOf("/checkipinfo.aspx", StringComparison.Ordinal) < 0 && // 前端轮询时，不作为用户
            //    url.IndexOf("/z.aspx", StringComparison.Ordinal) < 0)
            //{
            //    LAST_ACCESS_TIME_KEY = now;
            //    Interlocked.Increment(ref AccessCount);
            //}
        }

        /// <summary>
        /// webform出错时的处理方法。
        /// 注：仅支持*.aspx和*.ashx请求，
        ///     不支持MVC和WebApi请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError().InnerException ?? Server.GetLastError();
            if (ex is ThreadAbortException)
            {
                // 不记录Response.End引发的异常
                Thread.ResetAbort();
                HttpContext.Current.ClearError();
                return;
            }
            if (ex is HttpException exp404)
            {
                int erCode = exp404.GetHttpCode();
                if (erCode == 404 || erCode == 400)
                {
                    LoggerDefault.Default.Custom("err\\" + erCode.ToString(), ex.Message);
                    ClearErr();
                    return;
                }
            }
            string msg = $"\r\nGlobal异常: Post数据:{Request.Form}\r\n";
            if (ex is HttpRequestValidationException)
            {
                LoggerDefault.Default.Custom("err\\Validation", msg);
                ClearErr();
                return;
            }
            LoggerDefault.Default.Error(msg + ex);
            ClearErr();
        }

        #endregion

        /// <summary>
        /// 清除http请求错误，并跳转到指定错误页面
        /// </summary>
        void ClearErr()
        {
#if !DEBUG
            try
            {
                HttpContext.Current.ClearError();
                Response.Redirect("http://beinet.cn/404.htm", false);
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
#endif
        }
        private void DisponseContextData()
        {
            try
            {
                foreach (var item in HttpContext.Current.Items.Values)
                {
                    if (item is IDisposable disposableItem)
                        disposableItem.Dispose();
                    else
                    {
                        var dict = item as IDictionary;
                        if (dict?.Values != null)
                        {
                            foreach (var dictValue in dict.Values)
                            {
                                (dictValue as IDisposable)?.Dispose();
                            }
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                LoggerDefault.Default.Error("释放异常:" + exp);
            }
        }

    }
}
