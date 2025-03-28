using GodotServiceFramework.Util;

namespace GodotServiceFramework.Cache;

using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;

/// <summary>
/// 
/// </summary>
/// <param name="cache"></param>
[Obsolete("没在用的")]
public class InMemoryCacheService(IMemoryCache? cache = null)
{
    private readonly IMemoryCache _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    // 基础的获取和设置方法
    public async Task<T?> GetAsync<T>(string key)
    {
        return await Task.FromResult(_cache.Get<T>(key));
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiry.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiry;
        }

        await Task.FromResult(_cache.Set(key, value, options));
    }

    // Hash 表操作模拟
    public async Task HashSetAsync(string hashKey, string field, string value)
    {
        var dict = await GetOrCreateHashAsync(hashKey);
        dict[field] = value;
        await SetAsync(hashKey, dict);
    }

    public async Task<string?> HashGetAsync(string hashKey, string field)
    {
        var dict = await GetOrCreateHashAsync(hashKey);
        return dict.GetValueOrDefault(field);
    }

    private async Task<ConcurrentDictionary<string, string>> GetOrCreateHashAsync(string hashKey)
    {
        var dict = await GetAsync<ConcurrentDictionary<string, string>>(hashKey);
        if (dict == null)
        {
            dict = new ConcurrentDictionary<string, string>();
            await SetAsync(hashKey, dict);
        }

        return dict;
    }

    // 分布式锁模拟
    public async Task<IDisposable> AcquireLockAsync(string key, TimeSpan timeout)
    {
        var lockObj = _locks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));

        if (await lockObj.WaitAsync(timeout))
        {
            return new AsyncLock(lockObj);
        }

        throw new TimeoutException($"Unable to acquire lock for key {key}");
    }

    private class AsyncLock(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose()
        {
            semaphore.Release();
        }
    }

    // 计数器实现
    public async Task<long> IncrementAsync(string key)
    {
        var value = await GetAsync<long>(key);
        value++;
        await SetAsync(key, value);
        return value;
    }

    // 使用示例
    public static async void Example()
    {
        try
        {
            var cacheService = new InMemoryCacheService();

            // 基础缓存操作
            await cacheService.SetAsync("myKey", "myValue", TimeSpan.FromMinutes(5));
            var value = await cacheService.GetAsync<string>("myKey");
            Console.WriteLine($"Cached value: {value}");

            // Hash表操作
            await cacheService.HashSetAsync("user:1", "name", "John Doe");
            await cacheService.HashSetAsync("user:1", "email", "john@example.com");
            var name = await cacheService.HashGetAsync("user:1", "name");
            Console.WriteLine($"User name: {name}");

            // 使用分布式锁
            try
            {
                using (await cacheService.AcquireLockAsync("myLock", TimeSpan.FromSeconds(5)))
                {
                    // 执行需要同步的操作
                    Console.WriteLine("Lock acquired, performing operation...");
                    await Task.Delay(1000); // 模拟操作
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Failed to acquire lock");
            }

            // 计数器示例
            var count = await cacheService.IncrementAsync("visitors");
            Console.WriteLine($"Visitor count: {count}");
        }
        catch (Exception e)
        {
            Logger.Error(e);
        }
    }
}