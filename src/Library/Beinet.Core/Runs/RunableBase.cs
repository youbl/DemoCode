namespace Beinet.Core.Runs
{
    /// <summary>
    /// 带可执行方法的接口默认实现类
    /// </summary>
    public abstract class RunableBase : IRunable
    {
        /// <summary>
        /// 是否允许启动
        /// </summary>
        public abstract bool Enable { get; }

        /// <summary>
        /// 具体执行方法
        /// </summary>
        protected abstract void Process();

        /// <summary>
        /// 判断并启动方法
        /// </summary>
        public virtual void Run()
        {
            if (!Enable)
                return;

            Process();
        }
    }
}