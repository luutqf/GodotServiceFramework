using Godot;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Db;
using GodotServiceFramework.Util;

// using AutoGodotService = GodotServiceFramework.Context.Service.AutoGodotService;

namespace GodotServiceFramework.Config;

/// <summary>
/// 改进的全局配置管理器，支持多级配置源和更好的类型安全
/// 配置优先级：配置文件 > 环境变量（进程 > 用户 > 系统）
/// </summary>
[Order(-1000)]
[InjectService]
public partial class ConfigStore
{
    private readonly YamlConfigManager _manager;
    private readonly string _configPath;
    private readonly Dictionary<string, object> _defaultValues = new();
    private readonly Dictionary<string, Type> _configTypes = new();

    private static ConfigStore? _instance;

    public event Action<string, object?> OnConfigUpdated = delegate { };

    public ConfigStore()
    {
        // 1. 从环境变量获取配置文件目录，如果无法获取，则使用默认路径
        var configDir = GetConfigDirectoryFromEnvironment();
        _configPath = Path.Combine(configDir, "sigmus.yaml");

        FileUtils.CreateDirectoryWithCheck(ProjectSettings.GlobalizePath(configDir));
        _manager = new YamlConfigManager(_configPath);
        _manager.Initialize();

        _instance = this;

        Log.Info($"ConfigStore 初始化完成，配置文件路径: {_configPath}");
    }

    /// <summary>
    /// 从环境变量获取配置目录
    /// 优先级：SIGMUS_CONFIG_DIR > SIGMUS_HOME/config > user://config
    /// </summary>
    /// <returns>配置目录路径</returns>
    private string GetConfigDirectoryFromEnvironment()
    {
        // 尝试从环境变量获取配置目录
        var configDir = EnvironmentVariableManager.GetEnvironmentVariable("SIGMUS_CONFIG_DIR");
        if (!string.IsNullOrEmpty(configDir))
        {
            Log.Info($"从环境变量 SIGMUS_CONFIG_DIR 获取配置目录: {configDir}");
            return configDir;
        }

        // 尝试从 SIGMUS_HOME 环境变量获取
        var sigmusHome = EnvironmentVariableManager.GetEnvironmentVariable("SIGMUS_HOME");
        if (!string.IsNullOrEmpty(sigmusHome))
        {
            var homeConfigDir = Path.Combine(sigmusHome, "config");
            Log.Info($"从环境变量 SIGMUS_HOME 获取配置目录: {homeConfigDir}");
            return homeConfigDir;
        }

        // 使用默认路径
        var defaultConfigDir = ProjectSettings.GlobalizePath("user://config");
        Log.Info($"使用默认配置目录: {defaultConfigDir}");
        return defaultConfigDir;
    }

    /// <summary>
    /// 注册配置项，指定默认值和类型
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="description">配置项描述（可选）</param>
    public static void Register<T>(object key, T defaultValue, string? description = null)
    {
        var keyStr = key.ToString()!;
        _instance!._defaultValues[keyStr] = defaultValue!;
        _instance._configTypes[keyStr] = typeof(T);

        Log.Debug($"注册配置项 {keyStr}: 类型={typeof(T).Name}, 默认值={defaultValue}, 描述={description ?? "无"}");
    }

    /// <summary>
    /// 获取配置值，如果不存在则返回注册的默认值
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <returns>配置值或默认值</returns>
    public static T Get<T>(object key)
    {
        var keyStr = key.ToString()!;

        // 1. 优先从配置文件获取
        if (_instance!._manager.ContainsKey(keyStr))
        {
            try
            {
                var configValue = _instance._manager.Get<T>(keyStr);
                Log.Debug($"从配置文件获取配置 {keyStr}: {configValue}");
                return configValue;
            }
            catch (Exception ex)
            {
                Log.Warn($"从配置文件获取配置 {keyStr} 失败: {ex.Message}");
            }
        }

        // 2. 如果配置文件中不存在，尝试从环境变量获取
        var envValue = GetFromEnvironmentVariable<T>(keyStr);
        if (envValue != null)
        {
            Log.Debug($"从环境变量获取配置 {keyStr}: {envValue}");
            return envValue;
        }

        // 3. 返回注册的默认值
        if (_instance._defaultValues.TryGetValue(keyStr, out var defaultValue))
        {
            if (defaultValue is T typedDefault)
            {
                Log.Debug($"使用注册的默认值 {keyStr}: {typedDefault}");
                return typedDefault;
            }
            else
            {
                Log.Warn($"配置 {keyStr} 的默认值类型不匹配，期望 {typeof(T).Name}，实际 {defaultValue.GetType().Name}");
            }
        }

        // 4. 如果连默认值都没有，抛出异常
        throw new KeyNotFoundException($"配置项 {keyStr} 未找到且未注册默认值");
    }

    /// <summary>
    /// 尝试获取配置值
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="value">输出参数，配置值</param>
    /// <returns>是否成功获取（包括默认值）</returns>
    public static bool TryGet<T>(object key, out T value)
    {
        try
        {
            value = Get<T>(key);
            return true;
        }
        catch (KeyNotFoundException)
        {
            value = default!;
            return false;
        }
    }

    /// <summary>
    /// 获取配置值，如果不存在则使用指定的默认值
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>配置值或指定的默认值</returns>
    public static T GetOrDefault<T>(object key, T defaultValue)
    {
        var keyStr = key.ToString()!;

        // 1. 优先从配置文件获取
        if (_instance!._manager!.ContainsKey(keyStr))
        {
            try
            {
                var configValue = _instance._manager.Get<T>(keyStr);
                Log.Debug($"从配置文件获取配置 {keyStr}: {configValue}");
                return configValue;
            }
            catch (Exception ex)
            {
                Log.Warn($"从配置文件获取配置 {keyStr} 失败: {ex.Message}");
            }
        }

        // 2. 如果配置文件中不存在，尝试从环境变量获取
        try
        {
            var envValue = GetFromEnvironmentVariable<T>(keyStr);
            if (envValue != null)
            {
                Log.Debug($"从环境变量获取配置 {keyStr}: {envValue}");
                return envValue;
            }
        }
        catch (Exception e)
        {
            Log.Debug($"{keyStr} <UNK>: {e.Message}");
        }


        // 3. 返回指定的默认值
        Log.Debug($"使用指定的默认值 {keyStr}: {defaultValue}");
        return defaultValue;
    }

    /// <summary>
    /// 检查配置是否存在（包括默认值）
    /// </summary>
    /// <param name="key">配置键</param>
    /// <returns>是否存在</returns>
    public static bool HasValue(object key)
    {
        var keyStr = key.ToString()!;

        // 检查配置文件
        if (_instance!._manager!.ContainsKey(keyStr))
            return true;

        // 检查环境变量
        var envKey = ConvertKeyToEnvironmentVariable(keyStr);
        if (EnvironmentVariableManager.HasEnvironmentVariable(envKey))
            return true;

        // 检查是否有注册的默认值
        return _instance._defaultValues.ContainsKey(keyStr);
    }

    /// <summary>
    /// 从环境变量获取配置值
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <returns>配置值</returns>
    private static T? GetFromEnvironmentVariable<T>(string key)
    {
        // 将配置键转换为环境变量名（大写，点号替换为下划线）
        var envKey = $"SIGMUS_{ConvertKeyToEnvironmentVariable(key)}";

        return EnvironmentVariableManager.GetEnvironmentVariable<T>(envKey);
    }

    /// <summary>
    /// 将配置键转换为环境变量名
    /// 例如：database.connectionString -> DATABASE_CONNECTIONSTRING
    /// </summary>
    /// <param name="key">配置键</param>
    /// <returns>环境变量名</returns>
    internal static string ConvertKeyToEnvironmentVariable(string key)
    {
        return key.Replace('.', '_').ToUpperInvariant();
    }

    /// <summary>
    /// 设置配置值
    /// </summary>
    /// <param name="key">配置键</param>
    /// <param name="value">配置值</param>
    /// <typeparam name="T">配置值类型</typeparam>
    public static void Set<T>(object key, T value)
    {
        try
        {
            var keyStr = key.ToString()!;
            _instance!._manager!.Set(keyStr, value);

            // 更新类型信息
            _instance._configTypes[keyStr] = typeof(T);

            Log.Debug($"设置配置 {keyStr}: {value}");
        }
        finally
        {
            _instance?.OnConfigUpdated.Invoke(key.ToString()!, value);
        }
    }

    public static void Init<T>(object key, T value)
    {
        if (!HasValue(key))
        {
            Set(key, value);
        }
    }

    /// <summary>
    /// 删除配置项
    /// </summary>
    /// <param name="key">配置键</param>
    /// <returns>是否成功删除</returns>
    public static bool Delete(object key)
    {
        try
        {
            var keyStr = key.ToString()!;
            var result = _instance!._manager!.Remove(keyStr);
            if (result)
            {
                Log.Debug($"删除配置 {keyStr}");
            }

            return result;
        }
        finally
        {
            _instance?.OnConfigUpdated.Invoke(key.ToString()!, null);
        }
    }

    /// <summary>
    /// 获取所有配置项（包括默认值）
    /// </summary>
    /// <returns>配置项字典</returns>
    public static Dictionary<string, object> GetAll()
    {
        var result = new Dictionary<string, object>();

        // 添加配置文件中的值
        var configValues = _instance!._manager!.GetAll();
        foreach (var kvp in configValues)
        {
            result[kvp.Key] = kvp.Value;
        }

        // 添加默认值（如果配置文件中不存在）
        foreach (var kvp in _instance._defaultValues)
        {
            if (!result.ContainsKey(kvp.Key))
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// 获取配置项信息
    /// </summary>
    /// <param name="key">配置键</param>
    /// <returns>配置项信息</returns>
    public static ConfigItemInfo GetConfigInfo(object key)
    {
        var keyStr = key.ToString()!;

        return new ConfigItemInfo
        {
            Key = keyStr,
            Type = _instance!._configTypes.GetValueOrDefault(keyStr),
            HasDefaultValue = _instance._defaultValues.ContainsKey(keyStr),
            DefaultValue = _instance._defaultValues.GetValueOrDefault(keyStr),
            Source = GetConfigSource(keyStr),
            HasValue = HasValue(key)
        };
    }

    /// <summary>
    /// 获取配置来源信息
    /// </summary>
    /// <param name="key">配置键</param>
    /// <returns>配置来源描述</returns>
    private static string GetConfigSource(string key)
    {
        // 检查配置文件
        if (_instance!._manager!.ContainsKey(key))
            return "配置文件";

        // 检查环境变量
        var envKey = ConvertKeyToEnvironmentVariable(key);
        if (EnvironmentVariableManager.HasEnvironmentVariable(envKey, EnvironmentVariableTarget.Process))
            return "进程环境变量";

        if (EnvironmentVariableManager.HasEnvironmentVariable(envKey, EnvironmentVariableTarget.User))
            return "用户环境变量";

        if (EnvironmentVariableManager.HasEnvironmentVariable(envKey, EnvironmentVariableTarget.Machine))
            return "系统环境变量";

        if (_instance._defaultValues.ContainsKey(key))
            return "默认值";

        return "未找到";
    }

    /// <summary>
    /// 重新加载配置文件
    /// </summary>
    public static void Reload()
    {
        _instance!._manager!.Reload();
        Log.Info("配置文件已重新加载");
    }

    /// <summary>
    /// 获取配置文件路径
    /// </summary>
    /// <returns>配置文件路径</returns>
    public static string GetConfigPath()
    {
        return _instance!._configPath;
    }

    /// <summary>
    /// 获取所有注册的配置项信息
    /// </summary>
    /// <returns>配置项信息列表</returns>
    public static List<ConfigItemInfo> GetAllConfigInfo()
    {
        var result = new List<ConfigItemInfo>();
        var allKeys = new HashSet<string>();

        // 收集所有键
        allKeys.UnionWith(_instance!._manager!.GetAll().Keys);
        allKeys.UnionWith(_instance._defaultValues.Keys);

        foreach (var key in allKeys)
        {
            result.Add(GetConfigInfo(key));
        }

        return result;
    }
}

/// <summary>
/// 配置项信息
/// </summary>
public class ConfigItemInfo
{
    /// <summary>
    /// 配置键
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 配置值类型
    /// </summary>
    public Type? Type { get; set; }

    /// <summary>
    /// 是否有默认值
    /// </summary>
    public bool HasDefaultValue { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// 配置来源
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 是否有值（包括默认值）
    /// </summary>
    public bool HasValue { get; set; }
}