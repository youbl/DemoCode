using System;

using Beinet.Feign;

namespace Beinet.FeignDemoConsole
{
    [FeignClient("", Url = "https://47.107.125.247")]
    public interface FeignTestURI
    {
        // 参数中存在URI类型，且不为空时，会忽略FeignClient的Url配置
        [GetMapping("test/api.aspx")]
        FeignDtoDemo GetDtoObj(Uri uri);

        // 参数中存在URI类型，且不为空时，会忽略FeignClient的Url配置
        [GetMapping("test/api.aspx")]
        FeignDtoDemo GetDtoObj(string arg1, Uri uri);
    }


}