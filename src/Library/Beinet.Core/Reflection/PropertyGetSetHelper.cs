using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Beinet.Core.Reflection
{
    /// <summary>
    /// Property和Field读写相关的辅助类
    /// </summary>
    public static class PropertyGetSetHelper
    {
        /// <summary>
        /// 设置指定类型的静态属性或字段的值
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propOrFieldName"></param>
        /// <param name="value"></param>
        public static void Set(Type type, string propOrFieldName, object value)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (string.IsNullOrEmpty(propOrFieldName))
                throw new ArgumentNullException(nameof(propOrFieldName));

            var setter = ReflectionCache.GetSetter(type, propOrFieldName);
            setter.Set(null, value);
        }

        /// <summary>
        /// 设置指定实例的属性或字段的值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propOrFieldName"></param>
        /// <param name="value"></param>
        public static void Set(object obj, string propOrFieldName, object value)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (string.IsNullOrEmpty(propOrFieldName))
                throw new ArgumentNullException(nameof(propOrFieldName));

            var setter = ReflectionCache.GetSetter(obj.GetType(), propOrFieldName);
            setter.Set(obj, value);
        }

        /// <summary>
        /// 读取指定类型的静态属性或字段的值
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propOrFieldName"></param>
        public static object Get(Type type, string propOrFieldName)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (string.IsNullOrEmpty(propOrFieldName))
                throw new ArgumentNullException(nameof(propOrFieldName));

            var getter = ReflectionCache.GetGetter(type, propOrFieldName);
            return getter.Get(null);
        }

        /// <summary>
        /// 读取指定实例的属性或字段的值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propOrFieldName"></param>
        public static object Get(object obj, string propOrFieldName)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (string.IsNullOrEmpty(propOrFieldName))
                throw new ArgumentNullException(nameof(propOrFieldName));

            var getter = ReflectionCache.GetGetter(obj.GetType(), propOrFieldName);
            return getter.Get(obj);
        }


        #region internal的委托创建方法



        /// <summary>
        /// 创建指定属性的Set委托
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        internal static ISetter CreateSetter(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException(nameof(propertyInfo));

            if (propertyInfo.CanWrite == false)
                throw new NotSupportedException(propertyInfo.Name + " 属性不支持写操作.");

            MethodInfo methodInfo = propertyInfo.GetSetMethod(true);
            if (methodInfo.GetParameters().Length > 1)
                throw new NotSupportedException("不支持索引器属性: " + propertyInfo.Name);



            if (methodInfo.IsStatic)
            {
                Type staticType = typeof(StaticSetterWrapper<>).MakeGenericType(propertyInfo.PropertyType);
                return (ISetter) Activator.CreateInstance(staticType, methodInfo);
            }

            Type instanceType =
                typeof(SetterWrapper<,>).MakeGenericType(propertyInfo.ReflectedType, propertyInfo.PropertyType);
            return (ISetter) Activator.CreateInstance(instanceType, methodInfo);
        }
        /*
         emit 设置PropertyInfo
        public delegate void SetValueDelegate(object target, object arg);
        public static SetValueDelegate CreatePropertySetter(PropertyInfo property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (!property.CanWrite)
                return null;
            var type = property.ReflectedType;
            var ptype = property.PropertyType;
            MethodInfo setMethod = property.GetSetMethod(true);

            DynamicMethod dm = new DynamicMethod("PropertySetter", null,
                new Type[] {typeof(object), typeof(object)}, type, true);

            ILGenerator il = dm.GetILGenerator();

            if (!setMethod.IsStatic)
                il.Emit(OpCodes.Ldarg_0);

            il.Emit(OpCodes.Ldarg_1);

            if (ptype.IsValueType)
                il.Emit(OpCodes.Unbox_Any, ptype);
            else
                il.Emit(OpCodes.Castclass, ptype);
            if (!setMethod.IsStatic && !type.IsValueType)
                il.EmitCall(OpCodes.Callvirt, setMethod, null);
            else
                il.EmitCall(OpCodes.Call, setMethod, null);

            il.Emit(OpCodes.Ret);

            return (SetValueDelegate) dm.CreateDelegate(typeof(SetValueDelegate));
        }

         */

        /// <summary>
        /// 创建指定字段的Set委托.
        /// </summary>
        /// <param name="fieldInfo"></param>
        /// <returns></returns>
        internal static ISetter CreateSetter(FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
                throw new ArgumentNullException(nameof(fieldInfo));

            var type = fieldInfo.ReflectedType;
            if (type == null)
                throw new ArgumentException("字段反射类型为空", nameof(fieldInfo));

            // 因为Field没有Set和Get方法，因此只能用il code
            var methodName = type.FullName + ".set_" + fieldInfo.Name;
            DynamicMethod setterMethod =
                new DynamicMethod(methodName, null, new Type[] {type, fieldInfo.FieldType}, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            if (fieldInfo.IsStatic)
            {
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Stsfld, fieldInfo);
            }
            else
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Stfld, fieldInfo);
            }

            gen.Emit(OpCodes.Ret);

            var instanceType = typeof(SetterWrapper<,>).MakeGenericType(type, fieldInfo.FieldType);
            return (ISetter) Activator.CreateInstance(instanceType, setterMethod);
        }


        /// <summary>
        /// 创建指定属性的Get委托
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        internal static IGetter CreateGetter(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException(nameof(propertyInfo));

            if (propertyInfo.CanRead == false)
                throw new NotSupportedException(propertyInfo.Name + " 属性不支持读操作.");

            MethodInfo methodInfo = propertyInfo.GetGetMethod(true);
            if (methodInfo.GetParameters().Length > 1)
                throw new NotSupportedException("不支持索引器属性: " + propertyInfo.Name);



            if (methodInfo.IsStatic)
            {
                Type staticType = typeof(StaticGetterWrapper<>).MakeGenericType(propertyInfo.PropertyType);
                return (IGetter)Activator.CreateInstance(staticType, methodInfo);
            }

            Type instanceType =
                typeof(GetterWrapper<,>).MakeGenericType(propertyInfo.ReflectedType, propertyInfo.PropertyType);
            return (IGetter)Activator.CreateInstance(instanceType, methodInfo);
        }

        /// <summary>
        /// 创建指定字段的Get委托.
        /// </summary>
        /// <param name="fieldInfo"></param>
        /// <returns></returns>
        internal static IGetter CreateGetter(FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
                throw new ArgumentNullException(nameof(fieldInfo));

            var type = fieldInfo.ReflectedType;
            if (type == null)
                throw new ArgumentException("字段反射类型为空", nameof(fieldInfo));

            // 因为Field没有Set和Get方法，因此只能用il code
            var methodName = type.FullName + ".get_" + fieldInfo.Name;
            DynamicMethod getterMethod =
                new DynamicMethod(methodName, fieldInfo.FieldType, new Type[] {type}, true);
            ILGenerator gen = getterMethod.GetILGenerator();
            if (fieldInfo.IsStatic)
            {
                gen.Emit(OpCodes.Ldsfld, fieldInfo);
            }
            else
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, fieldInfo);
            }

            gen.Emit(OpCodes.Ret);

            var instanceType = typeof(GetterWrapper<,>).MakeGenericType(type, fieldInfo.FieldType);

            return (IGetter)Activator.CreateInstance(instanceType, getterMethod);
        }


        #endregion


        #region Set委托类


        /// <summary>
        /// 实例属性或字段的设置器委托
        /// </summary>
        /// <typeparam name="TTarget"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        class SetterWrapper<TTarget, TValue> : ISetter
        {
            private readonly Action<TTarget, TValue> _setter;

            public SetterWrapper(MethodInfo methodInfo)
            {
                var type = typeof(Action<TTarget, TValue>);

                _setter = (Action<TTarget, TValue>) methodInfo.CreateDelegate(type);
                // Delegate.CreateDelegate(type, null, methodInfo); // 这个不支持 DynamicMethod
            }

            /// <summary>
            /// 设置值
            /// </summary>
            /// <param name="target"></param>
            /// <param name="val"></param>
            public void Set(object target, object val)
            {
                _setter((TTarget) target, (TValue) val);
            }
        }

        /// <summary>
        /// 静态属性的设置器委托类
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        class StaticSetterWrapper<TValue> : ISetter
        {
            private readonly Action<TValue> _setter;

            public StaticSetterWrapper(MethodInfo methodInfo)
            {
                _setter = (Action<TValue>) methodInfo.CreateDelegate(typeof(Action<TValue>));
                //_setter = (Action<TValue>) Delegate.CreateDelegate(typeof(Action<TValue>), null, methodInfo);
            }

            /// <summary>
            /// 设置值
            /// </summary>
            /// <param name="target"></param>
            /// <param name="val"></param>
            public void Set(object target, object val)
            {
                _setter((TValue) val);
            }
        }


        #endregion

        #region Get委托类


        /// <summary>
        /// 实例属性或字段的设置器委托
        /// </summary>
        /// <typeparam name="TTarget"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        class GetterWrapper<TTarget, TValue> : IGetter
        {
            private readonly Func<TTarget, TValue> _getter;

            public GetterWrapper(MethodInfo methodInfo)
            {
                var type = typeof(Func<TTarget, TValue>);
                _getter = (Func<TTarget, TValue>) methodInfo.CreateDelegate(type);
            }

            /// <summary>
            /// 获取值
            /// </summary>
            /// <param name="target"></param>
            /// <returns></returns>
            public object Get(object target)
            {
                return _getter((TTarget) target);
            }
        }

        /// <summary>
        /// 静态属性的设置器委托类
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        class StaticGetterWrapper<TValue> : IGetter
        {
            private readonly Func<TValue> _getter;

            public StaticGetterWrapper(MethodInfo methodInfo)
            {
                _getter = (Func<TValue>) methodInfo.CreateDelegate(typeof(Func<TValue>));
            }

            /// <summary>
            /// 设置值
            /// </summary>
            /// <param name="target"></param>
            public object Get(object target)
            {
                return _getter();
            }
        }


        #endregion

    }
}
