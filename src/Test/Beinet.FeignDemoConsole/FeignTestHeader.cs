using Beinet.Feign;

namespace Beinet.FeignDemoConsole
{
    [FeignClient("", Url = "https://47.107.125.247")]
    public interface FeignTestHeader
    {
        // 在方法特性里增加header
        [GetMapping("test/api.aspx", Headers = new string[] { "headerName=headerValue", "user-agent=beinet feign1234" })]
        FeignDtoDemo GetDtoObj();

        // 在参数特性里增加header，一个使用参数名作为header name，一个使用自定义header name
        [GetMapping("test/api.aspx")]
        FeignDtoDemo GetDtoObj([RequestHeader]string headerName, [RequestHeader("RealHeaderName")]string arg2);

    }


}