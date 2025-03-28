using System.Globalization;

namespace GodotServiceFramework.Util;

public static class NumberUtils
{
    /// <summary>
    /// 推断字符串的类型并转换
    /// </summary>
    /// <param name="value">要推断类型的字符串</param>
    /// <returns>转换后的值</returns>
    private static object InferType(string value)
    {
        // 检查是否为空或空白字符串
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        // 尝试转换为整数
        if (int.TryParse(value, out int intResult))
        {
            return intResult;
        }

        // 尝试转换为长整数
        if (long.TryParse(value, out long longResult))
        {
            return longResult;
        }

        // 尝试转换为双精度浮点数
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleResult))
        {
            return doubleResult;
        }

        // 尝试转换为布尔值
        if (bool.TryParse(value, out bool boolResult))
        {
            return boolResult;
        }

        // 尝试转换为日期时间
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateResult))
        {
            return dateResult;
        }

        // 尝试判断是否为GUID
        if (Guid.TryParse(value, out Guid guidResult))
        {
            return guidResult;
        }

        // 默认保持为字符串
        return value;
    }

    /// <summary>
    /// 将Dictionary<string, object>中的字符串值自动转换为推断的类型
    /// </summary>
    /// <param name="dict">输入的字典</param>
    /// <returns>转换后的字典</returns>
    public static Dictionary<string, object> AutoInferTypes(IDictionary<string, object> dict)
    {
        var result = new Dictionary<string, object>();

        foreach (var kvp in dict)
        {
            if (kvp.Value is string strValue)
            {
                result[kvp.Key] = InferType(strValue);
            }
            else
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// 合并两个字典，但不覆盖目标字典中已存在的键值
    /// </summary>
    /// <typeparam name="TKey">键的类型</typeparam>
    /// <typeparam name="TValue">值的类型</typeparam>
    /// <param name="target">目标字典</param>
    /// <param name="source">要合并的源字典</param>
    /// <returns>合并后的字典</returns>
    public static Dictionary<TKey, TValue> MergeWithoutOverride<TKey, TValue>(
        this Dictionary<TKey, TValue> target,
        Dictionary<TKey, TValue>? source) where TKey : notnull
    {
        if (source == null)
            return target;

        // 创建一个新的字典来存储结果
        var result = new Dictionary<TKey, TValue>(target);

        // 只添加目标字典中不存在的键
        foreach (var item in source)
        {
            if (!result.ContainsKey(item.Key))
            {
                result.Add(item.Key, item.Value);
            }
            // 已存在的键不覆盖
        }

        return result;
    }

    /// <summary>
    /// 原地合并两个字典，但不覆盖目标字典中已存在的键值
    /// </summary>
    /// <typeparam name="TKey">键的类型</typeparam>
    /// <typeparam name="TValue">值的类型</typeparam>
    /// <param name="target">目标字典</param>
    /// <param name="source">要合并的源字典</param>
    public static void MergeWithoutOverrideInPlace<TKey, TValue>(
        this Dictionary<TKey, TValue> target,
        Dictionary<TKey, TValue>? source) where TKey : notnull
    {
        if (source == null)
            return;

        // 只添加目标字典中不存在的键
        foreach (var item in source)
        {
            if (!target.ContainsKey(item.Key))
            {
                target.Add(item.Key, item.Value);
            }
            // 已存在的键不覆盖
        }
    }
}