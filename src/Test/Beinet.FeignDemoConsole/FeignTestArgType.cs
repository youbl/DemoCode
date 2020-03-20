using System;

using Beinet.Feign;

namespace Beinet.FeignDemoConsole
{
    [FeignClient("", Url = "https://47.107.125.247")]
    public interface FeignTestArgType
    {
        // 参数中存在Type类型，且不为空时，会把返回值反序列化为该Type，注意type必须是返回类型的子类
        [GetMapping("test/api.aspx")]
        object GetDtoObj(Type type);

        // 参数中存在URI类型，且不为空时，会忽略FeignClient的Url配置
        [GetMapping("test/api.aspx")]
        object GetDtoObj(string arg1, Type type);

        // 参数中存在Type类型，且Type不是返回类型的子类时，会抛异常
        [GetMapping("test/api.aspx")]
        FeignDtoDemo GetErr(Type type);

    }


}