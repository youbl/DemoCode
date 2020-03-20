
using Beinet.Feign;

namespace Beinet.FeignDemoConsole
{
    // {env} 从app.config文件中读取配置，也可以整个Url读取配置，如 Url="{env}"
    [FeignClient("", Url = "https://47.107.125.247/{env}/cc")]
    public interface FeignTestPlace
    {
        // 占位符 num1和num2从方法参数读取，
        // 占位符 ConfigKey从app.config文件中读取配置
        [GetMapping("test/api.aspx?n1={num1}&n2={num2}&securekey={ConfigKey}")]
        FeignDtoDemo GetDtoObj([RequestNone]int num1, [RequestNone]int num2);
    }
}