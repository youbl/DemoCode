using Beinet.Feign;

namespace Beinet.FeignDemoConsole
{
    /// <summary>
    /// Url为占位符，如{CCUrl}，表示读取配置 ConfigurationManager.AppSettings["CCUrl"]
    /// </summary>
    [FeignClient("", Url = "{CCUrl}")]
    public interface FeignApiTest
    {
        /// <summary>
        /// 无参数 返回数值，paraConfig是读取ConfigurationManager.AppSettings["paraConfig"]
        /// </summary>
        /// <returns></returns>
        [GetMapping("test/api.aspx?flg=1&readconfig={paraConfig}")]
        int GetMs();

        /// <summary>
        /// 有参数 返回数值
        /// </summary>
        /// <returns></returns>
        [GetMapping("test/api.aspx?flg=2")]
        int GetAdd([RequestNone]int n1, int n2);

        /// <summary>
        /// 有参数 POST返回数值
        /// </summary>
        /// <returns></returns>
        [PostMapping("test/api.aspx?flg=2&n1={num1}&n2={num2}")]
        int PostAdd([RequestParam]int num1, [RequestParam]int num2);


        /// <summary>
        /// 直接返回响应的完整字符串
        /// </summary>
        /// <returns></returns>
        [GetMapping("test/api.aspx", Headers = new []{"abcd", "efgh=ijklmn=dd df", "aabc=", "=ddddd","user-agent  = b einet1.0 alpha "})]
        string GetUserStr();

        /// <summary>
        /// GET无参，返回对象
        /// </summary>
        /// <returns></returns>
        [GetMapping("test/api.aspx")]
        FeignDtoDemo GetUser();


        /// <summary>
        /// GET有参，返回对象
        /// </summary>
        /// <returns></returns>
        [GetMapping("test/api.aspx")]
        FeignDtoDemo GetUser(int id, [RequestParam]string name, [RequestHeader] string argHeader);



        /// <summary>
        /// POST无参，返回对象
        /// </summary>
        /// <returns></returns>
        [PostMapping("test/api.aspx")]
        FeignDtoDemo PostUser();

        /// <summary>
        /// POST有参，返回对象
        /// </summary>
        /// <returns></returns>
        [PostMapping("test/api.aspx")]
        FeignDtoDemo PostUser(int id, [RequestParam]string name);

        /// <summary>
        /// POST对象，返回对象
        /// </summary>
        /// <returns></returns>
        [PostMapping("test/api.aspx")]
        FeignDtoDemo PostUser(FeignDtoDemo user);

        /// <summary>
        /// POST对象，返回对象
        /// </summary>
        /// <returns></returns>
        [PostMapping("test/api.aspx?id={para2}")]
        FeignDtoDemo PostUser(FeignDtoDemo user, [RequestParam]int para2);
    }
    

}