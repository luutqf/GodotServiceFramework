namespace GodotServiceFramework.Extensions;

public static class DictionaryExtensions
{
    public static IDictionary<TKey, TValue> AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dict,
        IDictionary<TKey, TValue> other)
    {
        foreach (var kvp in other)
        {
            dict[kvp.Key] = kvp.Value; // 覆盖或添加新值
        }

        return dict;
    }
}