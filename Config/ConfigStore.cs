using Godot;
using GodotServiceFramework.Context;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Db;
using GodotServiceFramework.Util;
using AutoGodotService = GodotServiceFramework.Context.Service.AutoGodotService;

namespace GodotServiceFramework.Config;

/// <summary>
/// 注意, 这里的修改无法回退,直接更新文件
///
/// 配置的值通知序列化存储,尽量避免存储到Node类型
/// </summary>
[Order(-1000)]
public partial class ConfigStore : AutoGodotService
{
    private readonly YamlConfigManager _manager;

    private static ConfigStore? _instance;

    public event Action<string, object?> OnConfigUpdated = delegate { };

    public ConfigStore()
    {
        var path = ProjectSettings.GlobalizePath("user://config/default.yaml");

        FileUtils.CreateDirectoryWithCheck(ProjectSettings.GlobalizePath("user://config"));
        _manager = new YamlConfigManager(path);
        _manager.Initialize();

        _instance = this;
    }


    /// <summary>
    /// 初始化, 如果已存在则不更新
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    public static void Init<T>(object key, T value)
    {
        if (_instance!._manager!.Get<T>(key.ToString()!) == null)
        {
            _instance._manager.Set(key.ToString()!, value);
        }
    }

    /// <summary>
    /// 获取一个配置
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? Get<T>(object key)
    {
        return _instance!._manager!.Get<T>(key.ToString()!);
    }


    public static bool TryGet<T>(object key, out T? value)
    {
        var result = _instance!._manager!.Get<T>(key.ToString()!);
        if (result == null)
        {
            value = default;
            return false;
        }

        value = (T)result;
        return true;
    }

    /// <summary>
    /// 删除一个配置
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static bool Delete(object key)
    {
        try
        {
            return _instance!._manager!.Remove(key.ToString()!);
        }
        finally
        {
            _instance?.OnConfigUpdated.Invoke(key.ToString()!, null);
        }
    }

    /// <summary>
    /// 设置一个配置
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    public static void Set<T>(object key, T value)
    {
        try
        {
            _instance!._manager!.Set(key.ToString()!, value);
        }
        finally
        {
            _instance?.OnConfigUpdated.Invoke(key.ToString()!, value);
        }
    }


    public override void Destroy()
    {
    }
}