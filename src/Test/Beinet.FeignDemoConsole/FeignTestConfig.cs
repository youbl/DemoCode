
using Beinet.Feign;

namespace Beinet.FeignDemoConsole
{
    [FeignClient("", Url = "https://47.107.125.247", Configuration = typeof(FeignConfigDeom))]
    public interface FeignTestConfig
    {
        // 发起正常请求
        [GetMapping("test/api.aspx")]
        FeignDtoDemo GetDtoObj();

        // 发起出错的请求
        [GetMapping("test/api.aspx?flg=3")]
        FeignDtoDemo GetErr();
    }


}