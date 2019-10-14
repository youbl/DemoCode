using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Beinet.SqlLog
{
    /// <summary>
    /// 用于注册和执行SQL拦截器的类
    /// </summary>
    public class SqlFilterCollection
    {
        internal static List<InnerItem> Filters { get; set; } = new List<InnerItem>();

        static SqlFilterCollection()
        {
            SqlFilterCollection.Register(new SqlFilter());
        }

        /// <summary>
        /// 注册Sql执行的拦截器
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="order"></param>
        public static void Register(IFilter filter, int order = 0)
        {
            lock (Filters)
            {
                Filters.Add(new InnerItem {Filter = filter, Order = order});
                // 先按Order排序，再按id排序
                Filters = Filters.OrderBy(o => o.Order).ThenBy(o => o.Id).ToList();
            }
        }

        internal class InnerItem
        {
            /// <summary>
            /// 加入顺序
            /// </summary>
            public int Id { get; } = GetIdx();
            /// <summary>
            /// 拦截方法
            /// </summary>
            public IFilter Filter { get; set; }

            /// <summary>
            /// 执行顺序，默认值10000，从小到大
            /// </summary>
            public int Order { get; set; } = 10000;

            private static int idx;

            static int GetIdx()
            {
                return Interlocked.Increment(ref idx);
            }
        }
    }
}