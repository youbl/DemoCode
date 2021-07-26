using System.Web.Http;
using System.Web.Routing;
using DemoCodeWeb.App_Start;

namespace DemoCodeWeb
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API 配置和服务

            // Web API 路由
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new {id = RouteParameter.Optional}
            );

            // 添加一个URL处理类，处理 http://localhost/actuator/info 这种请求
            RouteTable.Routes.Add(new Route("actuator/info", new ActuatorInfoRouteHandler()));
            // 在这里 RouteTable.Routes 与 config.Routes 是同步的，根据搜索的资料，RouteTable仅支持IIS，而config.Routes还支持非IIS，比如类似core的自启动
        }
    }
}