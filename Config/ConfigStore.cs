using Godot;
using GodotServiceFramework.Context;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Db;
using GodotServiceFramework.Util;
using AutoGodotService = GodotServiceFramework.Context.Service.AutoGodotService;

namespace GodotServiceFramework.Config;

/// <summary>
/// 全局配置管理器，支持多级配置源
/// 配置优先级：配置文件 > 环境变量（进程 > 用户 > 系统）
/// </summary>
[Order(-1000)]
public partial class ConfigStore : AutoGodotService
{
    private readonly YamlConfigManager _manager;
    private readonly string _configPath;

    private static ConfigStore? _instance;

    public event Action<string, object?> OnConfigUpdated = delegate { };

    public ConfigStore()
    {
        // 1. 从环境变量获取配置文件目录，如果无法获取，则使用默认路径
        var configDir = GetConfigDirectoryFromEnvironment();
        _configPath = Path.Combine(configDir, "default.yaml");

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
    /// 获取配置值，支持多级配置源
    /// 优先级：配置文件 > 环境变量（进程 > 用户 > 系统）
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>配置值</returns>
    public static T? Get<T>(object key, T? defaultValue = default)
    {
        var keyStr = key.ToString()!;

        // 1. 优先从配置文件获取
        var configValue = _instance!._manager!.Get<T>(keyStr);
        if (configValue != null)
        {
            Log.Debug($"从配置文件获取配置 {keyStr}: {configValue}");
            return configValue;
        }

        // 2. 如果配置文件中不存在，尝试从环境变量获取
        var envValue = GetFromEnvironmentVariable<T>(keyStr);
        if (envValue != null)
        {
            Log.Debug($"从环境变量获取配置 {keyStr}: {envValue}");
            return envValue;
        }

        // 3. 返回默认值
        Log.Debug($"配置 {keyStr} 未找到，使用默认值: {defaultValue}");
        return defaultValue;
    }

    /// <summary>
    /// 从环境变量获取配置值
    /// 优先级：进程 > 用户 > 系统
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <returns>配置值</returns>
    private static T? GetFromEnvironmentVariable<T>(string key)
    {
        // 将配置键转换为环境变量名（大写，点号替换为下划线）
        var envKey = ConvertKeyToEnvironmentVariable(key);

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
    /// 尝试获取配置值
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="value">输出参数，配置值</param>
    /// <returns>是否成功获取</returns>
    public static bool TryGet<T>(object key, out T? value)
    {
        var result = Get<T>(key);
        if (result == null)
        {
            value = default;
            return false;
        }

        value = result;
        return true;
    }

    /// <summary>
    /// 初始化配置，如果已存在则不更新
    /// </summary>
    /// <param name="key">配置键</param>
    /// <param name="value">配置值</param>
    /// <typeparam name="T">配置值类型</typeparam>
    public static void Init<T>(object key, T value)
    {
        var keyStr = key.ToString()!;

        // 检查配置是否已存在
        if (_instance!._manager!.Get<T>(keyStr) == null)
        {
            _instance._manager.Set(keyStr, value);
            Log.Debug($"初始化配置 {keyStr}: {value}");
        }
        else
        {
            Log.Debug($"配置 {keyStr} 已存在，跳过初始化");
        }
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
            Log.Debug($"设置配置 {keyStr}: {value}");
        }
        finally
        {
            _instance?.OnConfigUpdated.Invoke(key.ToString()!, value);
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
    /// 获取所有配置项
    /// </summary>
    /// <returns>配置项字典</returns>
    public static Dictionary<string, object> GetAll()
    {
        return _instance!._manager!.GetAll();
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
    /// 检查配置是否存在
    /// </summary>
    /// <param name="key">配置键</param>
    /// <returns>是否存在</returns>
    public static bool ContainsKey(object key)
    {
        var keyStr = key.ToString()!;

        // 检查配置文件
        if (_instance!._manager!.ContainsKey(keyStr))
            return true;

        // 检查环境变量
        var envKey = ConvertKeyToEnvironmentVariable(keyStr);
        return EnvironmentVariableManager.HasEnvironmentVariable(envKey);
    }

    /// <summary>
    /// 获取配置来源信息
    /// </summary>
    /// <param name="key">配置键</param>
    /// <returns>配置来源描述</returns>
    public static string GetConfigSource(object key)
    {
        var keyStr = key.ToString()!;

        // 检查配置文件
        if (_instance!._manager!.ContainsKey(keyStr))
            return "配置文件";

        // 检查环境变量
        var envKey = ConvertKeyToEnvironmentVariable(keyStr);
        if (EnvironmentVariableManager.HasEnvironmentVariable(envKey, EnvironmentVariableTarget.Process))
            return "进程环境变量";

        if (EnvironmentVariableManager.HasEnvironmentVariable(envKey, EnvironmentVariableTarget.User))
            return "用户环境变量";

        if (EnvironmentVariableManager.HasEnvironmentVariable(envKey, EnvironmentVariableTarget.Machine))
            return "系统环境变量";

        return "未找到";
    }
}