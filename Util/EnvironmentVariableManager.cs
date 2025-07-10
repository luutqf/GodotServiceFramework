using System.Runtime.InteropServices;

namespace GodotServiceFramework.Util;

/// <summary>
/// 环境变量管理器，支持按优先级读取环境变量
/// </summary>
public static class EnvironmentVariableManager
{
    /// <summary>
    /// 环境变量读取优先级
    /// </summary>
    public enum EnvironmentVariableTarget
    {
        /// <summary>
        /// 进程环境变量（最高优先级）
        /// </summary>
        Process,

        /// <summary>
        /// 用户环境变量（中等优先级）
        /// </summary>
        User,

        /// <summary>
        /// 系统环境变量（最低优先级）
        /// </summary>
        Machine
    }

    /// <summary>
    /// 按优先级获取环境变量
    /// 优先级：进程 > 用户 > 系统
    /// </summary>
    /// <param name="variable">环境变量名</param>
    /// <param name="target">指定目标（可选，默认按优先级查找）</param>
    /// <returns>环境变量值，如果不存在则返回null</returns>
    public static string? GetEnvironmentVariable(string variable, System.EnvironmentVariableTarget? target = null)
    {
        if (target.HasValue)
        {
            return GetEnvironmentVariableFromTarget(variable, target.Value);
        }

        // 按优先级查找：进程 > 用户 > 系统
        var value = GetEnvironmentVariableFromTarget(variable, System.EnvironmentVariableTarget.Process);
        if (!string.IsNullOrEmpty(value))
            return value;

        value = GetEnvironmentVariableFromTarget(variable, System.EnvironmentVariableTarget.User);
        if (!string.IsNullOrEmpty(value))
            return value;

        return GetEnvironmentVariableFromTarget(variable, System.EnvironmentVariableTarget.Machine);
    }

    /// <summary>
    /// 从指定目标获取环境变量
    /// </summary>
    /// <param name="variable">环境变量名</param>
    /// <param name="target">目标类型</param>
    /// <returns>环境变量值</returns>
    private static string? GetEnvironmentVariableFromTarget(string variable, System.EnvironmentVariableTarget target)
    {
        try
        {
            switch (target)
            {
                case System.EnvironmentVariableTarget.Process:
                    return Environment.GetEnvironmentVariable(variable);

                case System.EnvironmentVariableTarget.User:
                    return Environment.GetEnvironmentVariable(variable, System.EnvironmentVariableTarget.User);

                case System.EnvironmentVariableTarget.Machine:
                    return Environment.GetEnvironmentVariable(variable, System.EnvironmentVariableTarget.Machine);

                default:
                    return null;
            }
        }
        catch (Exception ex)
        {
            // 记录错误但不抛出异常，继续尝试其他目标
            Log.Warn($"获取环境变量 {variable} 失败 (目标: {target}): {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 设置环境变量
    /// </summary>
    /// <param name="variable">环境变量名</param>
    /// <param name="value">环境变量值</param>
    /// <param name="target">目标类型，默认为进程</param>
    public static void SetEnvironmentVariable(string variable, string value,
        System.EnvironmentVariableTarget target = System.EnvironmentVariableTarget.Process)
    {
        try
        {
            Environment.SetEnvironmentVariable(variable, value, target);
        }
        catch (Exception ex)
        {
            Log.Error($"设置环境变量 {variable} 失败 (目标: {target}): {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 检查环境变量是否存在
    /// </summary>
    /// <param name="variable">环境变量名</param>
    /// <param name="target">指定目标（可选，默认按优先级查找）</param>
    /// <returns>是否存在</returns>
    public static bool HasEnvironmentVariable(string variable, System.EnvironmentVariableTarget? target = null)
    {
        var value = GetEnvironmentVariable(variable, target);
        return !string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// 获取所有环境变量
    /// </summary>
    /// <param name="target">目标类型，默认为进程</param>
    /// <returns>环境变量字典</returns>
    public static Dictionary<string, string> GetAllEnvironmentVariables(
        System.EnvironmentVariableTarget target = System.EnvironmentVariableTarget.Process)
    {
        try
        {
            var variables = Environment.GetEnvironmentVariables(target);
            return variables.Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!);
        }
        catch (Exception ex)
        {
            Log.Warn($"获取环境变量失败 (目标: {target}): {ex.Message}");
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// 获取环境变量并转换为指定类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="variable">环境变量名</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="target">指定目标（可选）</param>
    /// <returns>转换后的值</returns>
    public static T? GetEnvironmentVariable<T>(string variable, T? defaultValue = default,
        System.EnvironmentVariableTarget? target = null)
    {
        var value = GetEnvironmentVariable(variable, target);
        if (string.IsNullOrEmpty(value))
            throw new KeyNotFoundException(variable);

        try
        {
            if (typeof(T) == typeof(string))
                return (T)(object)value;

            if (typeof(T) == typeof(int) && int.TryParse(value, out var intValue))
                return (T)(object)intValue;

            if (typeof(T) == typeof(long) && long.TryParse(value, out var longValue))
                return (T)(object)longValue;

            if (typeof(T) == typeof(double) && double.TryParse(value, out var doubleValue))
                return (T)(object)doubleValue;

            if (typeof(T) == typeof(bool) && bool.TryParse(value, out var boolValue))
                return (T)(object)boolValue;

            if (typeof(T) == typeof(DateTime) && DateTime.TryParse(value, out var dateTimeValue))
                return (T)(object)dateTimeValue;

            // 尝试使用Convert.ChangeType进行转换
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            Log.Warn($"环境变量 {variable} 类型转换失败: {ex.Message}");
            return defaultValue;
        }
    }
}