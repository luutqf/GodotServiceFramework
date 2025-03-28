using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GodotServiceFramework.Binding;

/// <summary>
/// 缓存项包装类，包含值和属性
/// </summary>
public class CacheItem(object value)
{
    public object Value { get; set; } = value;
    public Dictionary<object, object> Properties { get; } = new();
}

/// <summary>
/// 基于类型的缓存管理器
/// </summary>
public class TypeCacheManager
{
    private static readonly Lazy<TypeCacheManager> _instance = new(() => new TypeCacheManager());

    // 使用ConcurrentDictionary来确保线程安全，key类型改为int
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<int, CacheItem>> _caches;

    private TypeCacheManager()
    {
        _caches = new ConcurrentDictionary<Type, ConcurrentDictionary<int, CacheItem>>();
    }

    public static TypeCacheManager Instance => _instance.Value;

    /// <summary>
    /// 添加或更新缓存项
    /// </summary>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <returns>缓存项的Properties集合</returns>
    public Dictionary<object, object> Set<T>(int key, T value)
    {
        var type = typeof(T);
        var typeCache = _caches.GetOrAdd(type, _ => new ConcurrentDictionary<int, CacheItem>());
        var cacheItem = new CacheItem(value!);
        typeCache.AddOrUpdate(key, cacheItem, (_, _) => cacheItem);
        return cacheItem.Properties;
    }

    /// <summary>
    /// 获取缓存项
    /// </summary>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="properties">输出缓存项的Properties集合，如果项不存在则为null</param>
    /// <returns>缓存值，如果不存在返回默认值</returns>
    public T? Get<T>(int key, out Dictionary<object, object> properties)
    {
        properties = [];
        var type = typeof(T);
        if (_caches.TryGetValue(type, out var typeCache) &&
            typeCache.TryGetValue(key, out var cacheItem))
        {
            properties = cacheItem.Properties;
            return (T)cacheItem.Value;
        }

        return default;
    }

    /// <summary>
    /// 尝试获取缓存项
    /// </summary>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">输出的缓存值</param>
    /// <param name="properties">输出缓存项的Properties集合</param>
    /// <returns>是否成功获取到值</returns>
    public bool TryGet<T>(int key, out T? value, out Dictionary<object, object> properties)
    {
        value = default;
        properties = [];
        var type = typeof(T);

        if (_caches.TryGetValue(type, out var typeCache) &&
            typeCache.TryGetValue(key, out var cacheItem))
        {
            value = (T)cacheItem.Value;
            properties = cacheItem.Properties;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取缓存项的Properties，如果缓存项不存在则返回null
    /// </summary>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <returns>Properties集合，如果缓存项不存在则返回null</returns>
    public Dictionary<object, object> GetProperties<T>(int key)
    {
        var type = typeof(T);
        if (_caches.TryGetValue(type, out var typeCache) &&
            typeCache.TryGetValue(key, out var cacheItem))
        {
            return cacheItem.Properties;
        }

        return [];
    }

    /// <summary>
    /// 移除指定的缓存项
    /// </summary>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <returns>是否成功移除</returns>
    public bool Remove<T>(int key)
    {
        var type = typeof(T);
        if (_caches.TryGetValue(type, out var typeCache))
        {
            return typeCache.TryRemove(key, out _);
        }

        return false;
    }

    /// <summary>
    /// 清除指定类型的所有缓存
    /// </summary>
    /// <typeparam name="T">要清除的缓存类型</typeparam>
    public void ClearType<T>()
    {
        var type = typeof(T);
        _caches.TryRemove(type, out _);
    }

    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public void ClearAll()
    {
        _caches.Clear();
    }
}