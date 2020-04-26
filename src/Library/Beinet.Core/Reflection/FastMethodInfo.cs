

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Beinet.Core.Reflection
{

    /// <summary>
    /// 反射性能优化类，参考：
    /// https://stackoverflow.com/questions/10313979/methodinfo-invoke-performance-issue
    /// </summary>
    internal class FastMethodInfo
    {
        private delegate object ReturnValueDelegate(object instance, object[] arguments);
        private delegate void VoidDelegate(object instance, object[] arguments);

        public FastMethodInfo(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));
            var classType = methodInfo.ReflectedType;
            if (classType == null)
                throw new ArgumentException("所属类型为空", nameof(methodInfo));

            var instanceExpression = Expression.Parameter(typeof(object), "instance");
            var argumentsExpression = Expression.Parameter(typeof(object[]), "arguments");
            var argumentExpressions = new List<Expression>();
            var parameterInfos = methodInfo.GetParameters();
            for (var i = 0; i < parameterInfos.Length; ++i)
            {
                var parameterInfo = parameterInfos[i];
                argumentExpressions.Add(Expression.Convert(Expression.ArrayIndex(argumentsExpression, Expression.Constant(i)), parameterInfo.ParameterType));
            }
            var callExpression = Expression.Call(!methodInfo.IsStatic ? Expression.Convert(instanceExpression, classType) : null, methodInfo, argumentExpressions);
            if (callExpression.Type == typeof(void))
            {
                var voidDelegate = Expression.Lambda<VoidDelegate>(callExpression, instanceExpression, argumentsExpression).Compile();
                Delegate = (instance, arguments) => { voidDelegate(instance, arguments); return null; };
            }
            else
                Delegate = Expression.Lambda<ReturnValueDelegate>(Expression.Convert(callExpression, typeof(object)), instanceExpression, argumentsExpression).Compile();
        }

        private ReturnValueDelegate Delegate { get; }

        /// <summary>
        /// 执行方法 
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public object Invoke(object instance, params object[] arguments)
        {
            return Delegate(instance, arguments);
        }
    }
}
