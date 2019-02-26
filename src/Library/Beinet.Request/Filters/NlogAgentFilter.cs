using System;
using System.Net;
using System.Text;
using System.Web;
using NLog;

namespace Beinet.Request.Filters
{
    /// <summary>
    /// 编辑HttpWebRequest的UserAgent
    /// </summary>
    public class NLogFilter : HttpWebRequestFilter
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public override void AfterGetResponse(HttpWebRequest request, HttpWebResponse response, Exception exception)
        {
            try
            {
                if (exception != null)
                {
                    logger.Error(() => GetMessage(request, response, exception));
                }
                else
                {
                    logger.Debug(() => GetMessage(request, response, null));
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        private string GetMessage(HttpWebRequest request, HttpWebResponse response, Exception exception)
        {
            StringBuilder sb = GetRequestMessage(request);
            sb.Append(GetResponseContent(response));
            if (exception != null)
            {
                sb.AppendFormat("Exception:{0}.", exception);
            }
            return sb.ToString();
        }

        private StringBuilder GetRequestMessage(HttpWebRequest request)
        {
            StringBuilder sb = new StringBuilder();
            if (request != null)
            {
                var method = request.Method;
                sb.AppendFormat("{0}:{1},", method, request.RequestUri);

                if (request is ExtendHttpWebRequest webRequest &&
                    (method == "POST" || method == "PUT" || method == "PATCH") &&
                    webRequest.HasRequestStream)
                {
                    // 获取BODY里的数据
                    sb.AppendFormat("Content: {0},", HttpUtility.UrlDecode(webRequest.GetRequestBodyStr()));
                }
                sb.AppendFormat("Header:{{{0}}}", request.Headers);
            }
            return sb;
        }
        private StringBuilder GetResponseContent(HttpWebResponse response)
        {
            StringBuilder sb = new StringBuilder();
            if (response != null)
            {
                sb.AppendFormat("ReponseHeader:{{{0}}}", response.Headers);
                string content = "";
                if (response is ExtendHttpWebResponse webResponse)
                {
                    content = webResponse.GetResponseStr();
                }
                sb.AppendFormat("ReponseContent:{0}.", content);
            }
            return sb;
        }
    }
}
