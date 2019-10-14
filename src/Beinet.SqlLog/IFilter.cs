using System.Data.Common;
using System.Text;
using NLog;

namespace Beinet.SqlLog
{
    public interface IFilter
    {
        /// <summary>
        /// 执行sql之前的方法
        /// </summary>
        /// <param name="command"></param>
        void BeforeExecute(DbCommand command);

        /// <summary>
        /// 执行sql之后的方法
        /// </summary>
        /// <param name="command"></param>
        void AfterExecute(DbCommand command);
    }
}
