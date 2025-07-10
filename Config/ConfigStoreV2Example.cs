using GodotServiceFramework.Config;

namespace GodotServiceFramework.Config;

/// <summary>
/// ConfigStoreV2 使用示例
/// </summary>
public static class ConfigStoreV2Example
{
    /// <summary>
    /// 配置键常量
    /// </summary>
    public static class ConfigKeys
    {
        public const string DatabaseConnectionString = "database.connectionString";
        public const string ServerPort = "server.port";
        public const string MaxConnections = "server.maxConnections";
        public const string EnableLogging = "logging.enabled";
        public const string LogLevel = "logging.level";
        public const string GameMode = "game.mode";
        public const string PlayerTimeout = "game.playerTimeout";
    }

    /// <summary>
    /// 初始化配置项
    /// </summary>
    public static void InitializeConfigs()
    {
        // 注册配置项，指定默认值和类型
        ConfigStore.Register(ConfigKeys.DatabaseConnectionString, "Data Source=./sigmus.db", "数据库连接字符串");
        ConfigStore.Register(ConfigKeys.ServerPort, 8080, "服务器端口");
        ConfigStore.Register(ConfigKeys.MaxConnections, 100, "最大连接数");
        ConfigStore.Register(ConfigKeys.EnableLogging, true, "是否启用日志");
        ConfigStore.Register(ConfigKeys.LogLevel, "INFO", "日志级别");
        ConfigStore.Register(ConfigKeys.GameMode, "normal", "游戏模式");
        ConfigStore.Register(ConfigKeys.PlayerTimeout, 300, "玩家超时时间（秒）");
    }

    /// <summary>
    /// 演示如何安全地获取配置值
    /// </summary>
    public static void DemonstrateSafeConfigAccess()
    {
        // 1. 使用 Get<T>() - 如果配置不存在会返回注册的默认值
        var port = ConfigStore.Get<int>(ConfigKeys.ServerPort);
        Console.WriteLine($"服务器端口: {port}"); // 输出: 8080

        var connectionString = ConfigStore.Get<string>(ConfigKeys.DatabaseConnectionString);
        Console.WriteLine($"数据库连接: {connectionString}"); // 输出: Data Source=./sigmus.db

        // 2. 使用 TryGet<T>() - 安全地尝试获取配置
        if (ConfigStore.TryGet<int>(ConfigKeys.MaxConnections, out var maxConn))
        {
            Console.WriteLine($"最大连接数: {maxConn}"); // 输出: 100
        }
        else
        {
            Console.WriteLine("最大连接数配置未找到");
        }

        // 3. 使用 GetOrDefault<T>() - 指定自定义默认值
        var customTimeout = ConfigStore.GetOrDefault<int>(ConfigKeys.PlayerTimeout, 600);
        Console.WriteLine($"玩家超时时间: {customTimeout}秒");

        // 4. 检查配置是否存在
        if (ConfigStore.HasValue(ConfigKeys.EnableLogging))
        {
            var enableLogging = ConfigStore.Get<bool>(ConfigKeys.EnableLogging);
            Console.WriteLine($"日志启用状态: {enableLogging}");
        }

        // 5. 获取配置信息
        var configInfo = ConfigStore.GetConfigInfo(ConfigKeys.ServerPort);
        Console.WriteLine($"配置项信息:");
        Console.WriteLine($"  键: {configInfo.Key}");
        Console.WriteLine($"  类型: {configInfo.Type?.Name}");
        Console.WriteLine($"  有默认值: {configInfo.HasDefaultValue}");
        Console.WriteLine($"  默认值: {configInfo.DefaultValue}");
        Console.WriteLine($"  来源: {configInfo.Source}");
        Console.WriteLine($"  有值: {configInfo.HasValue}");
    }

    /// <summary>
    /// 演示配置的动态设置和获取
    /// </summary>
    public static void DemonstrateDynamicConfig()
    {
        // 设置新的配置值
        ConfigStore.Set(ConfigKeys.GameMode, "hardcore");
        ConfigStore.Set(ConfigKeys.MaxConnections, 200);

        // 获取设置的值
        var gameMode = ConfigStore.Get<string>(ConfigKeys.GameMode);
        Console.WriteLine($"当前游戏模式: {gameMode}"); // 输出: hardcore

        var maxConn = ConfigStore.Get<int>(ConfigKeys.MaxConnections);
        Console.WriteLine($"当前最大连接数: {maxConn}"); // 输出: 200

        // 删除配置项（会回退到默认值）
        ConfigStore.Delete(ConfigKeys.GameMode);
        var gameModeAfterDelete = ConfigStore.Get<string>(ConfigKeys.GameMode);
        Console.WriteLine($"删除后游戏模式: {gameModeAfterDelete}"); // 输出: normal（默认值）
    }

    /// <summary>
    /// 演示环境变量配置
    /// </summary>
    public static void DemonstrateEnvironmentVariableConfig()
    {
        // 设置环境变量（在实际使用中，这些会在系统环境中设置）
        // Environment.SetEnvironmentVariable("SIGMUS_SERVER_PORT", "9090");
        // Environment.SetEnvironmentVariable("SIGMUS_DATABASE_CONNECTIONSTRING", "Data Source=./prod.db");

        // 获取配置（会优先从环境变量获取）
        var port = ConfigStore.Get<int>(ConfigKeys.ServerPort);
        Console.WriteLine($"从环境变量获取的端口: {port}");

        var dbConn = ConfigStore.Get<string>(ConfigKeys.DatabaseConnectionString);
        Console.WriteLine($"从环境变量获取的数据库连接: {dbConn}");
    }

    /// <summary>
    /// 演示获取所有配置信息
    /// </summary>
    public static void DemonstrateGetAllConfigs()
    {
        // 获取所有配置项
        var allConfigs = ConfigStore.GetAll();
        Console.WriteLine("所有配置项:");
        foreach (var kvp in allConfigs)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }

        // 获取所有配置项信息
        var allConfigInfos = ConfigStore.GetAllConfigInfo();
        Console.WriteLine("\n所有配置项详细信息:");
        foreach (var info in allConfigInfos)
        {
            Console.WriteLine($"  {info.Key}:");
            Console.WriteLine($"    类型: {info.Type?.Name}");
            Console.WriteLine($"    来源: {info.Source}");
            Console.WriteLine($"    有默认值: {info.HasDefaultValue}");
            Console.WriteLine($"    默认值: {info.DefaultValue}");
        }
    }

    /// <summary>
    /// 演示错误处理
    /// </summary>
    public static void DemonstrateErrorHandling()
    {
        // 尝试获取未注册的配置项
        try
        {
            var unknownConfig = ConfigStore.Get<string>("unknown.config");
            Console.WriteLine($"未知配置: {unknownConfig}");
        }
        catch (KeyNotFoundException ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
        }

        // 使用 TryGet 安全地处理
        if (ConfigStore.TryGet<string>("unknown.config", out var value))
        {
            Console.WriteLine($"未知配置: {value}");
        }
        else
        {
            Console.WriteLine("未知配置未找到");
        }

        // 使用 GetOrDefault 提供自定义默认值
        var safeValue = ConfigStore.GetOrDefault<string>("unknown.config", "默认值");
        Console.WriteLine($"安全获取的值: {safeValue}");
    }
} 