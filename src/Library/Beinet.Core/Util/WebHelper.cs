using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web.UI;
using System.IO;
using System.Threading.Tasks;
using System.Web;

namespace Beinet.Core.Util
{
    /// <summary>
    /// 
    /// </summary>
    public static class WebHelper
    {
        private static readonly Encoding Utf8 = FileHelper.UTF8_NoBom;
        #region HttpContext相关

        /// <summary>
        /// 获取 Cookies值
        /// </summary>
        /// <param name="cookKey">Cookies 名称</param>
        public static string GetCookies(string cookKey)
        {
            var cook = HttpContext.Current?.Request.Cookies[cookKey];
            if (cook == null)
            {
                return string.Empty;
            }
            return cook.Value ?? string.Empty;
        }

        /// <summary>
        /// 设置Cookies,
        /// </summary>
        /// <param name="cookKey">Cookies 名称</param>
        /// <param name="value">Cookies 值</param>
        /// <param name="domain">Cookies 关联的域, 如：“.beinet.cn”</param>
        /// <param name="expires">Cookies过期时间</param>
        public static void SetCookies(string cookKey, string value, string domain = null, DateTime? expires = null)
        {
            // Response为空时，让它抛出异常
            var response = HttpContext.Current.Response;

            // 添加过，不再添加，避免header里多次set cookie
            if (response.Cookies.AllKeys.Contains(cookKey))
            {
                return;
            }

            var cookie = new HttpCookie(cookKey)
            {
                Value = value,
                HttpOnly = true
            };
            if (!string.IsNullOrEmpty(domain))
            {
                cookie.Domain = domain;
            }
            if (expires != null)
            {
                cookie.Expires = expires.Value;
            }
            HttpContext.Current.Response.Cookies.Add(cookie);
        }


        /// <summary>
        /// 返回 Web服务器控件的HTML 输出
        /// </summary>
        /// <param name="ctl"></param>
        /// <returns></returns>
        public static string GetHtml(Control ctl)
        {
            if (ctl == null)
                return string.Empty;

            using (StringWriter sw = new StringWriter())
            using (HtmlTextWriter htw = new HtmlTextWriter(sw))
            {
                ctl.RenderControl(htw);
                return sw.ToString();
            }
        }

        /// <summary>
        /// 获取当前访问的页面的完整URL，如http://www.a.com/dir/a.aspx
        /// </summary>
        /// <param name="getQueryString"></param>
        /// <returns></returns>
        public static string GetUrl(bool getQueryString = true)
        {
            string url = HttpContext.Current.Request.ServerVariables["SERVER_NAME"];

            if (HttpContext.Current.Request.ServerVariables["SERVER_PORT"] != "80")
                url += ":" + HttpContext.Current.Request.ServerVariables["SERVER_PORT"];
            //strTemp = strTemp & CheckStr(HttpContext.Current.Request.ServerVariables("URL")) 

            url += HttpContext.Current.Request.ServerVariables["SCRIPT_NAME"];

            if (getQueryString)
            {
                if (HttpContext.Current.Request.QueryString.ToString() != "")
                {
                    url += "?" + HttpContext.Current.Request.QueryString;
                }
            }

            string https = HttpContext.Current.Request.ServerVariables["HTTPS"];
            if (string.IsNullOrEmpty(https) || https == "off")
            {
                url = "http://" + url;
            }
            else
            {
                url = "https://" + url;
            }
            return url;
        }

        /// <summary>
        /// 从url中截取出域名和端口
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetHostFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                url = url.Substring("http://".Length);
            }
            else if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = url.Substring("https://".Length);
            }

            var idx = url.IndexOf('/');
            if (idx <= 0)
            {
                return string.Empty;
            }
            return url.Substring(0, idx);
        }

        #endregion


        #region WebRequest方法相关
        private static CookieContainer _cookie = new CookieContainer();


        #region 文件下载上传相关

        /// <summary>
        /// 文件下载
        /// </summary>
        /// <param name="downloadUrl"></param>
        /// <param name="savePath"></param>
        /// <param name="refererUrl"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="proxy"></param>
        /// <param name="userAgent"></param>
        /// <param name="requestTimeOut">WebRequest的最大请求时间，单位：毫秒，0为不设置</param>
        /// <param name="allowRedirect"></param>
        /// <param name="downsize">分块下载每次下载字节数，小于等于0时表示不分块下载，默认每次1M</param>
        public static void DownloadFile(string downloadUrl, string savePath,
            string refererUrl = null, string userName = null, string password = null, string proxy = null,
            string userAgent = null, int requestTimeOut = 0, bool allowRedirect = false, int downsize = 1024 * 1024)
        {
            DownloadFile(downloadUrl, savePath, ref _cookie, refererUrl, userName, password, proxy, userAgent,
                         requestTimeOut, allowRedirect, downsize);
        }


        /// <summary>
        /// 文件下载(注：如果savePath已经存在，会被先删除)
        /// </summary>
        /// <param name="downloadUrl"></param>
        /// <param name="savePath"></param>
        /// <param name="cookieContainer"></param>
        /// <param name="refererUrl"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="proxy"></param>
        /// <param name="userAgent"></param>
        /// <param name="requestTimeOut">WebRequest的最大请求时间，单位：毫秒，0为不设置</param>
        /// <param name="allowRedirect"></param>
        /// <param name="downsize">分块下载时，每次下载字节数，小于等于0时表示不分块下载</param>
        public static void DownloadFile(string downloadUrl, string savePath, ref CookieContainer cookieContainer,
            string refererUrl = null, string userName = null, string password = null, string proxy = null,
            string userAgent = null, int requestTimeOut = 0, bool allowRedirect = false, int downsize = 1024 * 1024)
        {
            // 先删除源文件，避免下载追加到源文件后面
            if (File.Exists(savePath))
                File.Delete(savePath);

            var begin = 0L;
            var total = -1L;
            var complete = false;
            byte[] buffer = null;
            while (!complete)
            {
                HttpWebRequest request = CreateRequest(downloadUrl, ref cookieContainer, refererUrl, userName, password, proxy,
                                        userAgent, requestTimeOut, allowRedirect);

                // 分块下载
                if (downsize > 0)
                    request.AddRange((int)begin, (int)begin + downsize - 1);

                int readSize = 0;
                using (FileStream stream = GetWriteStream(savePath))
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var responseStream = GetResponseStream(response))
                {
                    if (responseStream == null)
                        return;
                    if (total == -1)
                    {
                        var range = response.Headers["Content-Range"] != null
                                        ? response.Headers["Content-Range"].Split('/')[1]
                                        : string.Empty;
                        if (string.IsNullOrEmpty(range) || !long.TryParse(range, out total))
                        {
                            total = response.ContentLength; //stream.Length
                        }
                    }

                    #region 保存到文件
                    int read;
                    if (downsize <= 0 || (total > 0 && downsize > total))
                        downsize = (int)total;

                    if (buffer == null)
                        buffer = new byte[downsize];
                    while ((read = responseStream.Read(buffer, 0, buffer.Length)) != 0) //&& s < l
                    {
                        stream.Write(buffer, 0, read);
                        stream.Flush();
                        readSize += read;
                        // 计算下载百分比
                        //var percent = ((begin + readSize) * 100 / (decimal)total).ToString("N");
                    }
                    #endregion
                }// end using

                begin += readSize;//downsize;
                if (begin >= total)
                    complete = true;
            }// end while
        }


        /// <summary>
        /// 模拟post文件
        /// </summary>
        /// <param name="strFileToUpload"></param>
        /// <param name="strUrl"></param>
        /// <param name="returnContentEncode">返回内容编码</param>
        /// <returns></returns>
        public static string PostFile(string strFileToUpload, string strUrl, Encoding returnContentEncode)
        {
            string strFileFormName = "file";
            string strBoundary = "----------" + DateTime.Now.Ticks.ToString("x");

            // The trailing boundary string  
            byte[] boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + strBoundary + "\r\n");

            // The post message header  
            StringBuilder sb = new StringBuilder();
            sb.Append("--");
            sb.Append(strBoundary);
            sb.Append("\r\n");
            sb.Append("Content-Disposition: form-data; name=\"");
            sb.Append(strFileFormName);
            sb.Append("\"; filename=\"");
            sb.Append(Path.GetFileName(strFileToUpload));
            sb.Append("\"");
            sb.Append("\r\n");
            sb.Append("Content-Type: ");
            sb.Append("application/octet-stream");
            sb.Append("\r\n");
            sb.Append("\r\n");
            string strPostHeader = sb.ToString();
            byte[] postHeaderBytes = Utf8.GetBytes(strPostHeader);

            // The WebRequest  
            HttpWebRequest oWebrequest = null;

            try
            {
                oWebrequest = CreateRequest(strUrl, ref _cookie);
                oWebrequest.ContentType = "multipart/form-data; boundary=" + strBoundary;
                oWebrequest.Method = "POST";

                // This is important, otherwise the whole file will be read to memory anyway...  
                oWebrequest.AllowWriteStreamBuffering = false;

                // Get a FileStream and set the final properties of the WebRequest  
                using (FileStream oFileStream = new FileStream(strFileToUpload, FileMode.Open, FileAccess.Read))
                {
                    long length = postHeaderBytes.Length + oFileStream.Length + boundaryBytes.Length;
                    oWebrequest.ContentLength = length;
                    using (Stream oRequestStream = oWebrequest.GetRequestStream())
                    {
                        // Write the post header  
                        oRequestStream.Write(postHeaderBytes, 0, postHeaderBytes.Length);

                        // Stream the file contents in small pieces (4096 bytes, max).  
                        byte[] buffer = new Byte[checked((uint)Math.Min(4096, (int)oFileStream.Length))];
                        int bytesRead;
                        while ((bytesRead = oFileStream.Read(buffer, 0, buffer.Length)) != 0)
                            oRequestStream.Write(buffer, 0, bytesRead);

                        // Add the trailing boundary  
                        oRequestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
                    }
                }

                using (HttpWebResponse oWResponse = (HttpWebResponse)oWebrequest.GetResponse())
                    return GetResponseString(oWResponse, returnContentEncode);
            }
            catch (Exception)
            {
                return "";
            }
            finally
            {
                oWebrequest?.Abort();
            }
        }
        #endregion


        /// <summary>
        /// 获取响应header指定key的值
        /// </summary>
        /// <param name="url"></param>
        /// <param name="key"></param>
        /// <param name="proxy">代理(xxx.xxx.xxx.xxx:xxx)</param>
        /// <returns></returns>
        public static string GetResponseHeader(string url, string key, string proxy = null)
        {
            try
            {
                HttpWebRequest req = CreateRequest(url, ref _cookie);
                if (!string.IsNullOrEmpty(proxy))
                {
                    string[] ipPort = proxy.Split(':');
                    if (ipPort.Length == 2)
                    {
                        if (ipPort[0].StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                            ipPort[0] = ipPort[0].Substring(7);
                        else if (ipPort[0].StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                            ipPort[0] = ipPort[0].Substring(8);
                        if (!int.TryParse(ipPort[1], out var port))
                            port = 80;
                        req.Proxy = new WebProxy(ipPort[0], port);
                    }
                }

                req.Method = "HEAD";
                using (var response = (HttpWebResponse) req.GetResponse())
                {
                    return response.GetResponseHeader(key);
                }
            }
            catch (Exception)
            {
                return "";
            }
        }


        #region 获取string的方法
        /// <summary>
        /// 抓取页面
        /// </summary>
        /// <param name="url">要抓取的网址</param>
        /// <param name="para">参数</param>
        /// <param name="httpMethod">GET POST</param>
        /// <param name="encoding">编码格式，默认UTF-8</param>
        /// <param name="proxy">WebRequest要用到的代理</param>
        /// <param name="catchExp">是否捕获异常,true时不抛异常，返回错误文本</param>
        /// <param name="paramBytes">bytes类型的参数</param>
        /// <returns></returns>
        public static string GetPage(string url, string para, string httpMethod, string proxy, Encoding encoding, bool catchExp = true, byte[] paramBytes = null)
        {
            return GetPage(url, proxy: proxy, param: para, HttpMethod: httpMethod, encoding: encoding, catchExp: catchExp, paramBytes: paramBytes);
        }

        /// <summary>
        /// 抓取页面
        /// </summary>
        /// <param name="url">要抓取的网址</param>
        /// <param name="param">参数</param>
        /// <param name="HttpMethod">GET POST</param>
        /// <param name="encoding">编码格式，默认UTF-8</param>
        /// <param name="refererUrl">要设置的头信息的引用页</param>
        /// <param name="showHeader">返回内容是否包括头信息</param>
        /// <param name="userName">网页登录名</param>
        /// <param name="password">登录密码</param>
        /// <param name="proxy">WebRequest要用到的代理</param>
        /// <param name="userAgent">要设置的头信息里的用户代理</param>
        /// <param name="requestTimeOut">WebRequest的最大请求时间，单位：毫秒，0为不设置</param>
        /// <param name="allowRedirect">出现301或302之类的转向时，是否要转向</param>
        /// <param name="headers">要设置的Header键值对</param>
        /// <param name="catchExp">是否捕获异常,true时不抛异常，返回错误文本</param>
        /// <param name="paramBytes">bytes类型的参数</param>
        /// <returns></returns>
        public static string GetPage(string url,
            string param = null, string HttpMethod = null, Encoding encoding = null, string refererUrl = null,
            bool showHeader = false, string userName = null, string password = null, string proxy = null,
            string userAgent = null, int requestTimeOut = 0, bool allowRedirect = false,
            Dictionary<string, string> headers = null, bool catchExp = true, byte[] paramBytes = null)
        {
            return GetPage(url, out _, ref _cookie, out _, out _, param, HttpMethod, encoding,
                refererUrl, showHeader, userName, password, proxy, userAgent, requestTimeOut, allowRedirect, headers, catchExp, paramBytes);
        }

        /// <summary>
        /// 抓取页面
        /// </summary>
        /// <param name="url">要抓取的网址</param>
        /// <param name="isok">返回抓取结果，成功还是失败</param>
        /// <param name="processtime">返回抓取页面耗费的时间，毫秒</param>
        /// <param name="responseStatus">响应状态码，如200、302、503；600表示web异常；700表示其它异常</param>
        /// <param name="param">参数</param>
        /// <param name="HttpMethod">GET POST</param>
        /// <param name="encoding">编码格式，默认UTF-8</param>
        /// <param name="refererUrl">要设置的头信息的引用页</param>
        /// <param name="showHeader">返回内容是否包括头信息</param>
        /// <param name="userName">网页登录名</param>
        /// <param name="password">登录密码</param>
        /// <param name="proxy">WebRequest要用到的代理</param>
        /// <param name="userAgent">要设置的头信息里的用户代理</param>
        /// <param name="requestTimeOut">WebRequest的最大请求时间，单位：毫秒，0为不设置</param>
        /// <param name="allowRedirect">出现301或302之类的转向时，是否要转向</param>
        /// <param name="headers">要设置的Header键值对</param>
        /// <param name="catchExp">是否捕获异常,true时不抛异常，返回错误文本</param>
        /// <param name="paramBytes">bytes类型的参数</param>
        /// <returns></returns>
        public static string GetPage(string url, out bool isok, out long processtime, out int responseStatus,
            string param = null, string HttpMethod = null, Encoding encoding = null, string refererUrl = null,
            bool showHeader = false, string userName = null, string password = null, string proxy = null,
            string userAgent = null, int requestTimeOut = 0, bool allowRedirect = false,
            Dictionary<string, string> headers = null, bool catchExp = true, byte[] paramBytes = null)
        {
            return GetPage(url, out isok, ref _cookie, out processtime, out responseStatus, param, HttpMethod, encoding,
                refererUrl, showHeader, userName, password, proxy, userAgent, requestTimeOut, allowRedirect, headers, catchExp, paramBytes);
        }

        // 主调方法
        /// <summary>
        /// 抓取页面
        /// </summary>
        /// <param name="url">要抓取的网址</param>
        /// <param name="isok">返回抓取结果，成功还是失败</param>
        /// <param name="cookieContainer">要使用的cookie</param>
        /// <param name="processtime">返回抓取页面耗费的时间，毫秒</param>
        /// <param name="responseStatus">响应状态码，如200、302、503；600表示web异常；700表示其它异常</param>
        /// <param name="param">参数</param>
        /// <param name="HttpMethod">GET POST</param>
        /// <param name="encoding">编码格式，默认UTF-8</param>
        /// <param name="refererUrl">要设置的头信息的引用页</param>
        /// <param name="showHeader">返回内容是否包括头信息</param>
        /// <param name="userName">网页登录名</param>
        /// <param name="password">登录密码</param>
        /// <param name="proxy">WebRequest要用到的代理</param>
        /// <param name="userAgent">要设置的头信息里的用户代理</param>
        /// <param name="requestTimeOut">WebRequest的最大请求时间，单位：毫秒，0为不设置</param>
        /// <param name="allowRedirect">出现301或302之类的转向时，是否要转向</param>
        /// <param name="headers">要设置的Header键值对</param>
        /// <param name="catchExp">是否捕获异常,true时不抛异常，返回错误文本</param>
        /// <param name="paramBytes">bytes类型的参数</param>
        /// <returns></returns>
        public static string GetPage(string url, out bool isok, ref CookieContainer cookieContainer, out long processtime, out int responseStatus,
            string param = null, string HttpMethod = null, Encoding encoding = null, string refererUrl = null,
            bool showHeader = false, string userName = null, string password = null, string proxy = null,
            string userAgent = null, int requestTimeOut = 0, bool allowRedirect = false,
            Dictionary<string, string> headers = null, bool catchExp = true, byte[] paramBytes = null)
        {
            isok = false;

            InitPara(HttpMethod, param, ref url, ref encoding);

            // 必须在写入Post Stream之前设置Proxy
            HttpWebRequest request = CreateRequest(url, ref cookieContainer, refererUrl, userName, password, proxy,
                                                   userAgent, requestTimeOut, allowRedirect);

            // 初始化request的其它属性
            InitRequest(request, encoding, headers, HttpMethod, param, paramBytes);
            
            HttpWebResponse response;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                response = (HttpWebResponse)request.GetResponse();
                sw.Stop();
                processtime = sw.ElapsedMilliseconds;
            }

            #region WebRequest异常处理

            catch (WebException webExp)
            {
                sw.Stop();
                processtime = sw.ElapsedMilliseconds;

                if (!catchExp)
                    throw;
                if (webExp.Response != null)
                {
                    using (var responseErr = (HttpWebResponse)webExp.Response)
                    {
                        responseStatus = (int)responseErr.StatusCode;
                        var html = GetResponseString(responseErr, encoding);
                        if (showHeader)
                            html = "请求头信息：\r\n" + request.Headers + "\r\n\r\n响应头信息：\r\n" + responseErr.Headers +
                                   "\r\n\r\n响应内容:\r\n" +
                                   html;
                        return html;
                    }
                }
                else
                {
                    responseStatus = 600;
                }
                return "返回错误：" + webExp;
            }
            catch (Exception exp)
            {
                sw.Stop();
                processtime = sw.ElapsedMilliseconds;
                responseStatus = 700;
                if (!catchExp)
                    throw;
                return "返回错误：" + exp;
            }

            #endregion

            responseStatus = (int)response.StatusCode;
            try
            {
                var html = GetResponseString(response, encoding);
                isok = true;
                if (showHeader)
                    html = "请求头信息：\r\n" + request.Headers + "\r\n\r\n响应头信息：\r\n" + response.Headers +
                           "\r\n\r\n响应内容:\r\n" +
                           html;
                return html;
            }
            catch (Exception exp)
            {
                responseStatus = 700;
                if (!catchExp)
                    throw;
                return "返回错误：" + exp;
            }
            finally
            {
                response.Dispose();
            }

        }

        // 主调方法
        /// <summary>
        /// 抓取页面
        /// </summary>
        /// <param name="url">要抓取的网址</param>
        /// <param name="param">参数</param>
        /// <param name="HttpMethod">GET POST</param>
        /// <param name="encoding">编码格式，默认UTF-8</param>
        /// <param name="refererUrl">要设置的头信息的引用页</param>
        /// <param name="showHeader">返回内容是否包括头信息</param>
        /// <param name="userName">网页登录名</param>
        /// <param name="password">登录密码</param>
        /// <param name="proxy">WebRequest要用到的代理</param>
        /// <param name="userAgent">要设置的头信息里的用户代理</param>
        /// <param name="requestTimeOut">WebRequest的最大请求时间，单位：毫秒，0为不设置</param>
        /// <param name="allowRedirect">出现301或302之类的转向时，是否要转向</param>
        /// <param name="headers">要设置的Header键值对</param>
        /// <param name="catchExp">是否捕获异常,true时不抛异常，返回错误文本</param>
        /// <param name="paramBytes">bytes类型的参数</param>
        /// <returns></returns>
        public static async Task<string> GetPageAsync(string url,
            string param = null, string HttpMethod = null, Encoding encoding = null, string refererUrl = null,
            bool showHeader = false, string userName = null, string password = null, string proxy = null,
            string userAgent = null, int requestTimeOut = 0, bool allowRedirect = false,
            Dictionary<string, string> headers = null, bool catchExp = true, byte[] paramBytes = null)
        {
            InitPara(HttpMethod, param, ref url, ref encoding);

            CookieContainer cookieContainer = _cookie;
            HttpWebRequest request = CreateRequest(url, ref cookieContainer, refererUrl, userName, password, proxy,
                                                   userAgent, requestTimeOut, allowRedirect);

            // 初始化request的其它属性
            InitRequest(request, encoding, headers, HttpMethod, param, paramBytes);

            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false);
            }
            #region WebRequest异常处理
            catch (WebException webExp)
            {
                if (!catchExp)
                    throw;
                if (webExp.Response != null)
                {
                    using (var responseErr = (HttpWebResponse)webExp.Response)
                    {
                        var html = GetResponseString(responseErr, encoding);
                        if (showHeader)
                            html = "请求头信息：\r\n" + request.Headers + "\r\n\r\n响应头信息：\r\n" + responseErr.Headers +
                                   "\r\n\r\n响应内容:\r\n" +
                                   html;
                        return html;
                    }
                }
                return "返回错误：" + webExp;
            }
            catch (Exception exp)
            {
                return "返回错误：" + exp;
            }
            #endregion

            try
            {
                var html = GetResponseString(response, encoding);
                if (showHeader)
                    html = "请求头信息：\r\n" + request.Headers + "\r\n\r\n响应头信息：\r\n" + response.Headers +
                           "\r\n\r\n响应内容:\r\n" +
                           html;
                return html;
            }
            catch (Exception exp)
            {
                if (!catchExp)
                    throw;
                return "返回错误：" + exp;
            }
            finally
            {
                response.Dispose();
            }
        }
        #endregion



        #region 获取byte[]的方法
        // 主调方法
        /// <summary>
        /// 抓取页面
        /// </summary>
        /// <param name="url">要抓取的网址</param>
        /// <param name="isok">返回抓取结果，成功还是失败</param>
        /// <param name="cookieContainer">要使用的cookie</param>
        /// <param name="processtime">返回抓取页面耗费的时间，毫秒</param>
        /// <param name="responseStatus">响应状态码，如200、302、503；600表示web异常；700表示其它异常</param>
        /// <param name="param">参数</param>
        /// <param name="HttpMethod">GET POST</param>
        /// <param name="encoding">编码格式，默认UTF-8</param>
        /// <param name="refererUrl">要设置的头信息的引用页</param>
        /// <param name="userName">网页登录名</param>
        /// <param name="password">登录密码</param>
        /// <param name="proxy">WebRequest要用到的代理</param>
        /// <param name="userAgent">要设置的头信息里的用户代理</param>
        /// <param name="requestTimeOut">WebRequest的最大请求时间，单位：毫秒，0为不设置</param>
        /// <param name="allowRedirect">出现301或302之类的转向时，是否要转向</param>
        /// <param name="headers">要设置的Header键值对</param>
        /// <param name="catchExp">是否捕获异常,true时不抛异常，返回错误文本</param>
        /// <param name="paramBytes">bytes类型的参数</param>
        /// <returns></returns>
        public static byte[] GetPageBinary(string url, out bool isok, ref CookieContainer cookieContainer, out long processtime, out int responseStatus,
            string param = null, string HttpMethod = null, Encoding encoding = null, string refererUrl = null,
            string userName = null, string password = null, string proxy = null,
            string userAgent = null, int requestTimeOut = 0, bool allowRedirect = false, Dictionary<string, string> headers = null, bool catchExp = true, byte[] paramBytes = null)
        {
            isok = false;
            InitPara(HttpMethod, param, ref url, ref encoding);

            HttpWebRequest request = CreateRequest(url, ref cookieContainer, refererUrl, userName, password, proxy,
                                                   userAgent, requestTimeOut, allowRedirect);

            // 初始化request的其它属性
            InitRequest(request, encoding, headers, HttpMethod, param, paramBytes);


            HttpWebResponse response;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                response = (HttpWebResponse)request.GetResponse();
                sw.Stop();
                processtime = sw.ElapsedMilliseconds;
            }
            #region WebRequest异常处理
            catch (WebException webExp)
            {
                sw.Stop();
                processtime = sw.ElapsedMilliseconds;
                if (!catchExp)
                    throw;
                if (webExp.Response != null)
                {
                    using (var responseErr = (HttpWebResponse)webExp.Response)
                    {
                        responseStatus = (int)responseErr.StatusCode;
                        var html = GetResponseString(responseErr, encoding);
                        return encoding.GetBytes(html);
                    }
                }
                else
                {
                    responseStatus = 600;
                }
                return encoding.GetBytes("返回错误：" + webExp);
            }
            catch (Exception exp)
            {
                sw.Stop();
                processtime = sw.ElapsedMilliseconds;
                responseStatus = 700;
                if (!catchExp)
                    throw;
                return encoding.GetBytes("返回错误：" + exp);
            }
            #endregion

            responseStatus = (int)response.StatusCode;
            try
            {
                byte[] buffer = new byte[16 * 1024];
                using (Stream stream = GetResponseStream(response))
                using (MemoryStream ms = new MemoryStream())
                {
                    if (stream == null)
                        return encoding.GetBytes("GetResponseStream is null");

                    isok = true;
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, read);
                    }
                    return ms.ToArray();

                    // 下面的方法ContentLength可能为-1(gzip时)，导致new byte时报算术溢出
                    // 即使ContentLength正确，Read时也可能读取2遍以上，所以使用上面的循环来读取返回值
                    //int len = (int)response.ContentLength; //stream.Length;
                    //byte[] arr = new byte[len];
                    //stream.Read(arr, 0, len);
                }
            }
            catch (Exception exp)
            {
                responseStatus = 700;
                if (!catchExp)
                    throw;
                return encoding.GetBytes("返回错误：" + exp);
            }
            finally
            {
                response.Dispose();            // 关闭stream即可
            }
        }

        /// <summary>
        /// 抓取页面
        /// </summary>
        /// <param name="url">要抓取的网址</param>
        /// <param name="isok">返回抓取结果，成功还是失败</param>
        /// <param name="processtime">返回抓取页面耗费的时间，毫秒</param>
        /// <param name="responseStatus">响应状态码，如200、302、503；600表示web异常；700表示其它异常</param>
        /// <param name="param">参数</param>
        /// <param name="HttpMethod">GET POST</param>
        /// <param name="encoding">编码格式，默认UTF-8</param>
        /// <param name="refererUrl">要设置的头信息的引用页</param>
        /// <param name="userName">网页登录名</param>
        /// <param name="password">登录密码</param>
        /// <param name="proxy">WebRequest要用到的代理</param>
        /// <param name="userAgent">要设置的头信息里的用户代理</param>
        /// <param name="requestTimeOut">WebRequest的最大请求时间，单位：毫秒，0为不设置</param>
        /// <param name="allowRedirect">出现301或302之类的转向时，是否要转向</param>
        /// <param name="headers">要设置的Header键值对</param>
        /// <param name="catchExp">是否捕获异常,true时不抛异常，返回错误文本</param>
        /// <returns></returns>
        public static byte[] GetPageBinary(string url, out bool isok, out long processtime, out int responseStatus,
            string param = null, string HttpMethod = null, Encoding encoding = null, string refererUrl = null,
            string userName = null, string password = null, string proxy = null,
            string userAgent = null, int requestTimeOut = 0, bool allowRedirect = false, Dictionary<string, string> headers = null, bool catchExp = true)
        {
            return GetPageBinary(url, out isok, ref _cookie, out processtime, out responseStatus, param, HttpMethod, encoding,
                refererUrl, userName, password, proxy, userAgent, requestTimeOut, allowRedirect, headers, catchExp);
        }

        #endregion



        #region 用于代码重用的私有方法列表
        static void InitPara(string HttpMethod, string param,
            ref string url, ref Encoding encoding)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("网址不允许为空", nameof(url));
            }


            encoding = encoding ?? Utf8;
            var isGet = (string.IsNullOrEmpty(HttpMethod) ||
                          HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase));

            // 删除网址后面的#号
            var idx = url.IndexOf('#');
            if (idx >= 0)
                url = url.Substring(0, idx);

            // Get方式，且有参数时，把参数拼接到Url后面
            if (isGet && !string.IsNullOrEmpty(param))
                if (url.IndexOf('?') < 0)
                    url += "?" + param;
                else
                    url += "&" + param;

        }

        static HttpWebRequest CreateRequest(string url, ref CookieContainer cookieContainer,
            string refererUrl = null, string userName = null, string password = null, string proxy = null,
            string userAgent = null, int requestTimeOut = 0, bool allowRedirect = false, bool enableGzip = true)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("网址不允许为空", nameof(url));
            }

            // 访问Https网站时，加上特殊处理，用于处理证书有问题的网站
            bool isHttps = url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
            if (isHttps)
                ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;
            else if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                url = "http://" + url; // 只支持http和https协议，不考虑其它协议支持

            // 用于读取当前上下文里的旧数据
            HttpRequest originalRequest;
            try
            {
                originalRequest = HttpContext.Current?.Request;
            }
            catch
            {
                // 避免在Application_Start里调用时，无法访问Request导致出错
                originalRequest = null;
            }
            // 拼接并转发前端扔过来的region
            var region = originalRequest?.QueryString["region"];
            if (!string.IsNullOrEmpty(region))
            {
                if (url.IndexOf('?') < 0)
                    url += "?region=" + region;
                else
                    url += "&region=" + region;
            }

            HttpWebRequest request = (HttpWebRequest)System.Net.WebRequest.Create(url);
            // 禁止缓存可能导致的bug
            request.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");

            if (!string.IsNullOrEmpty(userName) || !string.IsNullOrEmpty(password))
                request.Credentials = new NetworkCredential(userName, password);

            #region 加Cookie
            if (cookieContainer == null)
            {
                cookieContainer = _cookie;
            }
            request.CookieContainer = cookieContainer;
            //            request.CookieContainer.SetCookies(new Uri(url), "aaa=bbb&ccc=ddd");// 必须一次全部加入Cookie
            #endregion


            #region 添加其它头

            // UserAgent设置
            if (!string.IsNullOrEmpty(userAgent))
                request.UserAgent = userAgent;
            else
            {
                if (originalRequest != null && !string.IsNullOrEmpty(originalRequest.UserAgent))
                {
                    var addUG = "(Beinet1.0)";
                    var ua = originalRequest.UserAgent;
                    if (ua.IndexOf(addUG, StringComparison.Ordinal) < 0)
                        ua += addUG;
                    request.UserAgent = ua;
                }
                else
                    request.UserAgent = "Beinet.Core 1.0";
            }

            // referer设置
            if (!string.IsNullOrEmpty(refererUrl))
                request.Referer = refererUrl;
            else if (originalRequest != null)
                request.Referer = originalRequest.RawUrl;
            
            // 请求链路跟踪ID
            request.Headers.Add("M-Request-Id",
                originalRequest?.Headers.Get("M-Request-Id")
                ?? originalRequest?.QueryString.Get("M-Request-Id")
                ?? Guid.NewGuid().ToString()
            );
            // 请求发出时间，Server可以用此时间排查网络耗时
            request.Headers.Add("M-Request-Seq",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
            );
            var mRequestDepth = originalRequest?.Headers.Get("M-Request-Depth")
                ?? originalRequest?.QueryString.Get("M-Request-Depth") ?? "0";
            int.TryParse(mRequestDepth, out int intDepth);
            // 当前请求位于整个链路的层级
            request.Headers.Add("M-Request-Depth", (intDepth + 1).ToString());
            // nginx扔过来的客户端原始IP
            request.Headers.Add("M-Real-IP", originalRequest?.Headers.Get("M-Real-IP") ?? "");

            #endregion

            if (requestTimeOut > 0)
                request.Timeout = requestTimeOut; // 设置超时时间，默认值为 100,000 毫秒（100 秒）
            else
                request.Timeout = 5000; // 默认5秒超时

            request.AllowAutoRedirect = allowRedirect; //出现301或302之类的转向时，是否要转向

            //request.KeepAlive = true;
            request.Accept = "*/*";
            if (enableGzip)
                request.Headers.Add("Accept-Encoding", "gzip, deflate");
            //request.Headers.Add("Accept-Language", "zh-cn,en-us");

            if (!string.IsNullOrEmpty(proxy))
            {
                #region 设置代理
                string[] tmp = proxy.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                int port = 80;
                if (tmp.Length >= 2)
                {
                    if (!int.TryParse(tmp[1], out port))
                    {
                        port = 80;
                    }
                }

                if (tmp[0].StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    tmp[0] = tmp[0].Substring(7);
                else if (tmp[0].StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    tmp[0] = tmp[0].Substring(8);

                request.Proxy = new WebProxy(tmp[0], port);
                #endregion
            }
            //else request.Proxy = null; // 如果配置为null，Fiddler2将捕获不到请求
            return request;
        }

        static void InitRequest(HttpWebRequest request, Encoding encoding,
            Dictionary<string, string> headers, string method, string param, byte[] paramBytes)
        {
            if (request == null)
            {
                throw new ArgumentException("不允许为空", nameof(request));
            }
            method = (method ?? "GET").ToUpper();

            // if (Equals(encoding, Encoding.UTF8))
            request.Headers.Add("Accept-Charset", encoding.WebName);

            // 用于后面设置ContentType
            string contentType = null;
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> pair in headers)
                {
                    if (pair.Key == null || pair.Value == null)
                        continue;
                    if (pair.Key.Equals("ContentType", StringComparison.OrdinalIgnoreCase) ||
                        pair.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                        contentType = pair.Value;
                    else
                        request.Headers[pair.Key] = pair.Value; // 不用add，避免跟前面的key重复
                }
            }

            request.Method = method;
            if (method == "GET")
            {
                if (contentType != null)
                    request.ContentType = contentType;
            }
            else
            {
                // 未设置时，POST/PUT/DELETE默认采用form data方式
                request.ContentType = contentType ?? "application/x-www-form-urlencoded";

                // 设置提交的数据
                if (!string.IsNullOrEmpty(param) || paramBytes != null)
                {
                    // 把数据转换为字节数组
                    byte[] l_data = paramBytes ?? encoding.GetBytes(param);
                    // request.ContentLength = l_data.Length;  // 可以不设置，写流会自动设置Length
                    // 打开GetRequestStream之后，不允许设置ContentLength，会抛异常
                    // ContentLength设置后，reqStream.Close前必须写入相同字节的数据，否则Request会被取消
                    using (Stream newStream = request.GetRequestStream())
                    {
                        newStream.Write(l_data, 0, l_data.Length);
                    }
                }
                else
                    request.ContentLength = 0;// POST时，必须设置ContentLength属性
            }
        }

        static Stream GetResponseStream(HttpWebResponse response)
        {
            Stream stream = response.GetResponseStream();
            if (stream == null)
                return null;
            var contentEncoding = response.ContentEncoding;
            if (contentEncoding.IndexOf("gzip", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                stream = new GZipStream(stream, CompressionMode.Decompress);
            }
            else if (contentEncoding.IndexOf("deflate", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                stream = new DeflateStream(stream, CompressionMode.Decompress);
            }
            return stream;
        }

        /// <summary>
        /// 从HttpResposne中获取响应字符串
        /// </summary>
        /// <param name="response"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        static string GetResponseString(HttpWebResponse response, Encoding encoding)
        {
            using (Stream stream = response.GetResponseStream())
            {
                if (stream == null)
                    return "GetResponseStream is null";
                string str;
                string contentEncoding = response.ContentEncoding.ToLower();
                if (contentEncoding.Contains("gzip"))
                    using (Stream stream2 = new GZipStream(stream, CompressionMode.Decompress))
                        str = GetFromStream(stream2, encoding);
                else if (contentEncoding.Contains("deflate"))
                    using (Stream stream2 = new DeflateStream(stream, CompressionMode.Decompress))
                        str = GetFromStream(stream2, encoding);
                else
                    str = GetFromStream(stream, encoding);
                return str;
            }
        }

        static string GetFromStream(Stream stream, Encoding encoding)
        {
            using (StreamReader reader = new StreamReader(stream, encoding))
                return reader.ReadToEnd();
        }

        static FileStream GetWriteStream(string savePath)
        {
            FileStream stream;
            if (File.Exists(savePath))
            {
                stream = File.OpenWrite(savePath);
                stream.Seek(stream.Length, SeekOrigin.Current); //移动文件流中的当前指针 
            }
            else
            {
                string dir = Path.GetDirectoryName(savePath) ?? string.Empty;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                stream = new FileStream(savePath, FileMode.Create);
            }
            return stream;
        }
        #endregion


        /// <summary>
        /// 用于访问Https站点时，证书有问题，始终返回true
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            // Always accept
            //Console.WriteLine("accept" + certificate.GetName());
            return true; //总是接受
        }
        #endregion
    }
}
