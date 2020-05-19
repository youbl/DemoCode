

using System;
using Beinet.Repository.Repositories;

namespace Beinet.Repository
{
    /// <summary>
    /// Jpa方法执行主调类
    /// </summary>
    class JpaProcess
    {
        /// <summary>
        /// 实体类的类型
        /// </summary>
        public Type EntityType { get; set; }

        /// <summary>
        /// 实体类主键字段类型
        /// </summary>
        public Type KeyType { get; set; }

        /// <summary>
        /// 仓储操作类
        /// </summary>
        private object _jpaRepositoryBase;

        private object jpaRepositoryBase
        {
            get
            {
                if (_jpaRepositoryBase == null)
                {
                    _jpaRepositoryBase = typeof(JpaRepositoryBase<,>).MakeGenericType(EntityType, KeyType);
                }

                return _jpaRepositoryBase;
            }
        }

        public object Process(string methodName, Type returnType)
        {

            return null;
        }

        public void ParseEntity()
        {
        }

    }
}
