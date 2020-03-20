<%@ Page Language="C#" ValidateRequest="false" EnableViewState="false" EnableSessionState="false" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Net" %>
<%@ Import Namespace="System.Net.Sockets" %>
<%@ Import Namespace="Newtonsoft.Json" %>


<script type="text/C#" language="C#" runat="server">

    protected void Page_Load(object sender, EventArgs e)
    {
        Response.Cache.SetNoStore(); // 这一句会导致Response.WriteFile 无法下载

        int flg = GetInt("flg");
        if (flg != 0)
        {
            switch (flg)
            {
                case 1:
                    Response.Write(DateTime.Now.Millisecond);
                    return;
                case 2:
                    Response.Write(GetInt("n1") + GetInt("n2"));
                    return;
                case 3:
                    int.Parse("abc");
                    return;
            }
        }

        Response.ContentType = "application/json";

        int id = GetInt("id");
        if (id == 0)
            id = PostInt("id");
        string name = GetStr("name");
        if (name.Length == 0)
            name = PostStr("name");

        string stream = GetStream();
        Users ret = null;
        try
        {
            if(!string.IsNullOrEmpty(stream))
                ret=JsonConvert.DeserializeObject<Users>(stream);
        }
        catch
        {
        }
        if (ret == null)
        {
            ret = new Users();
            ret.Name = "fankuai " + name;
            ret.Id = id > 0 ? id : 123456789;
        }
        else
        {
            ret.Name += "-FromStream";
            ret.Id *= 100;
        }
        ret.url = Request.HttpMethod + " " + Request.Url;
        ret.Post = Request.Form.ToString();
        ret.Stream = stream;
        ret.Headers = new Dictionary<string, string>();
        foreach (string header in Request.Headers)
        {
            ret.Headers.Add(header, Request.Headers[header]);
        }

        ret.AddTime = DateTime.Now.AddYears(-20);
        ret.Works = new Work[3];

        Work work = new Work();
        ret.Works[0] = work;
        work.Id = 88;
        work.Company = "百度";
        work.StartTime = DateTime.Parse("2010-6-1 14:56:32.579");
        work.EndTime = DateTime.Parse("2017-11-1 18:56:32.321");

        work = new Work();
        ret.Works[1] = work;
        work.Id = 99;
        work.Company = "米客";
        work.StartTime = DateTime.Parse("2017-11-2 14:56:32.579");
        work.EndTime = DateTime.MaxValue;

        Response.Write(JsonConvert.SerializeObject(ret));
    }

    int GetInt(string name)
    {
        string val = Request.QueryString[name];
        if (string.IsNullOrEmpty(val))
            return 0;

        int ret;
        int.TryParse(val, out ret);
        return ret;
    }

    string GetStr(string name)
    {
        string val = Request.QueryString[name];
        if (string.IsNullOrEmpty(val))
            return "";
        return val;
    }


    int PostInt(string name)
    {
        string val = Request.Form[name];
        if (string.IsNullOrEmpty(val))
            return 0;

        int ret;
        int.TryParse(val, out ret);
        return ret;
    }

    string PostStr(string name)
    {
        string val = Request.Form[name];
        if (string.IsNullOrEmpty(val))
            return "";
        return val;
    }

    string GetStream()
    {
        if (Request.InputStream.Length > 0)
        {
            using (var sr = new StreamReader(Request.InputStream, Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }
        return "";
    }

    public class Users
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime AddTime { get; set; }
        public Work[] Works { get; set; }

        public string url { get; set; }
        public string Stream { get; set; }
        public string Post { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }

    public class Work
    {
        public int Id { get; set; }
        public string Company { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
</script>
