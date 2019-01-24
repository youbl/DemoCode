<%@ Page Language="C#" EnableViewState="false" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Data.SqlClient" %>
<%@ Import Namespace="System.Diagnostics" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Net" %>
<%@ Import Namespace="System.Net.Security" %>
<%@ Import Namespace="System.Net.Sockets" %>
<%@ Import Namespace="System.Reflection" %>
<%@ Import Namespace="System.Runtime.InteropServices.ComTypes" %>
<%@ Import Namespace="System.Security.Cryptography.X509Certificates" %>
<%@ Import Namespace="System.Threading" %>
<%@ Import Namespace="System.Xml" %>
<%@ Import Namespace="System.Web.Configuration" %>
<%@ Import Namespace="MySql.Data.MySqlClient" %>
<%@ Import Namespace="Npgsql" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<title>工具页</title>

<!-- 要自定义的变量列表 -->    
<script language="c#" runat="server">
    //下面替换为你需要用的md5值，比如a3de83c477b3b24d52a1c8bebbc7747b是sj.91.com
    string _pwd = "a3de83c477b3b24d52a1c8bebbc7747b"; // ip在白名单里时，进入此页面的密码md5值
    string _xxx = "a3de83c477b3b24d52a1c8bebbc7747b"; // ip不在白名单时，进入此页面的密码md5值
    // 属于内网IP或VPN IP的白名单列表
    string[] _whiteIp = new string[] {"10."};

    //要显示在 配置文本框里的ip列表
    private string m_ipLst = "127.0.0.1;";
</script>
<script language="C#" runat="server">

    private string m_currentUrl;
    private string m_localIp, m_remoteIp, m_remoteIpLst;
    /// <summary></summary>
    protected override void OnInit(EventArgs e)
    {
        try
        {
            m_localIp = GetServerIpList();
            m_remoteIp = GetRemoteIp();
            m_remoteIpLst = GetRemoteIpLst();
            m_currentUrl = GetUrl(false);

            Log("客户端ip：" + m_remoteIpLst +
                "\r\n服务器ip：" + m_localIp +
                "\r\nUrl：" + Request.Url +
                "\r\nPost：" + Request.Form,
                "", null);

            if (string.IsNullOrEmpty(_pwd))
            {
                Response.Write("未设置密码，请修改页面源码以设置密码\r\n" +
                               m_remoteIp + ";" + m_localIp);
                Response.End();
                return;
            }

            bool islogout = !string.IsNullOrEmpty(Request.QueryString["logout"]);
            if (islogout)
            {
                SetSession("p", "");
                Response.Write(Request.QueryString + "\r\n<hr />\r\n" + Request.Form + "\r\n<hr />\r\n" +
                               m_remoteIp + "--------" + m_remoteIpLst + "; server ip: " + m_localIp);
                Response.End();
                return;
            }


            // 检查md5不需要密码flg == "clientdirmd5"
            if ((Request.Form["f"] ?? "") != "clientdirmd5" && !IsLogined(m_remoteIp))
            {
                Response.Write(Request.QueryString + "\r\n<hr />\r\n" + Request.Form + "\r\n<hr />\r\n" +
                               m_remoteIp + "--------" + m_remoteIpLst + "; server ip: " + m_localIp);
                Response.End();
                return;
            }

            // 如果提交了ip参数，表示是请求Proxy
            string ip = Request.Form["ip"];
            if(!string.IsNullOrEmpty(ip) && !ip.StartsWith("127.") && ip != "::1")
            {
                DoProxy(ip);
                return;
            }

            string flg = Request.Form["flg"];
            if(!string.IsNullOrEmpty(flg))
            {
                flg = flg.Trim().ToLower();
                switch (flg)
                {
                    case "showconfig":
                        RefreshOrShowConfig();
                        break;
                    case "telnet":
                        Telnet();
                        break;
                    case "sql":
                        SqlRun();
                        break;
                    case "redis":
                        Redis();
                        break;
                    case "mysqlarch":
                        CompareMySqlArch();
                        break;
                }
                Response.End();
            }
        }
        catch (ThreadAbortException) { }
        catch (Exception exp)
        {
            Response.Write("客户ip：" + m_remoteIpLst + "；服务器：" + m_localIp + "\r\n" + exp);
            Response.End();
        }
    }

    // 判断是否登录
    bool IsLogined(string ip)
    {
        // 不判断是否内网ip, 避免nginx作反代时，导致判断为内网了, 即必须要有HTTP_X_REAL_IP
        bool isInner = ip.StartsWith("127.") || ip == "::1";
        if (!isInner)
        {
            foreach (string wip in _whiteIp)
            {
                if (ip.StartsWith(wip))
                {
                    isInner = true;
                    break;
                }
            }
        }

        bool redirect = false;
        string str = Request.QueryString["p"];
        if (!string.IsNullOrEmpty(str))
            redirect = true;

        if (!string.IsNullOrEmpty(str))
        {
            str = FormsAuthentication.HashPasswordForStoringInConfigFile(str, "MD5");
            SetSession("p", str);
        }
        else
            str = GetSession("p");

        // ip proxy通过Form提交加密好的密码,proxy只允许是内网ip
        if (string.IsNullOrEmpty(str) && isInner)
            str = Request.Form["p"];

        if (string.IsNullOrEmpty(str))
            return false;
        if (str.Equals(_pwd, StringComparison.OrdinalIgnoreCase))
        {
            if (isInner)
            {
                if (redirect)
                {
                    Response.Redirect(m_currentUrl);
                }
                return true;
            }
        }
        else if (str.Equals(_xxx, StringComparison.OrdinalIgnoreCase))
        {
            if (redirect)
            {
                Response.Redirect(m_currentUrl);
            }
            return true;
        }
        return false;
    }

    static object lockobj = new object();
    static void Log(string msg, string prefix, string filename)
    {
        DateTime now = DateTime.Now;
        if (string.IsNullOrEmpty(filename))
        {
            filename = @"e:\Data\zzCustomConfigLog\" + prefix + "\\" + now.ToString("yyyyMMddHH") + ".txt";
        }
        string dir = Path.GetDirectoryName(filename);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        lock (lockobj)
        {
            using (StreamWriter sw = new StreamWriter(filename, true, Encoding.UTF8))
            {
                sw.WriteLine(now.ToString("yyyy-MM-dd HH:mm:ss_fff"));
                sw.WriteLine(msg);
                sw.WriteLine();
            }
        }
    }

    void DoProxy(string ip)
    {
        string[] tmp = ip.Split('_');
        ip = tmp[0];
        string proxyurl;
        if (tmp.Length >= 2)
        {
            proxyurl = "https://" + tmp[1] + "/";
            if (tmp.Length >= 3)
                proxyurl += tmp[2];
            else
                proxyurl += m_currentUrl.Substring(m_currentUrl.LastIndexOf('/') + 1);
        }
        else
            proxyurl = m_currentUrl;
        string para = HttpUtility.UrlDecode(Request.Form.ToString());
        para = System.Text.RegularExpressions.Regex.Replace(para, @"(?:^|&)ip=[^&]+", "");
        para += "&p=" + GetSession("p") + "&cl=" + m_remoteIp;
        byte[] arrBin = GetPage(proxyurl, para, ip);
        string contentType = Request.QueryString["contentType"];
        if (contentType != null)
        {
            if(contentType == "down")
            {
                Response.AppendHeader("Content-Disposition", "attachment;filename=tmp");
                Response.ContentType = "application/unknown";
            }
            else if (contentType == "text")
            {
                Response.ContentType = "text/plain"; //"text/html";
            }
        }
        Response.BinaryWrite(arrBin);
        Response.End();
    }
    </script>
    
<!-- 配置相关的方法集 -->    
<script language="c#" runat="server">
    void RefreshOrShowConfig()
    {
        string type = Request.Form["f"];
        if (string.IsNullOrEmpty(type))
            type = "2";

        string ret = "（客户ip：" + m_remoteIpLst + "；服务器：" + m_localIp + "）" + "\r\n";
        if (type == "2")
        {
            string classname = Request.Form["className"];
            ret += ShowConfigs(classname);
        }
        else if (type == "3")
        {
            string cachename = Request.Form["cache"];
            ret += GetCache(cachename);
        }
        else if (type == "4")
        {
            string cachename = Request.Form["cache"];
            ret += ClearCache(cachename);
        }
        Response.Write(ret);
    }

    static string ShowConfigs(string classname)
    {
        StringBuilder sb = new StringBuilder();
        if(!string.IsNullOrEmpty(classname))
        {
            sb.Append("<a href='javascript:void(0)' onclick='showHide(this);'>============================="
                      + classname + "静态属性列表：========================</a>\r\n<span>");
            Dictionary<string, string> props = GetAllProp(classname);
            if (props == null)
            {
                sb.AppendFormat("  {0} 未找到对应的类定义 \r\n", classname);
            }
            else if (props.Count == 0)
            {
                sb.AppendFormat("  {0} 未找到静态的field或property定义 \r\n", classname);
            }
            else
            {
                foreach (KeyValuePair<string, string> pair in props)
                {
                    sb.AppendFormat("  {0}={1}\r\n", pair.Key.PadRight(31), pair.Value);
                }
            }
            sb.Append("</span>\r\n\r\n");
            return sb.ToString();
        }

        NameValueCollection nameValues = ConfigurationManager.AppSettings;
        if (nameValues.Count > 0)
        {
            sb.Append("<a href='javascript:void(0)' onclick='showHide(this);'>=============================Web.config AppSetting配置列表：========================</a>\r\n<span>");
            SortedList<string, string> appsetting = new SortedList<string, string>();
            foreach (string key in nameValues.AllKeys)
            {
                appsetting.Add(key, nameValues[key]);
            }
            foreach (KeyValuePair<string, string> pair in appsetting)
            {
                sb.AppendFormat("  {0}={1}\r\n", pair.Key.PadRight(31), pair.Value);
            }
            sb.Append("</span>\r\n\r\n");
        }

        List<string> sections = GetConfigSections();
        if (sections != null && sections.Count > 0)
        {
            sb.Append("<a href='javascript:void(0)' onclick='showHide(this);'>=============================Web.config Sections配置列表：========================</a>\r\n<span>");
            foreach (string key in sections)
            {
                sb.AppendFormat("  {0}\r\n", key);
            }
            sb.Append("</span>\r\n\r\n");
        }

        sb.Append("<a href='javascript:void(0)' onclick='showHide(this);'>=============================GC配置：========================</a>\r\n<span>");
        sb.AppendFormat("  gcServer enabled={0}　　　　", System.Runtime.GCSettings.IsServerGC);
        sb.AppendFormat("  GCLatencyMode={0}\r\n", System.Runtime.GCSettings.LatencyMode.ToString());
        sb.Append("</span>\r\n\r\n");

        sb.Append("<a href='javascript:void(0)' onclick='showHide(this);'>=============================运行时相关：========================</a>\r\n<span>");
        Process process = Process.GetCurrentProcess();
        sb.AppendFormat("  机器名: {2}； 启动时间: {0}；  当前进程占用内存:{1} 兆； {3} \r\n", 
            process.StartTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            (process.WorkingSet64 / 1024.0 / 1024.0).ToString("N2"),
            process.MachineName,
            process.MainModule.FileName);
        sb.AppendFormat("  HttpRuntime.Cache个数   :{0} \r\n", HttpRuntime.Cache.Count.ToString());
        int availableWorkerThreads, availableCompletionPortThreads, maxWorkerThreads, maxPortThreads;
        ThreadPool.GetAvailableThreads(out availableWorkerThreads, out availableCompletionPortThreads);
        ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxPortThreads);

        int runThCnt = 0;
        foreach (ProcessThread thread in process.Threads)
        {
            if (thread.ThreadState == System.Diagnostics.ThreadState.Running)
                runThCnt++;
        }
        sb.AppendFormat("  已用用户线程数:{0}； 活动用户线程数:{1} \r\n", (process.Threads.Count).ToString().PadRight(8), runThCnt.ToString());
        sb.AppendFormat("  已用异步线程数:{0}； 已用异步I/O线程数:{1}\r\n", (maxWorkerThreads - availableWorkerThreads).ToString().PadRight(8), (maxPortThreads - availableCompletionPortThreads).ToString());
        sb.AppendFormat("  最大异步线程数:{0}； 最大异步I/O线程数:{1}\r\n", maxWorkerThreads.ToString().PadRight(8), maxPortThreads.ToString());
        sb.AppendFormat("  空闲异步线程数:{0}； 空闲异步I/O线程数:{1}\r\n", availableWorkerThreads.ToString().PadRight(8), availableCompletionPortThreads.ToString());

        sb.Append("</span>\r\n\r\n");

        return sb.ToString();
    }


    static Dictionary<string, string> GetAllProp(string classname)
    {
        Type type = GetType(classname);
        if (type == null)
        {
            return null;
        }
        Dictionary<string, string> ret = new Dictionary<string, string>();
        FieldInfo[] arrfield = type.GetFields(BindingFlags.Static | BindingFlags.Public);
        foreach (FieldInfo info in arrfield)
        {
            ret.Add(info.Name, Convert.ToString(info.GetValue(null)));
        }
        arrfield = type.GetFields(BindingFlags.Static | BindingFlags.NonPublic);
        foreach (FieldInfo info in arrfield)
        {
            ret.Add(info.Name, Convert.ToString(info.GetValue(null)));
        }
        PropertyInfo[] arrprop = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
        foreach (PropertyInfo info in arrprop)
        {
            ret.Add(info.Name, Convert.ToString(info.GetValue(null, null)));
        }
        arrprop = type.GetProperties(BindingFlags.Static | BindingFlags.NonPublic);
        foreach (PropertyInfo info in arrprop)
        {
            ret.Add(info.Name, Convert.ToString(info.GetValue(null, null)));
        }
        return ret;
    }

    static Type GetType(string classname)
    {
        try
        {
            Assembly assembly = null;
            // 循环命名空间，找到对应的Assembly
            int idx = classname.LastIndexOf(".", StringComparison.Ordinal);
            while (idx > 0)
            {
                string assemName = classname.Substring(0, idx);
                try
                {
                    assembly = Assembly.Load(assemName);
                    break;
                }
                catch
                {
                    idx = classname.LastIndexOf(".", idx - 1, StringComparison.Ordinal);
                }
            }
            if (assembly == null)
            {
                return null;
            }
            return assembly.GetType(classname);
        }
        catch
        {
            return null;
        }
    }

    // 遍历configSections配置
    static List<string> GetConfigSections()
    {
        List<string> ret = new List<string>();
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(HttpContext.Current.Server.MapPath("Web.config"));
        XmlNode sectionNode = xmlDoc.SelectSingleNode("configuration//configSections");
        if (sectionNode == null)
            return ret;

        XmlNodeList nodeList = sectionNode.ChildNodes;
        //遍历所有子节点   
        foreach (XmlNode xn in nodeList)
        {
            //将子节点类型转换为XmlElement类型       
            XmlElement xe = xn as XmlElement;
            if (xe == null || xe.Name != "section")
                continue;
            string secName = xe.GetAttribute("name");
            try
            {
                object section = ConfigurationManager.GetSection(secName);
                if (section == null)
                    continue;
                ret.Add(secName + "\r\n" + GetValue(section, true));
            }
            catch(Exception exp)
            {
                ret.Add(secName + "\r\n加载失败： " + exp.Message);
            }
        }
        return ret;
    }

    // 在obj对象里查找公共属性，并返回属性的名值
    static string GetValue(object obj, bool newline)
    {
        StringBuilder l_ret = new StringBuilder();
        try
        {
            BindingFlags flags =
                BindingFlags.GetProperty |
                BindingFlags.Public |
                BindingFlags.Instance;

            PropertyInfo[] infos = obj.GetType().GetProperties(flags);
            foreach (PropertyInfo info in infos)
            {
                if (info.Name == "Item")
                    continue;
                object val = info.GetValue(obj, null);
                if (val == null)
                    continue;
                string strVal = GetValueDetail(val);
                if (string.IsNullOrEmpty(strVal))
                    continue;
                if (newline)
                    l_ret.AppendFormat("\t{0}:{1}\r\n", info.Name, strVal);
                else
                    l_ret.AppendFormat(" {0}:{1};", info.Name, strVal);
            }
        }
        catch(Exception exp)
        {
            return exp.ToString();
        }
        return l_ret.ToString();
    }

    static string GetValueDetail(object val)
    {
        if (val is string)
            return val.ToString();

        string strVal = string.Empty;
        if (val is ValueType)
        {
            strVal = val.ToString();
            // 不返回默认值，比如0，False等
            if (strVal == Activator.CreateInstance(val.GetType()).ToString())
                return string.Empty;
            return strVal;
        }

        ICollection arr = val as ICollection;
        if (arr != null)
        {
            foreach (object o in arr)
            {
                strVal += GetValueDetail(o) + ";";
            }
        }
        else if (val.ToString() == "Res91com.ResourceDataAccess.MongoDB.ServerConfiguration")
        {
            strVal = GetValue(val, false) + "|---|";
        }
        return strVal;
    }

    static string GetCache(string cachename)
    {
        StringBuilder sb = new StringBuilder();
        int cnt = 0;
        try
        {
            if (!string.IsNullOrEmpty(cachename))
            {
                var obj = HttpRuntime.Cache[cachename];
                if (obj != null)
                {
                    cnt++;
                    sb.AppendFormat("  {0}={1}\r\n", cachename.PadRight(31), Convert.ToString(obj));
                }
            }
            else
            {
                IDictionaryEnumerator cache = HttpRuntime.Cache.GetEnumerator();
                while (cache.MoveNext())
                {
                    sb.AppendFormat("  {0}={1}\r\n", Convert.ToString(cache.Key).PadRight(31), Convert.ToString(cache.Value));
                    cnt++;
                }
            }
        }
        catch (Exception exp)
        {
            sb.AppendFormat("  遍历缓存出错：{0}\r\n", exp.Message);
        }
        sb.Insert(0, "<a href='javascript:void(0)' onclick='showHide(this);'>=============================缓存列表(" + cnt.ToString() + "个)：========================</a>\r\n<span>");
        sb.Append("</span>\r\n\r\n");
        return sb.ToString();
    }

    static string ClearCache(string cachename)
    {
        int cnt = 0;
        try
        {
            if (!string.IsNullOrEmpty(cachename))
            {
                if (HttpRuntime.Cache.Remove(cachename) != null)
                    cnt++;
            }
            else
            {
                IDictionaryEnumerator cache = HttpRuntime.Cache.GetEnumerator();
                while (cache.MoveNext())
                {
                    if (HttpRuntime.Cache.Remove(Convert.ToString(cache.Key)) != null)
                        cnt++;
                }
            }
            return "  清空" + cnt.ToString() + "个";
        }
        catch (Exception exp)
        {
            return "  清空" + cnt.ToString() + "个, 遍历缓存出错： " + exp.Message;
        }
    }
</script>

<!-- Telnet配置相关的方法集 -->    
<script language="c#" runat="server">
    void Telnet()
    {
        StringBuilder sb = new StringBuilder();
        string ips = Request.Form["tip"] ?? "";
        foreach (string item in ips.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            IPAddress ip;
            int port;
            if (!ParseIp(item, out ip, out port))
            {
                sb.AppendFormat("{0} 不是ip:端口\r\n", item);
                continue;
            }
            
            DateTime start = DateTime.Now;
            try
            {
                IPEndPoint serverInfo = new IPEndPoint(ip, port);
                using (Socket socket = new Socket(serverInfo.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    //socket.BeginConnect(serverInfo, CallBackMethod, socket);
                    socket.Connect(serverInfo);
                    if (socket.Connected)
                    {
                        sb.AppendFormat("{0} 连接正常({1:N0}ms)\r\n", item, (DateTime.Now - start).TotalMilliseconds);
                    }
                    else
                    {
                        sb.AppendFormat("{0} 连接失败({1:N0}ms)\r\n", item, (DateTime.Now - start).TotalMilliseconds);
                    }
                    socket.Close();
                }
            }
            catch (Exception exp)
            {
                sb.AppendFormat("{0} 连接出错({2:N0}ms) {1}\r\n", item, exp.Message, (DateTime.Now - start).TotalMilliseconds);
            }
        }
        sb.AppendFormat("\r\n（客户ip：{0}；服务器：{1}）", m_remoteIpLst, m_localIp);
        Response.Write(sb.ToString());
    }
</script>

<!-- Sql测试方法集 -->    
<script language="C#" runat="server">
    void SqlRun()
    {
        string dbtype = Request.Form["dbtype"] ?? "sqlserver";
        bool issqlserver = (dbtype == "" || dbtype == "sqlserver");
        bool isMySql = (dbtype == "mysql");
        bool isPgSql = (dbtype == "postgresql");
        bool isBak = !string.IsNullOrEmpty(Request.Form["isbak"]);

        string prefix = "（客户ip：" + m_remoteIpLst + "；服务器：" + m_localIp + "）<br />\r\n";
        string sql = Request.Form["sql"];
        string constr = Request.Form["constr"];
        if (!string.IsNullOrEmpty(sql) && !string.IsNullOrEmpty(constr))
        {
            //if (!sql.StartsWith("select ", StringComparison.OrdinalIgnoreCase))
            if (Regex.IsMatch(sql, @"^(?i)(update|delete|insert)"))
            {
                Response.Write(prefix + "只允许Select语句，且必须包含Top子句");
                return;
            }

            DataTable dt;
            if (issqlserver)
            {
                if (!Regex.IsMatch(sql, @"^(?i)select\s+top(?:\s|\()"))
                {
                    Response.Write(prefix + "必须包含Top子句");
                    return;
                }
                dt = SqlServerRun(constr, sql);
            }
            else if (isMySql)
            {
                if (!Regex.IsMatch(sql, @"(?i)\s+limit(?:\s|\()"))
                {
                    Response.Write(prefix + "必须包含limit子句");
                    return;
                }
                dt = MySqlRun(constr, sql);
            }
            else if (isPgSql)
            {
                if (!Regex.IsMatch(sql, @"(?i)\s+limit(?:\s|\()"))
                {
                    Response.Write(prefix + "必须包含limit子句");
                    return;
                }
                dt = PgSqlRun(constr, sql);
            }
            else
            {
                Response.Write(prefix + " 不支持的数据库类型：" + dbtype);
                return;
            }
            if (dt.Rows.Count <= 0)
            {
                Response.Write(prefix + sql + "<br/>没有找到任何数据");
                return;
            }
            if (isBak)
            {
                try
                {
                    DoBackup(dt, sql);
                }
                catch (Exception exp)
                {
                    Response.Write(prefix + sql + "<br/>" + exp);
                }
                return;
            }
            GridView gv1 = new GridView();
            gv1.DataSource = dt;
            gv1.DataBind();
            Response.Write(prefix + sql + GetHtml(gv1));
        }
        else
        {
            Response.Write(prefix + "没有输入Sql或连接串");
        }
    }

    void DoBackup(DataTable dt, string sql)
    {
        // dt.PrimaryKey
        Regex reg = new Regex(@"(?i)\sfrom\s+([^\s]+)");
        Match match = reg.Match(sql);
        if (!match.Success)
        {
            Response.Write("没有匹配到Sql的from");
            return;
        }
        string tbName = match.Result("$1");
        string[] cols = GetColumn(dt);
        int colNum = cols.Length;

        StringBuilder sqlInsert = new StringBuilder("INSERT INTO " + tbName + "\r\n( ");
        StringBuilder sqlUpdate = new StringBuilder();
        for (int i = 0; i < colNum; i++)
        {
            if(i == 0)
                sqlInsert.AppendFormat("{0}", cols[i]);
            else
                sqlInsert.AppendFormat(",{0}", cols[i]);
        }
        sqlInsert.Append(")\r\nVALUES\r\n");

        string sqlUpdateMain = "UPDATE " + tbName + " SET \r\n";
        bool isFirstRow = true;
        foreach (DataRow row in dt.Rows)
        {
            sqlUpdate.Append(sqlUpdateMain);

            StringBuilder itemInsert = new StringBuilder("(");
            for (int i = 0; i < colNum; i++)
            {
                string value = Convert.ToString(row[cols[i]]).Replace("'", "''");
                if (i == 0)
                {
                    itemInsert.AppendFormat("'{0}'", value);
                    sqlUpdate.AppendFormat("    {0}='{1}'", cols[i], value);
                }
                else
                {
                    itemInsert.AppendFormat(",'{0}'", value);
                    sqlUpdate.AppendFormat(",{0}='{1}'", cols[i], value);
                }
            }

            itemInsert.Append(")");
            if (isFirstRow)
            {
                sqlInsert.AppendFormat("{0}", itemInsert);
                isFirstRow = false;
            }
            else
                sqlInsert.AppendFormat(",\r\n{0}", itemInsert);

            bool isFirstKey = true;
            foreach (DataColumn column in dt.PrimaryKey)
            {
                string value = Convert.ToString(row[column.ColumnName]).Replace("'", "''");
                if (isFirstKey)
                {
                    sqlUpdate.AppendFormat("\r\n WHERE {0}='{1}'", column.ColumnName, value);
                    isFirstKey = false;
                }
                else
                    sqlUpdate.AppendFormat(" AND {0}='{1}'", column.ColumnName, value);
            }
            sqlUpdate.Append(";\r\n");
        }
        Response.Write("/* INSERT数据的备份SQL，最下面还有UPDATE数据的备份SQL */\r\n" + sqlInsert +
            ";\r\n\r\n/* 下面是更新回旧数据的SQL */\r\n");
        Response.Write(sqlUpdate);
    }

    static string[] GetColumn(DataTable dt)
    {
        string[] ret = new string[dt.Columns.Count];
        int i = 0;
        foreach (DataColumn column in dt.Columns)
        {
            ret[i] = column.ColumnName;
            i++;
        }
        return ret;
    }

    static DataTable SqlServerRun(string constr, string sql)
    {
        DataTable dt = new DataTable();
        using (SqlConnection con = new SqlConnection(constr))
        using (SqlCommand command = con.CreateCommand())
        using (SqlDataAdapter dataAdapter = new SqlDataAdapter(command))
        {
            con.Open();
            command.CommandText = sql;
            dataAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            dataAdapter.Fill(dt);
            con.Close();
        }
        return dt;
    }
    static DataTable MySqlRun(string constr, string sql)
    {
        DataTable dt = new DataTable();
        using (MySqlConnection con = new MySqlConnection(constr))
        using (MySqlCommand command = con.CreateCommand())
        using (MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command))
        {
            con.Open();
            command.CommandText = sql;
            dataAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            dataAdapter.Fill(dt);
            con.Close();
        }
        return dt;
    }
    static DataTable PgSqlRun(string constr, string sql)
    {
        // bin目录下要有2个dll： Mono.Security.dll Npgsql.dll
        // 连接串参考： server=10.0.0.1;port=3433;uid=xxx;pwd=xxx;database=stdb
        DataTable dt = new DataTable();
        using (NpgsqlConnection con = new NpgsqlConnection(constr))
        using (NpgsqlCommand command = con.CreateCommand())
        using (NpgsqlDataAdapter dataAdapter = new NpgsqlDataAdapter(command))
        {
            con.Open();
            command.CommandText = sql;
            dataAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            dataAdapter.Fill(dt);
            con.Close();
        }
        return dt;
    }
    static string GetHtml(Control ctl)
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
</script>

<!-- Redis管理方法集 -->    
<script language="c#" runat="server">
    void Redis()
    {
        string tip = Request.Form["tip"] ?? "";
        int pwdSplit = tip.LastIndexOf('@');
        string pwd = null;
        if (pwdSplit >= 0)
        {
            pwd = tip.Substring(0, pwdSplit);
            tip = tip.Substring(pwdSplit + 1);
        }

        IPAddress ip;
        int port;
        if (!ParseIp(tip, out ip, out port))
        {
            Response.Write(tip + " ip格式错误或端口不是数字");
            return;
        }
        string strdb = (Request.Form["rdb"] ?? "0").Trim();
        int db;
        if (!int.TryParse(strdb, out db) || db < 0)
        {
            Response.Write("db必须是正整数");
            return;
        }
        string searchCmd = (Request.Form["cm"] ?? "info").Trim();
        if (searchCmd.IndexOf("get", StringComparison.OrdinalIgnoreCase) != 0
            && searchCmd.IndexOf("hget", StringComparison.OrdinalIgnoreCase) != 0
            && searchCmd.IndexOf("info", StringComparison.OrdinalIgnoreCase) != 0)
        {
            Response.Write(" 安全起见，暂时只允许get 和 hget、info命令: " + searchCmd);
            return;
        }

        List<byte> arrAll = new List<byte>(1024);
        IPEndPoint serverInfo = new IPEndPoint(ip, port);
        using (Socket socket = new Socket(serverInfo.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
        {
            //socket.BeginConnect(serverInfo, CallBackMethod, socket);
            socket.Connect(serverInfo);
            if (socket.Connected)
            {
                byte[] bytesReceived = new byte[1024];
                byte[] command;
                if(!string.IsNullOrEmpty(pwd))
                {
                    command = Encoding.UTF8.GetBytes("auth " + pwd + "\r\n");
                    socket.Send(command);
                    socket.Receive(bytesReceived, bytesReceived.Length, 0);
                }
                if (db > 0)
                {
                    command = Encoding.UTF8.GetBytes("select " + db.ToString() + "\r\n");
                    socket.Send(command);
                    socket.Receive(bytesReceived, bytesReceived.Length, 0);
                }
                command = Encoding.UTF8.GetBytes(searchCmd + "\r\n");
                socket.Send(command);

                int zeroCnt = 0;
                while (true)
                {
                    int tmp = socket.Receive(bytesReceived, bytesReceived.Length, 0);
                    if (tmp <= 0)
                    {
                        zeroCnt++;// 总共5次0字节时，退出
                        if (zeroCnt > 5)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (tmp == bytesReceived.Length)
                        {
                            arrAll.AddRange(bytesReceived);
                        }
                        else
                        {
                            byte[] tarr = new byte[tmp];
                            Array.Copy(bytesReceived, tarr, tmp);
                            arrAll.AddRange(tarr);
                            break;
                        }
                    }
                }
            }
            socket.Close();
        }
        Response.Write(Encoding.UTF8.GetString(arrAll.ToArray()));

        //sb.AppendFormat("\r\n（客户ip：{0}；服务器：{1}）", m_remoteIpLst, m_localIp);
    }
</script>

<!-- 数据库对比方法集 -->    
<script language="c#" runat="server">
    void CompareMySqlArch()
    {
        var offDb = HttpUtility.UrlDecode(Request.Form["testdb"] ?? "");
        var onDb = HttpUtility.UrlDecode(Request.Form["onlinedb"] ?? "");
        if (offDb.Length == 0 || onDb.Length == 0)
        {
            Response.Write("请输入线下或线上库连接串");
            return;
        }
        var ignoreCase = string.IsNullOrEmpty(Request.Form["chkCase"]);

        // 表的字段对比
        var offCol = GetTableMetaArr(offDb,ignoreCase);
        var onCol = GetTableMetaArr(onDb,ignoreCase);
        var colRet = CompareRet(offCol, onCol, false);
        var colHtml = GetTableHtmlArr(colRet, false);
        // 表的索引对比
        var offIndex = GetIndexMetaArr(offDb);
        var onIndex = GetIndexMetaArr(onDb);
        var indexRet = CompareRet(offIndex, onIndex, true);
        var indexHtml = GetTableHtmlArr(indexRet, true);
        // todo: 表的字符集对比

        var resolve = GetCombineResolve(offDb, colHtml.Item2, indexHtml.Item2);
        var column = colHtml.Item1;
        var index = indexHtml.Item1;
        DisplayCompare(offDb, onDb, column, index, resolve);
        //Response.Write(Encoding.UTF8.GetString(arrAll.ToArray()));

        //sb.AppendFormat("\r\n（客户ip：{0}；服务器：{1}）", m_remoteIpLst, m_localIp);
    }

    static string GetCombineResolve(string db, Dictionary<string, string> col, Dictionary<string, string> index)
    {
        var result = "";
        var newLine = Environment.NewLine;
        foreach (var item in index)
        {
            var tableName = item.Key;
            var resolve = item.Value;
            string sql;
            if (col.TryGetValue(tableName, out sql))
            {
                result += sql + resolve;
                if (sql.StartsWith("CREATE"))
                {
                    var tableInfo = getTableInfo(db, tableName);
                    result = result.TrimEnd(',', '\r', '\n') + newLine + ")" + tableInfo + ";" + newLine + newLine;
                }
                else
                {
                    result = result.TrimEnd(',', '\r', '\n') + ";" + newLine + newLine;
                }
            }
            else
            {
                result += "ALTER TABLE " + tableName + " " + newLine + resolve.TrimEnd(',', '\r', '\n') + ";" + newLine + newLine;
            }
        }

        foreach (var item in col)
        {
            var tableName = item.Key;
            var resolve = item.Value;

            if (!index.ContainsKey(tableName))
            {
                if (col[tableName].StartsWith("DROP"))
                {
                    result += resolve + ";" + newLine;
                }
                else if (col[tableName].StartsWith("CREATE"))
                {
                    var tableInfo = getTableInfo(db, tableName);
                    result += resolve.TrimEnd(',', '\r', '\n') + newLine + ")" + tableInfo + ";" + newLine;
                }
                else
                {
                    result += resolve.TrimEnd(',', '\r', '\n');
                    result = result + ";" + newLine;
                }
            }
        }
        return result;
    }

    static string getTableInfo(string constr, string tableName)
    {
        string dbname = GetDbName(constr);
        var sql = @"SELECT engine,table_collation,table_comment 
from information_schema.tables WHERE table_schema='" + dbname + "' AND TABLE_NAME='" + tableName + "'";
        var dt = MySqlRun(constr, sql);
        var data = dt.Rows[0];
        var charset = Convert.ToString(data["table_collation"]).Split('_');
        var result = " ENGINE=" + Convert.ToString(data["engine"]) +
                     " DEFAULT CHARSET=" + charset[0] + " COMMENT='" + data["table_comment"] + "'";
        return result;
    }

    void DisplayCompare(string offDb, string onDb, string column, string index, string resolve)
    {
        var html = @"<html>
          <head>
            <title>线上线下数据库对比</title>
            <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8""/>
          </head>
          <body>
            <script type=""text/javascript"" src=""https://ascdn.bdstatic.com/devplat/static/lib/js/jquery_125bece.js""></scr" + @"ipt>
            <script type=""text/javascript"" src=""https://ascdn.bdstatic.com/devplat/static/index/ui.tabs_d89ad1a.js""></scr" + @"ipt>
            <link rel=""stylesheet"" href=""https://ascdn.bdstatic.com/devplat/static/index/ui.tabs_2b0cf63.css"" type=""text/css"" media=""print, projection, screen""/>

            <div id=""container-1"">
<table border='1' cellspacing='1' cellpadding='10'>
<tr><td style='color:red;'>
注意：背景为黄色表示仅注释不同，可以不改线上；<br/>
　　　背景为红色表示字段定义有变化，线上需要同步修改。
</td>
<td>线下库连接串： " + offDb + "<br/>线上库连接串： " + onDb + @"</td>
</tr>
<tr><td colspan='2' style='font-weight:bold;color:blue;'>
当前对比结果：" +(string.IsNullOrEmpty(Request.Form["chkCase"])?"不区分":"区分")+@"大小写（含表名和字段名）
<td></tr>
<tr><td colspan='2' style='font-weight:bold;'>
<label><input type=""checkbox"" onclick=""showDirr(this)"">----只显示有变化的字段，隐藏无变化的字段----</label>
<td></tr>
</table>";
        var htmlTabBody = "<div><h1 align=center>字段对比</h1>" + column +
            @"<hr style=""border:3 double #987cb9"" color=#987cb9 SIZE=10><h1 align=center>索引对比</h1>" + index +
            @"<hr style=""border:3 double #987cb9"" color=#987cb9 SIZE=10><h1 align=center>解决语句</h1><textarea cols=""100"" rows=""50"">" + resolve +
            @"</textarea></div>

<script type=""text/javascript"">
function showDirr(obj){
    var divs = document.getElementsByClassName(""compare"");
    if(obj.checked){
        for(var i in divs){
            var hasDiff = false;
            if(divs[i].nodeType == 1){
                var trs = divs[i].getElementsByTagName(""tr"");
                for(var j in trs){
                    if(trs[j].nodeType == 1 && !trs[j].hasAttribute(""bgcolor"")){
                        if(trs[j].getElementsByTagName(""th"").length==0){
                            trs[j].style.display=""none"";
                        }
                    }else{
                        if(trs[j].nodeType == 1){
                            hasDiff = true;
                        }
                    }
                }
                if(!hasDiff){
                    divs[i].style.display=""none"";
                }
            }
        }
    }else{
        for(var i in divs){
            if(divs[i].nodeType == 1){
                var trs = divs[i].getElementsByTagName(""tr"");
                for(var j in trs){
                    if(trs[j].nodeType == 1 && !trs[j].hasAttribute(""bgcolor"")){
                        if(trs[j].getElementsByTagName(""th"").length==0){
                            trs[j].style.display="""";
                        }
                    }
                }
                divs[i].style.display="""";
            }
        }
    }
}
</scri"+@"pt>";
        Response.Write(html + htmlTabBody);
    }

    /// <summary>
    /// 根据连接字符串，收集所有表的字段信息.
    /// key为表名，value为字段和定义
    /// </summary>
    static Dictionary<string, List<string[]>> GetTableMetaArr(string constr, bool ignoreCase)
    {
        string dbname = GetDbName(constr);

        // 第一列必须是表名，第二列必须是字段名，最后一列必须是注释
        var sql = @"SELECT c.table_name, c.column_name, c.column_type, is_nullable,
        c.column_default,c.extra, c.column_comment
            FROM information_schema.columns c WHERE
        c.table_schema='" + dbname + "' ORDER BY c.table_name, c.column_name";
        var dt = MySqlRun(constr, sql);

        var ret = new Dictionary<string, List<string[]>>();
        string[] cols = GetColumn(dt);
        foreach (DataRow row in dt.Rows)
        {
            var key = "";
            var arrCol = new List<string>();
            var isNull = true;

            foreach (string colName in cols)
            {
                var colVal = Convert.ToString(row[colName]);
                var isValNull = row[colName] == null || row[colName] == DBNull.Value;
                if (key.Length == 0)
                    key = ignoreCase ? colVal.ToLower() : colVal; // 用第一个字段表名，作为Dict的key
                else
                {
                    switch (colName)
                    {
                        case "column_type":
                            arrCol.Add(colVal.Replace(",", "|"));
                            break;
                        case "column_default":
                            if (isValNull)
                            {
                                if (isNull)
                                    arrCol.Add("DEFAULT NULL");
                            }
                            else if (colVal.Length == 0)
                                arrCol.Add("DEFAULT ''");
                            else
                                arrCol.Add("DEFAULT '" + colVal + "'");
                            break;
                        case "is_nullable":
                            if (colVal == "NO")
                            {
                                isNull = false;
                                arrCol.Add("NOT NULL");
                            }
                            break;
                        case "column_comment":
                            if (colVal.Length > 0)
                                arrCol.Add("COMMENT '" + colVal + "'");
                            break;
                        case "extra":
                            if (colVal.Length > 0)
                                arrCol.Add(colVal);
                            break;
                        case "column_name":
                            arrCol.Add(ignoreCase ? colVal.ToLower() : colVal);// 字段名忽略大小写
                            break;
                        default:
                            arrCol.Add(colVal);
                            break;
                    }
                }
            }
            List<string[]> tableRow;
            if (!ret.TryGetValue(key, out tableRow))
            {
                tableRow = new List<string[]>();
                ret.Add(key, tableRow);
            }
            tableRow.Add(arrCol.ToArray());
        }
        return ret;
    }

    static Dictionary<string, List<string[]>> GetIndexMetaArr(string constr)
    {
        string dbname = GetDbName(constr);

        // 第一列必须是表名，第二列必须是索引名
        var sql = @"SELECT TABLE_NAME,index_name,column_name,non_unique,index_type
        FROM information_schema.statistics WHERE TABLE_SCHEMA='" + dbname +
                  "'ORDER BY TABLE_NAME,index_name,seq_in_index";
        var dt = MySqlRun(constr, sql);

        var ret = new Dictionary<string, Dictionary<string, List<string[]>>>();
        foreach (DataRow row in dt.Rows)
        {
            var tableName = Convert.ToString(row["TABLE_NAME"]);
            var indexName = Convert.ToString(row["index_name"]);
            Dictionary<string, List<string[]>> val;
            if (!ret.TryGetValue(tableName, out val))
            {
                val = new Dictionary<string, List<string[]>>();
                ret.Add(tableName, val);
            }
            List<string[]> allCol;
            if (!val.TryGetValue(indexName, out allCol))
            {
                allCol = new List<string[]>();
                val.Add(indexName, allCol);
            }
            allCol.Add(new string[]
            {
                Convert.ToString(row["column_name"]),
                Convert.ToString(row["non_unique"]),
                Convert.ToString(row["index_type"])
            });
        }
        return ProcessIndexResult(ret);
    }

    static string GetDbName(string constr)
    {
        var regex = new Regex(@"Database=([^;]+)", RegexOptions.IgnoreCase);
        var match = regex.Match(constr);
        if (!match.Success)
            throw new Exception("连接串找不到Database");
        return match.Result("$1");
    }

    static Dictionary<string, List<string[]>> ProcessIndexResult(Dictionary<string, Dictionary<string, List<string[]>>> indexInfos)
    {
        var result = new Dictionary<string, List<string[]>>();
        foreach (var item in indexInfos)
        {
            var tableName = item.Key;
            var index = item.Value;
            result[tableName] = new List<string[]>();
            foreach (var itemIdx in index)
            {
                var indexName = itemIdx.Key;
                var indexInfo = itemIdx.Value;
                var cols = "(";
                var info = "";
                var type = "";
                for (var i = 0; i < indexInfo.Count; i++)
                {
                    if (info == "") {
                        type = "USING " + indexInfo[i][2];
                        if (indexName == "PRIMARY") {
                            info = "PRIMARY KEY";
                            type = "";
                        } else if (indexInfo[i][1] == "0") {
                            info = "UNIQUE INDEX " + indexName;
                        } else {
                            info = "INDEX " + indexName;
                        }
                    }
                    cols += indexInfo[i][0] + '|';
                }

                cols = cols.Substring(0, cols.Length - 1) + ')';
                result[tableName].Add(new string[] {indexName, info, cols, type});
            }
        }
        return result;
    }

    /// <summary>
    /// string[]第3列为0表示一致，1表示线上有线下没有，2表示线上没有线下有，
    /// 3表示线下线上都有但是定义不一致，5表示定义一致但是注释不同
    /// </summary>
    /// <param name="offData"></param>
    /// <param name="onData"></param>
    /// <param name="isIndex"></param>
    /// <returns></returns>
    static Dictionary<string, Dictionary<string, string[]>> CompareRet(Dictionary<string, List<string[]>> offData,
        Dictionary<string, List<string[]>> onData, bool isIndex)
    {
        var result = new Dictionary<string, Dictionary<string, string[]>>();
        foreach (var item in offData)
        {
            var table = item.Key;
            var offCols = item.Value;
            List<string[]> onCols;
            if (!onData.TryGetValue(table, out onCols))
                result.Add(table, GetEmptyTable(offCols, false)); // 线上不存在该表
            else
            {
                // 线上线下都有这个表，对比2边的字段
                var comparRet = CompareCols(offCols, onCols, isIndex);
                result.Add(table, comparRet);
            }
        }
        if (!isIndex)
        {
            foreach (var item in onData)
            {
                var table = item.Key;
                var onCols = item.Value;
                if (!offData.ContainsKey(table))
                    result.Add(table, GetEmptyTable(onCols, true)); // 线下不存在该表
            }
        }
        return result;
    }

    /// <summary>
    /// 获取空表字段信息
    /// </summary>
    /// <param name="columns"></param>
    /// <param name="isOnline"></param>
    /// <returns></returns>
    static Dictionary<string, string[]> GetEmptyTable(List<string[]> columns, bool isOnline)
    {
        var columnInfo = new Dictionary<string, string[]>();
        foreach (string[] column in columns)
        {
            var columnName = column[0];
            if (isOnline)
                columnInfo[columnName] = new string[] {"", Implode(',', column, true), "1"};
            else
                columnInfo[columnName] = new string[] {Implode(',', column, true), "", "2"};
        }
        return columnInfo;
    }

    static Dictionary<string, string[]> CompareCols(List<string[]> arrOffCols, List<string[]> arrOnCols, bool isIndex)
    {
        var columnInfo = new Dictionary<string, string[]>();
        var offCols = ReInitCols(arrOffCols);
        var onCols = ReInitCols(arrOnCols);
        foreach (var col in offCols)
        {
            var key = col.Key;
            var colOff = col.Value;
            string[] colOn;
            if (!onCols.TryGetValue(key, out colOn))
            {
                //线上不存在该字段，最后一个参数为2
                columnInfo.Add(key, new string[] {Implode(',', colOff, true), "", "2"});
                // unset($offCols[$key]);
                continue;
            }
            // 线下线上都存在时
            var strOff = Implode(',', colOff, true);
            var strOn = Implode(',', colOn, true);
            if (strOff.Equals(strOn))
            {
                //相同为0
                columnInfo.Add(key, new string[] {strOff, strOn, "0"});
                continue;
            }
            if (isIndex)
            {
                //线上线下索引定义不一致，最后一个参数为3
                columnInfo.Add(key, new string[] {strOff, strOn, "3"});
                continue;
            }
            // 判断是否注释不一致，最后一列是注释，把它置空
            colOff[colOff.Length - 1] = "";
            colOn[colOn.Length - 1] = "";
            if (Implode(',', colOff) == Implode(',', colOn))
                columnInfo.Add(key, new string[] {strOff, strOn, "5"});//评论不同为5
            else
                columnInfo.Add(key, new string[] {strOff, strOn, "3"});
        }

        foreach (var col in onCols)
        {
            var key = col.Key;
            if (!offCols.ContainsKey(key))
            {
                //线下不存在该字段，最后一个参数为1
                columnInfo.Add(key, new string[] {"", Implode(',', col.Value, true), "1"});
                //continue;
            }
        }

        return columnInfo;
    }

    static Dictionary<string, string[]> ReInitCols(List<string[]> cols)
    {
        var columnInfo = new Dictionary<string, string[]>();
        foreach (string[] col in cols)
        {
            columnInfo.Add(col[0], col);
        }
        return columnInfo;
    }

    /// <summary>
    /// 数组拼接成字符串
    /// </summary>
    /// <param name="ch"></param>
    /// <param name="arr"></param>
    /// <param name="ignoreFirst"></param>
    /// <returns></returns>
    static string Implode(char ch, string[] arr, bool ignoreFirst = false)
    {
        if (arr.Length <= 0)
            return string.Empty;
        var ret = "";
        var idx = 0;
        foreach (var item in arr)
        {
            idx++;
            if (ignoreFirst && idx <= 1)
                continue;
            ret += ch + item;
        }
        if (ret.Length > 0)
            ret = ret.Substring(1);
        return ret;
    }

    /// <summary>
    /// 组织成html返回。
    /// key是表结构修改sql，value是html展示
    /// </summary>
    /// <param name="tables"></param>
    /// <param name="isIndex"></param>
    /// <returns></returns>
    static Tuple<string, Dictionary<string, string>> GetTableHtmlArr(
        Dictionary<string, Dictionary<string, string[]>> tables, bool isIndex)
    {
        var dbArr = "";
        var resolves = new Dictionary<string, string>();
        var tab = "   ";
        var newLine = Environment.NewLine;
        foreach (var perTb in tables)
        {
            var table = perTb.Key;
            var columns = perTb.Value;
            dbArr += "<div class=\"compare\">";
            dbArr += "<table width=\"80%\" border=\"1\" cellspacing=\"0\">";
            dbArr += "<tr><th colspan=\"3\">" + table + "</th></tr>";
            if (isIndex)
            {
                dbArr += "<tr><th align=\"center\">索引名</th><th align=\"center\">线下</th><th align=\"center\">线上</th>";
            }
            else
            {
                dbArr += "<tr><th align=\"center\">列名</th><th align=\"center\">线下</th><th align=\"center\">线上</th>";
            }
            var hasDiff = false;
            var colSize = columns.Count;
            var noOnline = 0;
            var noOffline = 0;
            var resolve = "";
            if (!isIndex)
            {
                resolve = "ALTER TABLE " + table + newLine;
            }
            foreach (var perCol in columns)
            {
                var name = perCol.Key;
                var attrs = perCol.Value;
                var compareRet = int.Parse(attrs[2]);
                var bg = "";
                if (compareRet == 5)
                    bg = "bgcolor='yellow'";
                else if (compareRet > 0)
                {
                    hasDiff = true;
                    bg = "bgcolor='#FF6347'";
                    var colTypes = attrs[0].Replace(",", " ");
                    if (compareRet == 1)
                    {
                        noOffline++;
                        if (isIndex)
                        {
                            if (name == "PRIMARY")
                            {
                                resolve += tab + " DROP PRIMARY KEY," + newLine;
                            }
                            else
                            {
                                resolve += tab + " DROP INDEX " + name + "," + newLine;
                            }
                        }
                        else
                        {
                            resolve += tab + " DROP COLUMN " + name + "," + newLine;
                        }
                    }
                    else if (compareRet == 2)
                    {
                        noOnline++;
                        if (isIndex)
                        {
                            resolve += tab + " ADD " + colTypes + "," + newLine;
                        }
                        else
                        {
                            resolve += tab + " ADD COLUMN " + name + " " + colTypes + "," + newLine;
                        }
                    }
                    else if (compareRet == 3)
                    {
                        if (isIndex)
                        {
                            if (name == "PRIMARY")
                            {
                                resolve += tab + " DROP PRIMARY KEY," + newLine;
                            }
                            else
                            {
                                resolve += tab + " DROP INDEX " + name + "," + newLine;
                            }
                            resolve += tab + " ADD " + colTypes + "," + newLine;
                        }
                        else
                        {
                            resolve += tab + " MODIFY COLUMN " + name + " " + colTypes + "," + newLine;
                        }
                    }
                }
                dbArr += "<tr " + bg + "><td>" + name + "</td>";
                // string tdLeft = attrs[0];
                // string tdRight = attrs[1];
                dbArr += "<td>" + (string.IsNullOrEmpty(attrs[0]) ? "-" : attrs[0]) + "</td>";
                dbArr += "<td>" + (string.IsNullOrEmpty(attrs[1]) ? "-" : attrs[1]) + "</td>";
                dbArr += "</tr>";
            }

            if (hasDiff)
            {
                //resolve = resolve.TrimEnd('\r', '\n');
                if (noOffline == colSize)
                {
                    resolve = "\r\nDROP TABLE " + table;
                }
                if (noOnline == colSize)
                {
                    if (!isIndex)
                    {
                        resolve = resolve.Replace("ADD COLUMN", "").Replace("ALTER TABLE " +
                            table, "CREATE TABLE " + table + "(");
                    }
                    else
                    {
                        resolve = resolve.Replace("ADD", "");
                    }
                }
                resolve = resolve.Replace("|", ",");
                resolves[table] = resolve;
            }
            dbArr += "</table><br/><br/><br/></div>";
        }
        var result = new Tuple<string, Dictionary<string, string>>(dbArr, resolves);
        return result;
    }

</script>

<!-- 通用方法集 -->    
<script language="C#" runat="server">

    static bool ParseIp(string ipPort, out IPAddress ip, out int port)
    {
        ip = null;
        port = 0;
        string[] tmp = ipPort.Trim().Split(new char[] {':', ' '}, StringSplitOptions.RemoveEmptyEntries);
        if (tmp.Length != 1 && tmp.Length != 2)
        {
            return false;
        }
        if (tmp.Length == 1)
            port = 6379;
        else if (!int.TryParse(tmp[1], out port))
            return false;
        if (IPAddress.TryParse(tmp[0], out ip))
            return true;

        try
        {
            IPHostEntry hostinfo = Dns.GetHostEntry(tmp[0]);
            IPAddress[] aryIP = hostinfo.AddressList;
            ip = aryIP[0];
            return true;
        }
        catch
        {
            return false;
        }
    }

    // POST获取网页内容
    static byte[] GetPage(string url, string param, string proxy)
    {
        var needSetHost = !string.IsNullOrEmpty(proxy);
        if (needSetHost)
        {
            // 不再使用proxy方案，改用替换url里的host为ip，并设置header里的host实现
            SwitchHost(ref url, ref proxy);
        }

        ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;
        HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
        request.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");
        request.Headers.Add("Accept-Charset", "utf-8");
        request.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0;)";
        request.AllowAutoRedirect = true; //出现301或302之类的转向时，是否要转向
        if (needSetHost)
        {
            request.Host = proxy.Split(':')[0]; // 避免替换出来的域名带了端口
            //string[] tmp = proxy.Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries);
            //int port = 80;
            //if (tmp.Length >= 2)
            //    if (!int.TryParse(tmp[1], out port))
            //        port = 80;
            //request.Proxy = new WebProxy(tmp[0], port);
        }
        request.Method = "POST";
        request.ContentType = "application/x-www-form-urlencoded";
        // 设置提交的数据
        if (!string.IsNullOrEmpty(param))
        {
            // 把数据转换为字节数组
            byte[] l_data = Encoding.UTF8.GetBytes(param);
            request.ContentLength = l_data.Length;
            // 必须先设置ContentLength，才能打开GetRequestStream
            // ContentLength设置后，reqStream.Close前必须写入相同字节的数据，否则Request会被取消
            using (Stream newStream = request.GetRequestStream())
            {
                newStream.Write(l_data, 0, l_data.Length);
                newStream.Close();
            }
        }
        using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
        using (Stream stream = response.GetResponseStream())
        {
            if (stream == null)
                return new byte[0];
            //using (var sr = new StreamReader(stream, Encoding.UTF8))
            //{
            //    return sr.ReadToEnd();
            //}
            List<byte> ret = new List<byte>(10000);
            byte[] arr = new byte[10000];
            int readcnt;
            while ((readcnt = stream.Read(arr, 0, arr.Length)) > 0)
            {
                for (int i = 0; i < readcnt; i++)
                    ret.Add(arr[i]);
                //ret.AddRange(arr.Take(readcnt));
            }
            return ret.ToArray();
        }
    }


    static Regex regIp = new Regex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$", RegexOptions.Compiled);
    static void SwitchHost(ref string url, ref string hostip)
    {
        var urlHost = GetHostFromUrl(url);
        if (urlHost == String.Empty)
        {
            return;
        }
        if (string.IsNullOrEmpty(hostip) || !regIp.IsMatch(hostip.Split(':')[0]))
        {
            return;
        }
        url = url.Replace(urlHost, hostip);
        hostip = urlHost.Split(':')[0];
    }
    static string GetHostFromUrl(string url)
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
        if (idx == 0)
        {
            return string.Empty;
        }
        if (idx < 0)
        {
            return url;
        }
        return url.Substring(0, idx);
    }
    static bool CheckValidationResult(object sender, X509Certificate certificate,
        X509Chain chain, SslPolicyErrors errors)
    {
        return true; //总是接受
    }
    // 获取远程IP列表
    static string GetRemoteIp()
    {
        string ip = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
        if (ip != null && (ip.StartsWith("10.") || ip.StartsWith("172.") || ip.StartsWith("192.168")))
        {
            string realIp = HttpContext.Current.Request.ServerVariables["HTTP_X_REAL_IP"];
            if (realIp != null && (realIp = realIp.Trim()) != string.Empty)
                ip = realIp;
        }
        return ip;
    }

    static string GetRemoteIpLst()
    {
        if (HttpContext.Current == null)
            return string.Empty;
        var request = HttpContext.Current.Request;
        string ip1 = request.UserHostAddress;
        string ip2 = request.ServerVariables["REMOTE_ADDR"];
        string realip = request.ServerVariables["HTTP_X_REAL_IP"];
        string isvia = request.ServerVariables["HTTP_VIA"];
        string forwardip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
        return ip1 + ";" + ip2 + ";" + realip + ";" + isvia + ":" + forwardip;
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
            //LogHelper.WriteCustom("获取本地ip错误" + ex, @"zIP\", false);
            return string.Empty;
        }
    }

    // 获取Session，如果禁用Session时，获取Cookie
    static string GetSession(string key)
    {
        SessionStateSection sessionStateSection = (SessionStateSection) ConfigurationManager.GetSection("system.web/sessionState");
        if (sessionStateSection.Mode == SessionStateMode.Off)
        {
            HttpCookie cook = HttpContext.Current.Request.Cookies[key];
            if (cook == null) return string.Empty;
            return cook.Value;
        }
        else
            return Convert.ToString(HttpContext.Current.Session[key]);
    }

    // 设置Session，如果禁用Session时，设置Cookie
    static void SetSession(string key, string value)
    {
        SessionStateSection sessionStateSection = (SessionStateSection) ConfigurationManager.GetSection("system.web/sessionState");
        if (sessionStateSection.Mode == SessionStateMode.Off)
            HttpContext.Current.Response.Cookies.Add(new HttpCookie(key, value));
        else
            HttpContext.Current.Session[key] = value;
    }

    /// <summary>获取当前访问的页面的完整URL，如http://sj.91.com/dir/a.aspx </summary>
    static string GetUrl(bool getQueryString)
    {
        string url = "";//HttpContext.Current.Request.ServerVariables["SERVER_NAME"];

        //if (HttpContext.Current.Request.ServerVariables["SERVER_PORT"] != "80")
        //    url += ":" + HttpContext.Current.Request.ServerVariables["SERVER_PORT"];

        url += HttpContext.Current.Request.ServerVariables["SCRIPT_NAME"];

        if (getQueryString)
        {
            if (HttpContext.Current.Request.QueryString.ToString() != "")
            {
                url += "?" + HttpContext.Current.Request.QueryString;
            }
        }
        // nginx上把https脱了，所以导致返回http会出错
        //string https = HttpContext.Current.Request.ServerVariables["HTTPS"];
        //if (string.IsNullOrEmpty(https) || https == "off")
        //{
        //    url = "http://" + url;
        //}
        //else
        //{
        //    url = "https://" + url;
        //}
        return url;
    }


</script>

    <script type="text/javascript" src="https://ascdn.bdstatic.com/fz_act/js/jq_125bece.js"></script>
    <script type="text/javascript" src="https://ascdn.bdstatic.com/fz_act/js/ui.tabs_d89ad1a.js"></script>
    <link rel="stylesheet" href="https://ascdn.bdstatic.com/fz_act/css/ui.tabs_2b0cf63.css" type="text/css" />

    <link rel="stylesheet" href="https://ascdn.bdstatic.com/fz_act/js/jquery-ui-min.css">
    <script type="text/javascript" src="https://ascdn.bdstatic.com/fz_act/js/jquery-ui-min.js"></script>

    <script type="text/javascript" src="https://ascdn.bdstatic.com/fz_act/js/rowColor_666490a.js"></script>
    <style type="text/css">
        .filetb { border-collapse: collapse;}
        .filetb td,th{ border: black 1px solid;padding: 2px 2px 2px 2px}
        #divret a{ TEXT-DECORATION: none; }
    </style>
    <script type="text/javascript">

        $(document).ready(function () {
            // 初始化标签
            var s = new UI_TAB();
            s.init("container-1");

            // 初始化弹出的对话框
            $('#dialog').dialog({
                autoOpen: false,
                modal: true
            });

            refreshConfig(null, 2);
        });

        function doSubmit(callback) {
            $("#divret").html("");
            var ips = getIps();
            for (var i = 0; i < ips.length; i++) {
                var ip = ips[i];
                $("#divret").append("<div id='div" + i + "' style='border:solid 1px blue;'>" + ip + "处理中……<br /></div>");
                callback(ip, i);
            }
        }

        function getIps() {
            var ips = $("#txtIp").val();
            var ret = [];
            if (ips.length > 0) {
                var iparr = ips.split(';');
                for (var i = 0, j = iparr.length; i < j; i++) {
                    var ip = $.trim(iparr[i]);
                    if (ip.length > 0)
                        ret.push(ip);
                }
            }
            if (ret.length <= 0) {
                $("#txtIp").val("127.0.0.1");
                ret.push("127.0.0.1");
            }
            return ret;
        }

        function ajaxSend(para, callback) {
            var url = '<%=m_currentUrl %>' + "?" + new Date();
            $.ajax({
                url: url,
                //dataType: "json",
                type: "POST",
                data: para,
                success: callback,
                error: ajaxError
            });
        }
        // ajax失败时的回调函数
        function ajaxError(httpRequest, textStatus, errorThrown) {
            // 通常 textStatus 和 errorThrown 之中, 只有一个会包含信息
            //this; // 调用本次AJAX请求时传递的options参数
            alert(textStatus + errorThrown);
        }

        function showHide(obj, forceShow) {
            obj = $(obj).next("span:eq(0)");
            if (forceShow == undefined)
                forceShow = !obj.is(":visible");
            if (forceShow) {
                obj.show();
            } else {
                obj.hide();
            }
        }
        function doLogout() {
            location.href = '?logout=1';
        }
    </script>
</head>
<body style="font-size:12px;">
<div>
    要测试的服务器IP列表:<input type="text" id="txtIp" style="width:800px;" value="<%=m_ipLst %>" /><br/>
    (remote ip:<span style="color:blue;"><%=m_remoteIpLst %></span>　local ip:<span style="color:blue;"><%=m_localIp %></span>)
    <hr />
</div>
<div id="container-1">
    <ul class="ui-tabs-nav">
        <li class="" style="display: none"><a href="#fragment1"><span>配置与内存相关</span></a></li>
        <li class="ui-tabs-selected"><a href="#fragment2"><span>Telnet测试</span></a></li>
        <li class=""><a href="#fragment3"><span>Sql查询</span></a></li>
        <li class=""><a href="#fragment5"><span>Redis查询</span></a></li>
        <li class=""><a href="#fragment6"><span>MySql表结构对比</span></a></li>
    </ul>
    <input type="button" value="退出登录" onclick="doLogout();"/>
    <!-- 配置相关 -->
    <div style="display: none;" class="ui-tabs-panel ui-tabs-hide" id="fragment1">
        <table border="1" cellpadding="4" cellspacing="0">
            <tr>
                <td style="width:130px;text-align:right;">类全名：<br/>带命名空间</td>
                <td style="width:700px;text-align:left;">
                    <input type="text" id="txtClassName" style="width:80%;" value="" />
                    <input type="button" value="查看类数据" onclick="refreshConfig(this, 2);" />　
                    <br/>
                    如： Beinet.Core.LockDefault
                </td>
            </tr>
            <tr>
            </tr>
            <tr><td style="text-align:right;">
                HttpRuntime.Cache：
            </td><td>
                <input type="text" id="txtCacheName" style="width:60%;" value="" />
                <input type="button" value="查看缓存明细" onclick="refreshConfig(this, 3);" />　
                <input type="button" value="清空内存缓存" onclick="refreshConfig(this, 4);" />　
            </td></tr>
            <tr><td style="text-align:center;" colspan="2">
            </td></tr>
        </table>
        <script type="text/javascript">
            function refreshConfig(btn, flg) {
                var className = $.trim($("#txtClassName").val());
                var cacheName = '';

                if (flg === 3) {
                    cacheName = $.trim($("#txtCacheName").val());
                    if (cacheName.length === 0 && !confirm("您确认要查看缓存明细吗？数据量大可能很慢，甚至卡住")) {
                        return;
                    }
                } else if (flg === 4) {
                    cacheName = $.trim($("#txtCacheName").val());
                    if (cacheName.length === 0 && !confirm("您确认要清空全部内存缓存吗？此操作后果比较严重哦")) {
                        return;
                    } else if (cacheName.length !== 0 && !confirm("您确认要删除内存缓存" + cacheName + "吗？")) {
                        return;
                    }
                }
                $("#divret").html("");

                $(btn).attr("disabled", "disabled");
                doSubmit(function (ip, idx) {
                    var para = {};
                    para.className = className;
                    para.ip = ip;
                    para.flg = 'showconfig';
                    para.f = flg;
                    para.cache = cacheName;
                    ajaxSend(para, function (msg) {
                        var obj = $("#div" + idx);
                        obj.html("<pre>" + ip + "处理完成，返回如下：(处理时间：" +
                            (new Date()).toString() + ")\r\n" + msg.replace(/<(?!\/?(span|a))/g, "&lt;") + "</pre>");
                    });
                });
                $(btn).removeAttr("disabled");
            }
        </script>
    </div>

    <!-- Telnet测试 -->
    <div style="display: block;" class="ui-tabs-panel ui-tabs-hide" id="fragment2">
        <hr style="height: 5px; background-color: green" />
        目标服务器IP和端口列表（ip:端口 换行 ip:端口）：<br />
        <textarea id="txtTelnetIp" rows="5" cols="40">testredis.beinet.cn:6379
testmysql.beinet.cn:3306</textarea><br/>
        <input type="button" value="测试" onclick="telnetTest(this);" style="width:200px;"/>
        <script type="text/javascript">
            function telnetTest(btn) {
                var ipTo = $.trim($("#txtTelnetIp").val());
                if (ipTo.length <= 0) {
                    alert("请输入测试IP和端口列表");
                    return;
                }
                $("#divret").html("");

                $(btn).attr("disabled", "disabled");
                doSubmit(function (ip, idx) {
                    var para = "flg=telnet&ip=" + ip + "&tip=" + ipTo;
                    ajaxSend(para, function (msg) {
                        $("#div" + idx).html("<pre>" + ip + "返回如下：(处理时间：" +
                            (new Date()).toString() + ")\r\n" + msg.replace(/<(?!\/?(span|a))/g, "&lt;") + "</pre>");
                    });
                });
                $(btn).removeAttr("disabled");
            }
        </script>
    </div>

    <!-- Sql查询 -->
    <div class="ui-tabs-panel ui-tabs-hide" id="fragment3">
        <hr style="height: 5px; background-color: green" />
        <div>说明：Sql查询只能测试单机，不能多服务器查询，多服务器请用Telnet测试</div>
        <div style="color: red;">注意：只允许使用select查询语句，不允许update等修改性语句，且select语句不是很耗性能的语句</div>
        <br />
        数据库类型：<label><input type="radio" name="radDbType" value="mysql" checked="checked"/>mysql</label>
        <label><input type="radio" name="radDbType" value="sqlserver"/>sqlserver</label>
        <label><input type="radio" name="radDbType" value="postgresql"/>postgresql</label><br/>
        数据库连接串：<input type="text" id="txtSqlCon" value="server=testmysql.beinet.cn;uid=bn;pwd=bn.123;database=mysql" style="width:900px"/><br/>
        SQL：<textarea id="txtSql" rows="2" cols="20" style="height:200px;width:1000px;">SELECT t.table_schema,t.table_name, t.table_rows, t.avg_row_length, t.data_length , t.index_length, t.auto_increment 下一自增值, t.create_time, t.table_comment
FROM information_schema.tables t
WHERE t.table_type = 'base table'
ORDER BY t.table_schema, t.table_name
LIMIT 100</textarea><br/>
        <input type="button" value="测试" onclick="sqlTest(this);" style="width:200px;"/>
        <input type="button" value="备份指定SQL结果数据" onclick="sqlTest(this, true);" style="width:200px;"/>
        注意：备份只支持单表SQL，不支持多表join
        <script type="text/javascript">
            function sqlTest(btn, isBak) {
                var constr = $.trim($("#txtSqlCon").val());
                if (constr.length <= 0) {
                    alert("请输入数据库连接串");
                    return;
                }
                var sql = encodeURIComponent($.trim($("#txtSql").val()));
                if (sql.length <= 0) {
                    alert("请输入SQL");
                    return;
                }
                $("#divret").html("查询中，请稍候……");

                var dbtype = $('input[name="radDbType"]:checked').val();
                $(btn).attr("disabled", "disabled");
                var para = "flg=sql&sql=" + sql + "&constr=" + constr + "&dbtype=" + dbtype;
                if (isBak)
                    para += '&isbak=1';
                ajaxSend(para, function (msg) {
                    if (isBak) {
                        window.open('').document.write('<pre>' + msg + '</pre>');
                    } else
                        $("#divret").html(msg);
                });
                $(btn).removeAttr("disabled");
            }
        </script>
    </div>

    <!-- Redis管理 -->
    <div class="ui-tabs-panel ui-tabs-hide" id="fragment5">
        <hr style="height: 5px; background-color: green" />
        <table>
            <tr>
                <td>Redis IP和端口（格式：密码@ip:端口）：</td>
                <td>
                    <select onchange="$('#txtRedisServer').val($(this).val());">
                        <optgroup label="QA-Redis">
                            <option>testredis.beinet.cn:6379</option>
                        </optgroup>
                    </select>
                    <input type="text" id="txtRedisServer" style="width: 400px" value="rFWcC9KnrFty@testredis.beinet.cn:6379"/>
                </td>
            </tr>
            <tr>
                <td>Redis DB：</td><td><input type="text" id="txtRedisDB" style="width: 50px" value="0"/></td>
            </tr>
            <tr>
                <td>命令：</td><td><input type="text" id="txtRedisCommand" style="width: 500px" value="info"/></td>
            </tr>
            <tr>
                <td><input type="button" value="提交" onclick="sendRedis(this);"/></td>
            </tr>
        </table>
        <script type="text/javascript">
            function sendRedis(btn) {
                var ipTo = $.trim($("#txtRedisServer").val());
                if (ipTo.length <= 0) {
                    alert("请输入RedisIP和端口");
                    return;
                }
                var sql = encodeURIComponent($.trim($("#txtRedisCommand").val()));
                if (sql.length <= 0) {
                    alert("请输入命令");
                    return;
                }
                $("#divret").html("查询中，请稍候……");

                $(btn).attr("disabled", "disabled");
                var para = "flg=redis&cm=" + sql + "&tip=" + ipTo + '&rdb=' + $('#txtRedisDB').val();
                ajaxSend(para, function (msg) {
                    $("#divret").html("<pre>" + msg + "</pre>");
                });
                $(btn).removeAttr("disabled");
            }
        </script>
    </div>
    
    <!-- MySql表结构对比 -->
    <div class="ui-tabs-panel ui-tabs-hide" id="fragment6">
        <hr style="height: 5px; background-color: green" />
        <form method="post" enctype="multipart/form-data" target="_blank">
            <input type="hidden" name="flg" value="mysqlarch"/>
        <table>
            <tr>
                <td>线下数据库连接串：</td>
                <td>
                    <input type="text" id="txtTestDB" name="testdb" style="width: 800px" 
                        value="server=testmysql.beinet.cn;uid=bn;pwd=bn.123;database=mysql"/>
                </td>
            </tr>
            <tr>
                <td>线上数据库连接串：</td>
                <td>
                    <input type="text" id="txtOnlineDB" name="onlinedb" style="width: 800px" 
                           value="server=testmysql.beinet.cn;uid=bn;pwd=bn.123;database=mysql"/>
                </td>
            </tr>
            <tr>
                <td>
                    <input type="submit" value="显示差异"/>
                    <label style="font-weight: bold; color: blue;"><input type="checkbox" name="chkCase"/>表和字段名要区分大小写</label>
                </td>
            </tr>
        </table>
        </form>
    </div>

    <hr style="height: 5px; background-color: green" />
    <div id="divret"></div>
</div>    
</body>
</html>
