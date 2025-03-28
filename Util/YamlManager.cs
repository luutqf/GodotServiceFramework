using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GodotServiceFramework.Util;

/// <summary>
/// yaml管理器，我们用于配置文件
/// </summary>
/// <param name="yamlPath"></param>
public class YamlManager(string yamlPath)
{
    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
    
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    /// <summary>
    /// 读取YAML文件并转换为指定类型
    /// </summary>
    public T ReadYaml<T>() where T : class
    {
        try
        {
            if (!File.Exists(yamlPath))
            {
                throw new FileNotFoundException($"YAML file not found: {yamlPath}");
            }

            using (var reader = new StreamReader(yamlPath))
            {
                return _deserializer.Deserialize<T>(reader);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to read YAML file: {ex.Message}");
        }
    }

    /// <summary>
    /// 将对象序列化并保存为YAML文件
    /// </summary>
    public void WriteYaml<T>(T obj) where T : class
    {
        try
        {
            using (var writer = new StreamWriter(yamlPath))
            {
                _serializer.Serialize(writer, obj);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to write YAML file: {ex.Message}");
        }
    }

    /// <summary>
    /// 读取YAML文件为字典
    /// </summary>
    public Dictionary<string, object> ReadYamlAsDictionary()
    {
        try
        {
            using (var reader = new StreamReader(yamlPath))
            {
                return _deserializer.Deserialize<Dictionary<string, object>>(reader);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to read YAML as dictionary: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新YAML文件中的特定值
    /// </summary>
    public void UpdateValue<T>(string key, T value)
    {
        try
        {
            var dictionary = ReadYamlAsDictionary();
            dictionary[key] = value!;
            WriteYaml(dictionary);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to update YAML value: {ex.Message}");
        }
    }

    /// <summary>
    /// 合并两个YAML文件
    /// </summary>
    public void MergeYamlFiles(string secondaryYamlPath)
    {
        try
        {
            var primaryData = ReadYamlAsDictionary();
            var secondaryData = new YamlManager(secondaryYamlPath).ReadYamlAsDictionary();

            foreach (var kvp in secondaryData)
            {
                if (!primaryData.ContainsKey(kvp.Key))
                {
                    primaryData[kvp.Key] = kvp.Value;
                }
            }

            WriteYaml(primaryData);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to merge YAML files: {ex.Message}");
        }
    }

    /// <summary>
    /// 验证YAML文件格式是否正确
    /// </summary>
    public bool ValidateYamlFormat()
    {
        try
        {
            using (var reader = new StreamReader(yamlPath))
            {
                _deserializer.Deserialize<object>(reader);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}