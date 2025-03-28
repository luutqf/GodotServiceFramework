using System;
using System.Collections.Generic;
using GodotServiceFramework.Binding;
using GodotServiceFramework.Data;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.Binding;

/// <summary>
/// 这个是临时的数据仓库
/// </summary>
public static class DataStore
{
    private static Dictionary<Type, Dictionary<int, IBinding>> Store { get; set; } = new();

    private static Dictionary<Type, Dictionary<int, Dictionary<object, object>>> Properties { get; set; } = [];

    public static T? Get<T>(int id) where T : IBinding
    {
        if (!Store.TryGetValue(typeof(T), out var store))
        {
            store = [];
            Store[typeof(T)] = store;
        }

        if (store.TryGetValue(id, out var value))
        {
            return (T)value;
        }

        return default;
    }

    public static object? Get(int id, Type type)
    {
        if (Store.TryGetValue(type, out var value)) return value.GetValueOrDefault(id);

        value = [];
        Store[type] = value;

        return value.GetValueOrDefault(id);
    }

    public static TR? GetProperty<TR>(this IBinding @this, object key)
    {
        if (!Properties.TryGetValue(@this.GetType(), out var dict))
        {
            dict = [];
            Properties[@this.GetType()] = dict;
        }

        if (!dict.TryGetValue(@this.Id, out var properties)) return default;
        if (!properties.TryGetValue(key, out var value)) return default;

        if (value is TR r)
        {
            return r;
        }

        return default;
    }

    public static bool TryGetProperty<TR>(this IBinding @this, object key, out TR? result)
    {
        if (!Properties.TryGetValue(@this.GetType(), out var dict))
        {
            dict = [];
            Properties[@this.GetType()] = dict;
        }


        if (dict.TryGetValue(@this.Id, out var properties))
        {
            if (properties.TryGetValue(key, out var value))
            {
                if (value is TR r)
                {
                    result = r;
                    return true;
                }
            }
        }

        result = default;
        return false;
    }

    public static void RemoveProperty(this IBinding @this, object key)
    {
        if (!Properties.TryGetValue(@this.GetType(), out var properties)) return;

        if (properties.TryGetValue(@this.Id, out var dictionary))
        {
            dictionary.Remove(key);
        }
    }

    public static void ClearProperties(this IBinding @this)
    {
        if (!Properties.TryGetValue(@this.GetType(), out var properties)) return;
        if (properties.TryGetValue(@this.Id, out var value))
        {
            value.Clear();
        }
    }

    public static void Set<T>(T value) where T : IBinding
    {
        var type = value.GetType();
        if (!Store.TryGetValue(type, out var store))
        {
            store = [];
            Store[type] = store;
        }


        store[value.Id] = value;
    }


    public static void SetProperty<T>(this T @this, string key, object v) where T : IBinding
    {
        var type = @this.GetType();
        if (!Properties.TryGetValue(type, out var properties))
        {
            properties = [];
            Properties[type] = properties;
        }

        if (!properties.TryGetValue(@this.Id, out var value))
        {
            value = [];
            properties[@this.Id] = value;
        }

        value[key] = v;

        //TODO 这种地方考虑异步吧
        // IDataBinding.Binding(@this, key, v);
        IDataNode.Binding(@this, DataModifyType.Property, key, v);
    }


    public static void Remove<T>(T value) where T : IBinding
    {
        var type = value.GetType();
        if (Store.TryGetValue(type, out var v1))
        {
            if (v1.TryGetValue(value.Id, out var binding))
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (binding is ICloseable closeable)
                {
                    closeable.Close();
                }

                v1.Remove(value.Id);
            }
        }

        if (Properties.TryGetValue(type, out var v2))
        {
            v2.Remove(value.Id);
        }
    }

    public static void ClearStore(this IBinding @this)
    {
        if (Store.TryGetValue(@this.GetType(), out var s))
        {
            s.Remove(@this.Id);
        }

        if (Properties.TryGetValue(@this.GetType(), out var s2))
        {
            s2.Remove(@this.Id);
        }
    }
}