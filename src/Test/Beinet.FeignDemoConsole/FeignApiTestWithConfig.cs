using Beinet.Feign;

namespace Beinet.FeignDemoConsole
{
    [FeignClient("", Url = "https://www.beinet.cn", Configuration = typeof(FeignConfigDeom))]//
    public interface FeignApiTestWithConfig
    {
        /// <summary>
        /// 无参数 返回数值
        /// </summary>
        /// <returns></returns>
        [GetMapping("test/api.aspx?flg=1")]
        int GetMs();

        /// <summary>
        /// 有参数 返回数值
        /// </summary>
        /// <returns></returns>
        [GetMapping("test/api.aspx?flg=2")]
        int GetAdd(int n1, int n2);

        /// <summary>
        /// 有参数 POST返回数值
        /// </summary>
        /// <returns></returns>
        [PostMapping("test/api.aspx?flg=2&n1={num1}&n2={num2}")]
        int PostAdd(int num1, int num2);


        /// <summary>
        /// 直接返回响应的完整字符串
        /// </summary>
        /// <returns></returns>
        [GetMapping("test/api.aspx")]
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
        FeignDtoDemo GetUser(int id, string name);



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
        FeignDtoDemo PostUser(int id, string name);

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
        FeignDtoDemo PostUser(FeignDtoDemo user, int para2);
    }

}