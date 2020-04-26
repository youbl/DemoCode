

namespace Beinet.Core.Reflection
{

    /// <summary>
    /// Property/Field的获取接口
    /// </summary>
    internal interface IGetter
    {
        object Get(object target);
    }
}
