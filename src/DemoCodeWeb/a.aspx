<%@Page Language="C#" EnableSessionState="false" EnableViewState="false" Async="true" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Net" %>
<%@ Import Namespace="System.Net.Security" %>
<%@ Import Namespace="System.Security.Cryptography.X509Certificates" %>
<script language="c#" runat="server">

    /// <summary>
    /// 把Beinet.Request.dll复制到bin目录下，会自动替换HttpWebRequest类，
    /// 无侵入，不用修改业务代码
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void Page_Load(object sender, EventArgs args)
    {
        Response.ContentType = "text/plain";
        Response.Write(DateTime.Now);
        var ret = GetPage("http://baidu.com");
        Response.Write(ret);
        //await abcd.xxx();
    }

    // POST获取网页内容
    static string GetPage(string url)
    {
        ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;
        HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
        request = WebRequest.CreateHttp(url);
        request.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");
        request.Headers.Add("Accept-Charset", "utf-8");
        request.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0;)";
        request.AllowAutoRedirect = true; //出现301或302之类的转向时，是否要转向
        request.Method = "GET";
        using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
        {
            var ret = new StringBuilder();
            ret.AppendFormat("请求头信息：\r\n{0}\r\n\r\n响应头信息：\r\n{1}\r\n\r\n响应内容:\r\n",
                request.Headers, response.Headers);
            using (Stream stream = response.GetResponseStream())
            {
                if (stream == null)
                    return "";
                using (var sr = new StreamReader(stream, Encoding.UTF8))
                {
                    ret.Append(sr.ReadToEnd());
                }
            }
            return ret.ToString();
        }
    }

    static bool CheckValidationResult(object sender, X509Certificate certificate,
        X509Chain chain, SslPolicyErrors errors)
    {
        return true; //总是接受
    }
</script>