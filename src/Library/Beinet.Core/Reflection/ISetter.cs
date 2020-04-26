
namespace Beinet.Core.Reflection
{

    /// <summary>
    /// Property/Field的设置接口
    /// </summary>
    internal interface ISetter
    {
        void Set(object target, object val);
    }
}
