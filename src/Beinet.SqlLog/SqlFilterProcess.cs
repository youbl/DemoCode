using System.Data.Common;
using System.Diagnostics;

namespace Beinet.SqlLog
{
    /// <summary>
    /// 用于执行SQL拦截器的类
    /// </summary>
    internal static class SqlFilterProcess
    {
        /// <summary>
        /// 参数命名固定，参考： https://harmony.pardeike.net/articles/patching-injections.html
        /// </summary>
        /// <param name="__instance">拦截方法所属对象</param>
        /// <param name="__state">拦截方法要传递给Postfix的数据</param>
        public static void Prefix(DbCommand __instance, out Stopwatch __state)
        {
            __state = new Stopwatch();
            __state.Start();

            foreach (var filter in SqlFilterCollection.Filters)
            {
                filter.Filter.BeforeExecute(__instance);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="__instance">拦截方法所属对象</param>
        /// <param name="__result">拦截方法的返回值</param>
        /// <param name="__state">Prefix传递过来的数据</param>
        public static void Postfix(DbCommand __instance, object __result, Stopwatch __state)
        {
            __state.Stop();
            var costMillis = __state.ElapsedMilliseconds;

            foreach (var filter in SqlFilterCollection.Filters)
            {
                filter.Filter.AfterExecute(__instance, __result, costMillis);
            }
        }
    }
}