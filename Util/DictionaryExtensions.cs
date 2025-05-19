namespace SigmusV2.GodotServiceFramework.Util;

public static class DictionaryExtensions
{
    public static void Increment<TKey>(this Dictionary<TKey, int> dict, TKey key, int amount = 1) where TKey : notnull
    {
        if (!dict.TryAdd(key, amount))
            dict[key] += amount;
    }

    public static void Decrement<TKey>(this Dictionary<TKey, int> dict, TKey key, int amount = 1) where TKey : notnull
    {
        if (dict.ContainsKey(key))
            dict[key] -= amount;
        else
            dict[key] = -amount;
    }
}