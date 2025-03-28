using System.Collections;

namespace GodotServiceFramework.Util;

public static class EnvUtils
{
    /// <summary>
    /// 获取所有环境变量
    /// </summary>
    /// <returns></returns>
    public static Dictionary<string, string> GetEnvs()
    {
        return Environment.GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!);
    }
}