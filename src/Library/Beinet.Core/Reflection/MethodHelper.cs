

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Beinet.Core.Reflection
{

    /// <summary>
    /// 方法执行相关的辅助类
    /// </summary>
    public static class MethodHelper
    {
        /// <summary>
        /// 使用委托，执行静态方法
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object ExecuteMethod(Type type, string methodName, params object[] args)
        {
            var paraTypes = GetArgTypes(args);
            var methodInfo = ReflectionCache.GetDelegate(type, methodName, paraTypes);
            if (methodInfo == null)
            {
                throw new ArgumentException(methodName + "方法未找到.");
            }

            return methodInfo.Invoke(null, args);
        }

        /// <summary>
        /// 使用委托，执行实例方法
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object ExecuteMethod(object obj, string methodName, params object[] args)
        {
            var paraTypes = GetArgTypes(args);
            var methodInfo = ReflectionCache.GetDelegate(obj.GetType(), methodName, paraTypes);
            if (methodInfo == null)
            {
                throw new ArgumentException(methodName + "方法未找到.");
            }

            return methodInfo.Invoke(obj, args);
        }

        /// <summary>
        /// 使用原生MethodInfo反射，执行静态方法
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object ExecuteOriginMethodInfo(Type type, string methodName, params object[] args)
        {
            var paraTypes = GetArgTypes(args);
            var methodInfo = ReflectionCache.GetMethodInfo(type, methodName, paraTypes);

            args = CheckArgs(args, methodInfo);

            return methodInfo.Invoke(null, args);
        }

        /// <summary>
        /// 使用原生MethodInfo反射，执行实例方法
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object ExecuteOriginMethodInfo(object obj, string methodName, params object[] args)
        {
            var paraTypes = GetArgTypes(args);
            var methodInfo = ReflectionCache.GetMethodInfo(obj.GetType(), methodName, paraTypes);

            args = CheckArgs(args, methodInfo);
            return methodInfo.Invoke(obj, args);
        }


        internal static MethodInfo GetMethod(Type type, string methodName, params Type[] paraTypes)
        {
            var method = FindMethod(type, methodName, paraTypes);
            if (method == null)
                throw new KeyNotFoundException("根据指定的方法和参数列表，未能找到方法：" + methodName);
            return method;
        }

        internal static FastMethodInfo GetMethodDelegate(Type type, string methodName,
            params Type[] paraTypes)
        {
            var method = FindMethod(type, methodName, paraTypes);
            if (method == null)
                throw new KeyNotFoundException("根据指定的方法和参数列表，未能找到方法：" + methodName);

            var ret = new FastMethodInfo(method);
            return ret;
        }
        /*
        internal static Delegate GetMethodDelegate2(Type type, string methodName,
            params Type[] paraTypes)
        {
            var method = FindMethod(type, methodName, paraTypes);
            if (method == null)
                throw new KeyNotFoundException("根据指定的方法和参数列表，未能找到方法：" + methodName);

            // 参考 https://stackoverflow.com/questions/16364198/how-to-create-a-delegate-from-a-methodinfo-when-method-signature-cannot-be-known
            // 如果不用linq，对于 方法  string TestClass.aaa(int a, int b) 要用下面的语句创建委托：
            // var fun = (Func<TestClass, int, int, string>)methodTmp.CreateDelegate(typeof(Func<TestClass, int, int, string>));
            var delegateTypes = from parameter in method.GetParameters() select parameter.ParameterType;

            Type[] arrTypes;
            if (method.IsStatic)
            {
                // 静态方法不需要传递方法所在的class类型
                arrTypes = new Type[0];
            }
            else
            {
                // 委托的第一个参数是方法所在的class类型
                arrTypes = new[] {type};
            }
            
            // 委托参数的中间部分，是方法参数类型，最后一个是返回值类型
            arrTypes = arrTypes.Concat(delegateTypes).Concat(new[] {method.ReturnType}).ToArray();
            var ret = method.CreateDelegate(Expression.GetDelegateType(arrTypes));

//            var funcType = typeof(Func<>).MakeGenericType(arrTypes);
//            var aa = Activator.CreateInstance(funcType, method);
            //            Func()
            //
            //            ret.DynamicInvoke();

            return ret;
        }*/

        /// <summary>
        /// 根据参数列表，匹配出合适的方法
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="methodName">方法名</param>
        /// <param name="paraTypes">参数类型列表</param>
        /// <param name="isGeneric">是否泛型方法</param>
        /// <returns></returns>
        private static MethodInfo FindMethod(Type type, string methodName, Type[] paraTypes = null,
            bool isGeneric = false)
        {
            BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Instance |
                                 BindingFlags.Public | BindingFlags.NonPublic;
            var methods = type.GetMethods(flags);

            if (paraTypes == null)
                paraTypes = new Type[0];

            MethodInfo ret = null;
            foreach (var infoItem in methods)
            {
                if (infoItem.IsGenericMethod != isGeneric)
                    continue;

                // EndsWith用于查找类似于IDisposable.Dispose这种显式实现的方法
                if (infoItem.Name != methodName && !infoItem.Name.EndsWith("." + methodName))
                    continue;

                var methodParas = infoItem.GetParameters();
                var paraLen = methodParas.Length;
                if (paraLen != paraTypes.Length)
                    continue;

                if (IsTypesMatch(methodParas, paraTypes))
                {
                    ret = infoItem;
                    break;
                }
            }

            return ret;
        }

        /// <summary>
        /// 对比方法的参数类型和传入的参数列表类型是否匹配
        /// </summary>
        /// <param name="methodParas"></param>
        /// <param name="argParaTypes"></param>
        /// <returns></returns>
        private static bool IsTypesMatch(ParameterInfo[] methodParas, Type[] argParaTypes)
        {
            var paraLen = argParaTypes.Length;
            if (methodParas.Length != paraLen)
                return false;

            // 先判断 参数是否精确匹配
            var isParaExactMatch = true;
            for (var i = 0; i < paraLen; i++)
            {
                var ptype = methodParas[i].ParameterType;
                if (ptype != argParaTypes[i])
                {
                    isParaExactMatch = false;
                    break;
                }
            }

            if (isParaExactMatch)
                return true;

            // 参数不精确匹配时，进行子类匹配
            var isParaMatch = true;
            for (var i = 0; i < paraLen; i++)
            {
                var ptype = methodParas[i].ParameterType;
                if (ptype != argParaTypes[i] && !argParaTypes[i].IsSubclassOf(ptype) &&
                    !ptype.IsAssignableFrom(argParaTypes[i]))
                {
                    isParaMatch = false;
                    break;
                }
            }

            return isParaMatch;
        }

        private static Type[] GetArgTypes(params object[] args)
        {
            Type[] paraTypes;
            if (args == null)
            {
                paraTypes = new Type[0];
            }
            else
            {
                // 定义参数类型，避免重载方法调用导致：发现不明确的匹配
                paraTypes = new Type[args.Length];
                int i = 0;
                foreach (object arg in args)
                {
                    paraTypes[i] = arg.GetType();
                    i++;
                }
            }

            return paraTypes;
        }

        private static object[] CheckArgs(object[] args, MethodInfo method)
        {
            // 如果没有传递参数,要根据方法的参数个数，传递null
            if (args == null)
            {
                ParameterInfo[] arrPara = method.GetParameters();
                if (arrPara.Length > 0)
                {
                    args = new object[arrPara.Length];
                }
            }

            return args;
        }
    }
}
