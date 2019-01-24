<%@ Page Language="C#" ValidateRequest="false"%>
<%@ Import Namespace="System.Net" %>
<%@ Import Namespace="System.Net.Sockets" %>


<script type="text/C#" language="C#" runat="server">

    protected void Page_Load(object sender, EventArgs e)
    {
        Response.Cache.SetNoStore();

        Response.ContentType = "text/html";

        var isCurl = string.IsNullOrWhiteSpace(Request.UserAgent) || Request.UserAgent.IndexOf("curl", StringComparison.OrdinalIgnoreCase) >= 0;
        var newLine = isCurl ? "\n" : "<br/>";
        var splitCh = "\t";

        OutputVariable("REMOTE_ADDR", splitCh);
        OutputVariable("HTTP_X_REAL_IP", splitCh);
        OutputVariable("HTTP_VIA", splitCh);
        OutputVariable("HTTP_X_FORWARDED_FOR", newLine);

        Response.Write("---url--: " + Request.Url + newLine);
        Response.Write("serverip: " + GetServerIpList() + newLine);
        var postData = Request.Form + "";
        if (postData.Length > 0)
            Response.Write("postdata: " + postData + newLine);
        Response.Write("headers: " + newLine);
        foreach (string name in Request.Headers.ToString().Split('&'))
        {
            Response.Write(splitCh + name + newLine);
        }

        if (Request.Cookies.Count > 0)
        {
            Response.Write("cookies: " + newLine);
            foreach (string name in Request.Cookies)
            {
                if (Request.Cookies[name] != null)
                    Response.Write(splitCh + name + ":" + Request.Cookies[name].Value + newLine);
            }
        }
        Response.End();
    }

    void OutputVariable(string name, string splitCh)
    {
        var val = Request.ServerVariables[name];
        if (!string.IsNullOrWhiteSpace(val))
            Response.Write(name + ": " + val + splitCh);
    }
    
    // 获取本机IP列表
    static string GetServerIpList()
    {
        try
        {
            StringBuilder ips = new StringBuilder();
            IPHostEntry IpEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ipa in IpEntry.AddressList)
            {
                if (ipa.AddressFamily == AddressFamily.InterNetwork)
                    ips.AppendFormat("{0};", ipa);
            }
            return ips.ToString();
        }
        catch (Exception)
        {
            //LogHelper.Custom("获取本地ip错误" + ex, @"zIP\", false);
            return string.Empty;
        }
    }
</script>
