using System.Data.Common;

namespace Beinet.SqlLog
{
    /// <summary>
    /// 用于执行SQL拦截器的类
    /// </summary>
    internal class SqlFilterProcess
    {
        public static void Prefix(DbCommand __instance)
        {
            foreach (var filter in SqlFilterCollection.Filters)
            {
                filter.Filter.BeforeExecute(__instance);
            }
        }

        public static void Postfix(DbCommand __instance)
        {
            foreach (var filter in SqlFilterCollection.Filters)
            {
                filter.Filter.AfterExecute(__instance);
            }
        }
    }
}