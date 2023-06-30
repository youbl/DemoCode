using System.Web;
using System.Web.Routing;

namespace Beinet.EnumList
{
    /// <summary>
    /// 遍历所有类库，并列出所有的枚举，进行展示
    /// </summary>
    public class EnumHttpHandler : IHttpHandler, IRouteHandler
    {
        /// <summary>
        /// IHttpHandler接口
        /// </summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(EnumSearcher.ArrEnums));
        }

        public bool IsReusable { get; } = true;

        /// <summary>
        /// IRouteHandler接口
        /// </summary>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return this;
        }
    }
}
