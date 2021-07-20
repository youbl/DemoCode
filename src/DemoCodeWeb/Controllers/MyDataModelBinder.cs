using System;
using System.Net.Http.Headers;
using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;

namespace DemoCodeWeb.Controllers
{
    public class MyDataModelBinder : IModelBinder
    {
        public bool BindModel(HttpActionContext actionContext, ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType != typeof(MyData))
            {
                return false;
            }

            MyData data = new MyData();
            data.Agent = Convert.ToString(actionContext.Request.Headers.UserAgent);
            data.Method = Convert.ToString(actionContext.Request.Method);
            data.Url = Convert.ToString(actionContext.Request.RequestUri);

            bindingContext.Model = data;
            return true;
        }
    }

    public class MyData
    {
        public string Agent { get; set; }
        public string Method { get; set; }
        public string Url { get; set; }

        public override string ToString()
        {
            return Agent + ", " + Method + " " + Url;
        }
    }
}