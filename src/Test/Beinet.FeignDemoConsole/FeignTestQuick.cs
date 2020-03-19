using Beinet.Feign;

namespace Beinet.FeignDemoConsole
{
    [FeignClient("", Url = "https://47.107.125.247")]
    public interface FeignTestQuick
    {
        // http无参接口 无返回值
        [GetMapping("test/api.aspx?flg=1")]
        void Get();

        // http无参接口返回数值
        [GetMapping("test/api.aspx?flg=1")]
        int GetMs();

        // http有参接口返回数值，通过RequestParam把参数拼接到url里
        [GetMapping("test/api.aspx?flg=2")]
        int GetAdd([RequestParam]int n1, [RequestParam("n2")]int second2);

        // http有参接口，POST返回数值，通过占位符把参数拼接到url里
        [PostMapping("test/api.aspx?flg=2&n1={num1}&n2={num2}")]
        int PostAdd([RequestNone]int num1, [RequestNone]int num2);

        // http无参接口返回json字符串，不需要反序列化，想自行处理可以用
        [GetMapping("test/api.aspx")]
        string GetDtoStr();

        // http无参接口返回dto对象
        [GetMapping("test/api.aspx")]
        FeignDtoDemo GetDtoObj();

        // POST有参，返回dto对象，通过RequestParam把参数拼接到url里
        [PostMapping("test/api.aspx")]
        FeignDtoDemo PostDtoObj([RequestParam]int id, [RequestParam]string name);

        // POST参数为对象，并自定义url参数名为urlPara，返回dto对象
        [PostMapping("test/api.aspx")]
        FeignDtoDemo PostDtoObj(FeignDtoDemo dto, [RequestParam("urlPara")]string arg2);
    }


}