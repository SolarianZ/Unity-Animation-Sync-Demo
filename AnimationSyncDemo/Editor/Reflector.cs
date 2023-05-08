using System;
using System.Reflection;
#pragma warning disable CS8632

namespace GBG.AnimationSyncDemo.Editor
{
    public static class Reflector
    {
        public const BindingFlags DefaultBindingFlags = BindingFlags.Instance | BindingFlags.Static |
                                                        BindingFlags.Public | BindingFlags.NonPublic;


        #region Type/Instance

        public static Type GetType(Assembly assembly, string typeFullName)
        {
            var type = assembly.GetType(typeFullName);
            return type;
        }

        public static object CreateInstance(Type type, object[]? parameters = null)
        {
            var instance = Activator.CreateInstance(type, parameters);
            return instance;
        }

        public static object CreateInstance(Assembly assembly, string typeFullName, object[]? parameters = null)
        {
            var type = GetType(assembly, typeFullName);
            var instance = CreateInstance(type, parameters);
            return instance;
        }

        #endregion

        #region Get/Set/Invoke

        public static object GetField(Type type, object obj, string fieldName)
        {
            while (type != null)
            {
                var field = type.GetField(fieldName, DefaultBindingFlags);
                if (field != null)
                {
                    return field.GetValue(obj);
                }

                type = type.BaseType;
            }

            throw new ArgumentException($"Field '{fieldName}' not found.");
        }

        public static object GetField(this object obj, string fieldName)
        {
            return GetField(obj.GetType(), obj, fieldName);
        }

        public static void SetField(Type type, object obj, string fieldName, object value)
        {
            while (type != null)
            {
                var field = type.GetField(fieldName, DefaultBindingFlags);
                if (field != null)
                {
                    field.SetValue(obj, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new ArgumentException($"Field '{fieldName}' not found.");
        }

        public static void SetField(this object obj, string fieldName, object value)
        {
            SetField(obj.GetType(), obj, fieldName, value);
        }


        public static object GetProperty(Type type, object obj, string propertyName)
        {
            while (type != null)
            {
                var property = type.GetProperty(propertyName, DefaultBindingFlags);
                if (property != null)
                {
                    return property.GetValue(obj);
                }

                type = type.BaseType;
            }

            throw new ArgumentException($"Property '{propertyName}' not found.");
        }

        public static object GetProperty(this object obj, string propertyName)
        {
            return GetProperty(obj.GetType(), obj, propertyName);
        }

        public static void SetProperty(Type type, object obj, string propertyName, object value)
        {
            while (type != null)
            {
                var property = type.GetProperty(propertyName, DefaultBindingFlags);
                if (property != null)
                {
                    property.SetValue(obj, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new ArgumentException($"Property '{propertyName}' not found.");
        }

        public static void SetProperty(this object obj, string propertyName, object value)
        {
            SetProperty(obj.GetType(), obj, propertyName, value);
        }


        public static object GetIndexer(Type type, object obj, object[] indices,
            Type[]? paramTypes = null, Type? returnType = null)
        {
            while (type != null)
            {
                var property = paramTypes == null
                    ? type.GetProperty("Item", DefaultBindingFlags)
                    : type.GetProperty("Item", DefaultBindingFlags, null, returnType, paramTypes, null);
                if (property != null)
                {
                    return property.GetValue(obj, indices);
                }

                type = type.BaseType;
            }

            throw new ArgumentException("Indexer not found.");
        }

        public static object GetIndexer(this object obj, object[] indices,
            Type[]? paramTypes = null, Type? returnType = null)
        {
            return GetIndexer(obj.GetType(), indices, paramTypes, returnType);
        }

        public static void SetIndexer(Type type, object obj, object value, object[] indices,
            Type[]? paramTypes = null, Type? returnType = null)
        {
            while (type != null)
            {
                var property = paramTypes == null
                    ? type.GetProperty("Item", DefaultBindingFlags)
                    : type.GetProperty("Item", DefaultBindingFlags, null, returnType, paramTypes, null);
                if (property != null)
                {
                    property.SetValue(obj, value, indices);
                    return;
                }

                type = type.BaseType;
            }

            throw new ArgumentException("Indexer not found.");
        }

        public static void SetIndexer(this object obj, object value, object[] indices,
            Type[]? paramTypes = null, Type? returnType = null)
        {
            SetIndexer(obj.GetType(), obj, value, indices, paramTypes, returnType);
        }


        public static object Get(Type type, object obj, string memberName, object[]? indices = null,
            Type[]? paramTypes = null, Type? returnType = null)
        {
            if (string.IsNullOrEmpty(memberName))
            {
                return GetIndexer(type, obj, indices, paramTypes, returnType);
            }

            while (type != null)
            {
                var property = type.GetProperty(memberName, DefaultBindingFlags);
                if (property != null)
                {
                    return property.GetValue(obj);
                }

                var field = type.GetField(memberName, DefaultBindingFlags);
                if (field != null)
                {
                    return field.GetValue(obj);
                }

                type = type.BaseType;
            }

            throw new ArgumentException($"Member '{memberName}' not found.");
        }

        public static object Get(this object obj, string memberName, object[]? indices = null,
            Type[]? paramTypes = null, Type? returnType = null)
        {
            return Get(obj.GetType(), obj, memberName, indices, paramTypes, returnType);
        }

        public static void Set(Type type, object obj, string memberName, object value,
            object[]? indices = null, Type[]? paramTypes = null, Type? returnType = null)
        {
            if (string.IsNullOrEmpty(memberName))
            {
                SetIndexer(type, obj, indices, paramTypes, returnType);
                return;
            }

            while (type != null)
            {
                var property = type.GetProperty(memberName, DefaultBindingFlags);
                if (property != null)
                {
                    property.SetValue(obj, value);
                    return;
                }

                var field = type.GetField(memberName, DefaultBindingFlags);
                if (field != null)
                {
                    field.SetValue(obj, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new ArgumentException($"Member '{memberName}' not found.");
        }

        public static void Set(this object obj, string memberName, object value,
            object[]? indices = null, Type[]? paramTypes = null, Type? returnType = null)
        {
            Set(obj.GetType(), obj, memberName, value, indices, paramTypes, returnType);
        }


        public static object? Invoke(Type type, object obj, string methodName, object[] parameters,
            Type[]? paramTypes = null, Type[]? genericTypeArgs = null)
        {
            while (type != null)
            {
                var method = paramTypes == null
                    ? type.GetMethod(methodName, DefaultBindingFlags)
                    : type.GetMethod(methodName, DefaultBindingFlags, null, paramTypes, null);
                if (method != null)
                {
                    if (genericTypeArgs == null)
                    {
                        return method.Invoke(obj, parameters);
                    }

                    var genericMethod = method.MakeGenericMethod(genericTypeArgs);
                    return genericMethod.Invoke(obj, parameters);
                }

                type = type.BaseType;
            }

            throw new ArgumentException($"Method '{methodName}' not found.");
        }

        public static object? Invoke(this object obj, string methodName, object[] parameters,
            Type[]? paramTypes = null, Type[]? genericTypeArgs = null)
        {
            return Invoke(obj.GetType(), obj, methodName, parameters, paramTypes, genericTypeArgs);
        }

        #endregion

        #region Add/Remove Event Handler

        public static void AddEventHandler(Type type, object obj, string eventName, Delegate handler)
        {
            while (type != null)
            {
                var eventInfo = type.GetEvent(eventName, DefaultBindingFlags);
                if (eventInfo != null)
                {
                    eventInfo.AddEventHandler(obj, handler);
                    return;
                }

                type = type.BaseType;
            }

            throw new ArgumentException($"Event '{eventName}' not found.");
        }

        public static void AddEventHandler(this object obj, string eventName, Delegate handler)
        {
            AddEventHandler(obj.GetType(), obj, eventName, handler);
        }

        public static void RemoveEventHandler(Type type, object obj, string eventName, Delegate handler)
        {
            while (type != null)
            {
                var eventInfo = type.GetEvent(eventName, DefaultBindingFlags);
                if (eventInfo != null)
                {
                    eventInfo.RemoveEventHandler(obj, handler);
                    return;
                }

                type = type.BaseType;
            }

            throw new ArgumentException($"Event '{eventName}' not found.");
        }

        public static void RemoveEventHandler(this object obj, string eventName, Delegate handler)
        {
            RemoveEventHandler(obj.GetType(), obj, eventName, handler);
        }

        #endregion

        #region Create Delegate

        public static T CreateDelegate<T>(Type type, object obj, string methodName,
            Type[]? paramTypes = null, Type[]? genericTypeArgs = null) where T : Delegate
        {
            while (type != null)
            {
                var method = paramTypes == null
                    ? type.GetMethod(methodName, DefaultBindingFlags)
                    : type.GetMethod(methodName, DefaultBindingFlags, null, paramTypes, null);
                if (method != null)
                {
                    if (genericTypeArgs == null)
                    {
                        var handler = (T)Delegate.CreateDelegate(typeof(T), obj, method);
                        return handler;
                    }

                    var genericMethod = method.MakeGenericMethod(genericTypeArgs);
                    var geneticHandler = (T)Delegate.CreateDelegate(typeof(T), obj, genericMethod);
                    return geneticHandler;
                }

                type = type.BaseType;
            }

            throw new ArgumentException($"Method '{methodName}' not found.");
        }

        public static T CreateDelegate<T>(this object obj, string methodName,
            Type[]? paramTypes = null, Type[]? genericTypeArgs = null) where T : Delegate
        {
            return CreateDelegate<T>(obj.GetType(), obj, methodName, paramTypes, genericTypeArgs);
        }

        public static Delegate CreateDelegate(Type type, object obj, string methodName,
            Type delegateType, Type[]? paramTypes = null, Type[]? genericTypeArgs = null)
        {
            while (type != null)
            {
                var method = paramTypes == null
                    ? type.GetMethod(methodName, DefaultBindingFlags)
                    : type.GetMethod(methodName, DefaultBindingFlags, null, paramTypes, null);
                if (method != null)
                {
                    if (genericTypeArgs == null)
                    {
                        var handler = Delegate.CreateDelegate(delegateType, obj, method);
                        return handler;
                    }

                    var genericMethod = method.MakeGenericMethod(genericTypeArgs);
                    var geneticHandler = Delegate.CreateDelegate(delegateType, obj, genericMethod);
                    return geneticHandler;
                }

                type = type.BaseType;
            }

            throw new ArgumentException($"Method '{methodName}' not found.");
        }

        public static Delegate CreateDelegate(this object obj, string methodName, Type delegateType,
            Type[]? paramTypes = null, Type[]? genericTypeArgs = null)
        {
            return CreateDelegate(obj.GetType(), obj, methodName, delegateType, paramTypes, genericTypeArgs);
        }

        #endregion
    }
}