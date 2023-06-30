using System.Data.Common;

namespace Beinet.SqlLog
{
    public interface IFilter
    {
        /// <summary>
        /// 执行sql之前的方法
        /// </summary>
        /// <param name="command">执行的命令对象</param>
        void BeforeExecute(DbCommand command);

        /// <summary>
        /// 执行sql之后的方法
        /// </summary>
        /// <param name="command">执行的命令对象</param>
        /// <param name="result">执行的结果</param>
        /// <param name="costMillis">执行耗时</param>
        void AfterExecute(DbCommand command, object result, long costMillis);
    }
}