using System.Runtime.CompilerServices;

namespace GodotServiceFramework.Extensions;

/// <summary>
/// 针对所有对象的拓展
/// </summary>
public static class ObjectExtensions
{
    private static readonly Dictionary<int, HashSet<string>> ObjectTags = [];

    /// <summary>
    /// 内存地址转ID, 不可用于持久化
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static int GetObjectId(this object obj) => RuntimeHelpers.GetHashCode(obj);


    public static void SetTag(this object obj, string tag)
    {
        if (!ObjectTags.TryGetValue(obj.GetObjectId(), out var tags))
        {
            tags = [];
            ObjectTags[obj.GetObjectId()] = tags;
        }

        tags.Add(tag);
    }

    public static bool HasTag(this object obj, string tag)
    {
        return ObjectTags.TryGetValue(obj.GetObjectId(), out var tags) && tags.Contains(tag);
    }


    public static bool StartsWithTag(this object obj, string key)
    {
        return ObjectTags.TryGetValue(obj.GetObjectId(), out var tags) && tags.Any(t => t.StartsWith(key));
    }

    public static bool ContainsTag(this object obj, string key)
    {
        return ObjectTags.TryGetValue(obj.GetObjectId(), out var tags) && tags.Any(t => t.Contains(key));
    }

    public static HashSet<string> GetTags(this object obj)
    {
        return ObjectTags.ContainsKey(obj.GetObjectId()) ? ObjectTags[obj.GetObjectId()] : [];
    }
}