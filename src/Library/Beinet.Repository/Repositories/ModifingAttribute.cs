using System;

namespace Beinet.Repository.Repositories
{
    /// <summary>
    /// 仓储类的方法是否属于DML，即INSERT、UPDATE或DELETE
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ModifingAttribute : Attribute
    {
    }
}
