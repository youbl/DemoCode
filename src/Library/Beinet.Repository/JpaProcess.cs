using System;
using System.Collections.Generic;
using System.Reflection;
using Beinet.Repository.Entitys;
using Beinet.Repository.Repositories;
using Beinet.Repository.Tools;

namespace Beinet.Repository
{
    /// <summary>
    /// Jpa方法执行主调类
    /// </summary>
    public class JpaProcess
    {
        private Type _repositoryType;

        /// <summary>
        /// 业务仓储接口类型
        /// </summary>
        public Type RepositoryType
        {
            get => _repositoryType;
            set
            {
                _repositoryType = value;
                Init();
            }
        }

        private Type _entityType;

        /// <summary>
        /// 实体类的类型
        /// </summary>
        public Type EntityType
        {
            get => _entityType;
            set
            {
                _entityType = value;
                Init();
            }
        }

        private Type _keyType;

        /// <summary>
        /// 实体类主键字段类型
        /// </summary>
        public Type KeyType
        {
            get => _keyType;
            set
            {
                _keyType = value;
                Init();
            }
        }

        /// <summary>
        /// 仓储操作类
        /// </summary>
        private object _jpaRepositoryBase;

        /// <summary>
        /// 收集的实体类相关信息
        /// </summary>
        private EntityData _data;

        /// <summary>
        /// 收集的仓储方法列表，key为接口的方法，value为JpaRepositoryBase实现类的方法
        /// </summary>
        private Dictionary<MethodInfo, object> _arrJpaMethods;

        private void Init()
        {
            if (RepositoryType == null || KeyType == null || EntityType == null)
                return;

            var helper = new EntityMySqlHelper();

            _data = helper.ParseEntity(EntityType, KeyType);
            if (string.IsNullOrEmpty(_data.KeyName))
                throw new ArgumentException("未设置主键：" + EntityType.FullName);

            var runnerType = typeof(JpaRepositoryBase<,>).MakeGenericType(EntityType, KeyType);
            _jpaRepositoryBase = Activator.CreateInstance(runnerType);

            ((RepositoryProperty) _jpaRepositoryBase).Data = _data;
            ((RepositoryProperty)_jpaRepositoryBase).DataSource = helper.ParseConnectionString(RepositoryType);

            _arrJpaMethods = helper.ParseRepostory(RepositoryType, _jpaRepositoryBase.GetType());
        }

        public object Process(MethodInfo method, object[] parameters)
        {
            if (!_arrJpaMethods.TryGetValue(method, out var baseMethod) || baseMethod == null)
                throw new ArgumentException("指定的方法未找到：" + EntityType.FullName + ": " + method.Name);

            // 调用基础接口定义的方法
            if (baseMethod is MethodInfo baseMethodInfo)
                return baseMethodInfo.Invoke(_jpaRepositoryBase, parameters);

            // todo：调用自定义方法，根据命名规则处理

            return TypeHelper.GetDefaultValue(method.ReturnType);
        }


    }
}