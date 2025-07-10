using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GodotServiceFramework.Db;

/// <summary>
/// 基于YAML的键值配置管理器
/// </summary>
public class YamlConfigManager
{
    private readonly string _configPath;
    private Dictionary<string, object> _configData;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;
    private bool _isInitialized = false;

    /// <summary>
    /// 初始化一个新的YAML配置管理器实例
    /// </summary>
    /// <param name="configPath">YAML配置文件路径</param>
    public YamlConfigManager(string configPath)
    {
        _configPath = configPath ?? throw new ArgumentNullException(nameof(configPath));

        // 初始化序列化器，使用驼峰命名约定
        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        // 初始化反序列化器，使用驼峰命名约定
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        _configData = [];
    }

    /// <summary>
    /// 初始化配置管理器并加载配置
    /// </summary>
    /// <returns>初始化是否成功</returns>
    public bool Initialize()
    {
        try
        {
            LoadConfig();
            _isInitialized = true;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"配置初始化失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 异步初始化配置管理器并加载配置
    /// </summary>
    /// <returns>初始化是否成功</returns>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            await LoadConfigAsync();
            _isInitialized = true;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"配置初始化失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 加载YAML配置文件
    /// </summary>
    private void LoadConfig()
    {
        if (!File.Exists(_configPath))
        {
            _configData = [];
            SaveConfig(); // 创建一个空的配置文件
            return;
        }

        string yamlContent = File.ReadAllText(_configPath);
        if (string.IsNullOrWhiteSpace(yamlContent))
        {
            _configData = [];
            return;
        }

        _configData = _deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
    }

    /// <summary>
    /// 异步加载YAML配置文件
    /// </summary>
    private async Task LoadConfigAsync()
    {
        if (!File.Exists(_configPath))
        {
            _configData = [];
            await SaveConfigAsync(); // 创建一个空的配置文件
            return;
        }

        string yamlContent = await File.ReadAllTextAsync(_configPath);
        if (string.IsNullOrWhiteSpace(yamlContent))
        {
            _configData = [];
            return;
        }

        _configData = _deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
    }

    /// <summary>
    /// 保存配置到YAML文件
    /// </summary>
    private void SaveConfig()
    {
        string yamlContent = _serializer.Serialize(_configData);
        File.WriteAllText(_configPath, yamlContent);
    }

    /// <summary>
    /// 异步保存配置到YAML文件
    /// </summary>
    private async Task SaveConfigAsync()
    {
        string yamlContent = _serializer.Serialize(_configData);
        await File.WriteAllTextAsync(_configPath, yamlContent);
    }

    /// <summary>
    /// 检查配置管理器是否已初始化
    /// </summary>
    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("配置管理器尚未初始化，请先调用 Initialize() 方法");
        }
    }

    /// <summary>
    /// 获取指定键的配置值
    /// </summary>
    /// <typeparam name="T">值的类型</typeparam>
    /// <param name="key">配置键</param>
    /// <returns>配置值，如果键不存在则返回默认值</returns>
    public T Get<T>(string key)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (_configData.TryGetValue(key, out var value))
        {
            try
            {
                // 处理YAML反序列化的特殊情况
                if (value is T typedValue)
                {
                    return typedValue;
                }

                // 尝试转换复杂类型
                if (typeof(T).IsClass && !typeof(T).IsValueType && value is Dictionary<object, object> dict)
                {
                    string tempYaml = _serializer.Serialize(dict);
                    return _deserializer.Deserialize<T>(tempYaml);
                }

                // 尝试转换简单类型
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                throw new KeyNotFoundException(key);
            }
        }

        throw new KeyNotFoundException(key);
    }

    /// <summary>
    /// 设置指定键的配置值
    /// </summary>
    /// <typeparam name="T">值的类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="value">配置值</param>
    /// <param name="autoSave">是否自动保存到文件</param>
    public void Set<T>(string key, T value, bool autoSave = true)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        _configData[key] = value!;

        if (autoSave)
        {
            SaveConfig();
        }
    }

    /// <summary>
    /// 异步设置指定键的配置值
    /// </summary>
    /// <typeparam name="T">值的类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="value">配置值</param>
    /// <param name="autoSave">是否自动保存到文件</param>
    public async Task SetAsync<T>(string key, T value, bool autoSave = true)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        _configData[key] = value!;

        if (autoSave)
        {
            await SaveConfigAsync();
        }
    }

    /// <summary>
    /// 判断配置中是否包含指定键
    /// </summary>
    /// <param name="key">配置键</param>
    /// <returns>如果键存在返回true，否则返回false</returns>
    public bool ContainsKey(string key)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        return _configData.ContainsKey(key);
    }

    /// <summary>
    /// 从配置中移除指定键
    /// </summary>
    /// <param name="key">配置键</param>
    /// <param name="autoSave">是否自动保存到文件</param>
    /// <returns>如果键被移除返回true，否则返回false</returns>
    public bool Remove(string key, bool autoSave = true)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        bool removed = _configData.Remove(key);

        if (removed && autoSave)
        {
            SaveConfig();
        }

        return removed;
    }

    /// <summary>
    /// 异步从配置中移除指定键
    /// </summary>
    /// <param name="key">配置键</param>
    /// <param name="autoSave">是否自动保存到文件</param>
    /// <returns>如果键被移除返回true，否则返回false</returns>
    public async Task<bool> RemoveAsync(string key, bool autoSave = true)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        bool removed = _configData.Remove(key);

        if (removed && autoSave)
        {
            await SaveConfigAsync();
        }

        return removed;
    }

    /// <summary>
    /// 获取所有配置项
    /// </summary>
    /// <returns>配置项字典</returns>
    public Dictionary<string, object> GetAll()
    {
        EnsureInitialized();
        return new Dictionary<string, object>(_configData);
    }

    /// <summary>
    /// 清空所有配置
    /// </summary>
    /// <param name="autoSave">是否自动保存到文件</param>
    public void Clear(bool autoSave = true)
    {
        EnsureInitialized();
        _configData.Clear();

        if (autoSave)
        {
            SaveConfig();
        }
    }

    /// <summary>
    /// 异步清空所有配置
    /// </summary>
    /// <param name="autoSave">是否自动保存到文件</param>
    public async Task ClearAsync(bool autoSave = true)
    {
        EnsureInitialized();
        _configData.Clear();

        if (autoSave)
        {
            await SaveConfigAsync();
        }
    }

    /// <summary>
    /// 保存当前配置到文件
    /// </summary>
    public void Save()
    {
        EnsureInitialized();
        SaveConfig();
    }

    /// <summary>
    /// 异步保存当前配置到文件
    /// </summary>
    public async Task SaveAsync()
    {
        EnsureInitialized();
        await SaveConfigAsync();
    }

    /// <summary>
    /// 重新加载配置文件
    /// </summary>
    public void Reload()
    {
        EnsureInitialized();
        LoadConfig();
    }

    /// <summary>
    /// 异步重新加载配置文件
    /// </summary>
    public async Task ReloadAsync()
    {
        EnsureInitialized();
        await LoadConfigAsync();
    }
}