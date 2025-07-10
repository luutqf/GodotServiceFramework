# ConfigStoreV2 - 改进的配置管理系统

## 概述

`ConfigStoreV2` 是对原有 `ConfigStore` 的重大改进，解决了类型安全和默认值处理的问题，特别是解决了 `int` 类型配置项返回 `0` 的问题。

## 主要改进

### 1. 解决 int 类型返回 0 的问题

**原有问题：**
```csharp
// 旧版本的问题
var port = ConfigStore.GetConfig<int>("server.port"); // 如果配置不存在，会抛出异常
if (ConfigStore.TryGet<int>("server.port", out var port)) // 如果配置不存在，port 会是 0
{
    // 无法区分是配置不存在还是配置值就是 0
}
```

**新版本解决方案：**
```csharp
// 新版本 - 注册配置项和默认值
ConfigStoreV2.Register("server.port", 8080, "服务器端口");

// 获取配置值 - 如果不存在会返回注册的默认值
var port = ConfigStoreV2.Get<int>("server.port"); // 返回 8080（默认值）

// 安全地尝试获取
if (ConfigStoreV2.TryGet<int>("server.port", out var port))
{
    // port 要么是配置文件中的值，要么是注册的默认值
    Console.WriteLine($"端口: {port}");
}

// 或者指定自定义默认值
var customPort = ConfigStoreV2.GetOrDefault<int>("server.port", 9090);
```

### 2. 配置项注册机制

新版本引入了配置项注册机制，可以在初始化时注册所有配置项及其默认值：

```csharp
// 在应用启动时注册所有配置项
public static void InitializeConfigs()
{
    ConfigStoreV2.Register("database.connectionString", "Data Source=./sigmus.db", "数据库连接字符串");
    ConfigStoreV2.Register("server.port", 8080, "服务器端口");
    ConfigStoreV2.Register("server.maxConnections", 100, "最大连接数");
    ConfigStoreV2.Register("logging.enabled", true, "是否启用日志");
    ConfigStoreV2.Register("logging.level", "INFO", "日志级别");
    ConfigStoreV2.Register("game.mode", "normal", "游戏模式");
    ConfigStoreV2.Register("game.playerTimeout", 300, "玩家超时时间（秒）");
}
```

### 3. 多种获取方式

#### 3.1 使用注册的默认值
```csharp
// 如果配置不存在，返回注册的默认值
var port = ConfigStoreV2.Get<int>("server.port");
```

#### 3.2 安全尝试获取
```csharp
// 安全地尝试获取，返回 true 表示成功（包括默认值）
if (ConfigStoreV2.TryGet<int>("server.port", out var port))
{
    Console.WriteLine($"端口: {port}");
}
```

#### 3.3 指定自定义默认值
```csharp
// 如果配置不存在，使用指定的默认值
var port = ConfigStoreV2.GetOrDefault<int>("server.port", 9090);
```

### 4. 配置项信息查询

新版本提供了丰富的配置项信息查询功能：

```csharp
// 获取配置项详细信息
var configInfo = ConfigStoreV2.GetConfigInfo("server.port");
Console.WriteLine($"键: {configInfo.Key}");
Console.WriteLine($"类型: {configInfo.Type?.Name}");
Console.WriteLine($"有默认值: {configInfo.HasDefaultValue}");
Console.WriteLine($"默认值: {configInfo.DefaultValue}");
Console.WriteLine($"来源: {configInfo.Source}"); // 配置文件/环境变量/默认值
Console.WriteLine($"有值: {configInfo.HasValue}");

// 获取所有配置项信息
var allConfigInfos = ConfigStoreV2.GetAllConfigInfo();
foreach (var info in allConfigInfos)
{
    Console.WriteLine($"{info.Key}: {info.Source}");
}
```

### 5. 配置存在性检查

```csharp
// 检查配置是否存在（包括默认值）
if (ConfigStoreV2.HasValue("server.port"))
{
    var port = ConfigStoreV2.Get<int>("server.port");
    Console.WriteLine($"端口配置存在: {port}");
}
```

## 配置优先级

配置值的获取优先级（从高到低）：

1. **配置文件** - YAML 文件中的值
2. **环境变量** - 系统环境变量（进程 > 用户 > 系统）
3. **注册的默认值** - 通过 `Register` 方法注册的默认值
4. **自定义默认值** - 通过 `GetOrDefault` 指定的默认值

## 环境变量支持

环境变量名称会自动转换：
- 配置键：`server.port` → 环境变量：`SIGMUS_SERVER_PORT`
- 配置键：`database.connectionString` → 环境变量：`SIGMUS_DATABASE_CONNECTIONSTRING`

## 使用建议

### 1. 在应用启动时注册所有配置项

```csharp
public class Startup
{
    public static void InitializeConfigs()
    {
        // 数据库配置
        ConfigStoreV2.Register("database.connectionString", "Data Source=./sigmus.db");
        ConfigStoreV2.Register("database.timeout", 30);
        
        // 服务器配置
        ConfigStoreV2.Register("server.port", 8080);
        ConfigStoreV2.Register("server.maxConnections", 100);
        ConfigStoreV2.Register("server.enableHttps", false);
        
        // 日志配置
        ConfigStoreV2.Register("logging.enabled", true);
        ConfigStoreV2.Register("logging.level", "INFO");
        ConfigStoreV2.Register("logging.filePath", "./logs/sigmus.log");
        
        // 游戏配置
        ConfigStoreV2.Register("game.mode", "normal");
        ConfigStoreV2.Register("game.playerTimeout", 300);
        ConfigStoreV2.Register("game.maxPlayers", 50);
    }
}
```

### 2. 在代码中安全地使用配置

```csharp
public class GameServer
{
    public void Start()
    {
        // 获取服务器配置
        var port = ConfigStoreV2.Get<int>("server.port");
        var maxConnections = ConfigStoreV2.Get<int>("server.maxConnections");
        var enableHttps = ConfigStoreV2.Get<bool>("server.enableHttps");
        
        // 获取数据库配置
        var connectionString = ConfigStoreV2.Get<string>("database.connectionString");
        var timeout = ConfigStoreV2.Get<int>("database.timeout");
        
        // 获取游戏配置
        var gameMode = ConfigStoreV2.Get<string>("game.mode");
        var playerTimeout = ConfigStoreV2.Get<int>("game.playerTimeout");
        
        Console.WriteLine($"启动服务器: 端口={port}, 最大连接={maxConnections}, HTTPS={enableHttps}");
        Console.WriteLine($"数据库: {connectionString}, 超时={timeout}秒");
        Console.WriteLine($"游戏模式: {gameMode}, 玩家超时={playerTimeout}秒");
    }
}
```

### 3. 处理配置变更

```csharp
// 监听配置变更事件
ConfigStoreV2.OnConfigUpdated += (key, value) =>
{
    Console.WriteLine($"配置变更: {key} = {value}");
    
    // 根据配置变更执行相应操作
    switch (key)
    {
        case "logging.level":
            UpdateLogLevel(value?.ToString());
            break;
        case "server.maxConnections":
            UpdateMaxConnections(Convert.ToInt32(value));
            break;
    }
};
```

## 迁移指南

从 `ConfigStore` 迁移到 `ConfigStoreV2`：

### 1. 替换类名
```csharp
// 旧版本
var port = ConfigStore.GetConfig<int>("server.port");

// 新版本
var port = ConfigStoreV2.Get<int>("server.port");
```

### 2. 添加配置项注册
```csharp
// 在应用启动时添加
ConfigStoreV2.Register("server.port", 8080);
```

### 3. 更新错误处理
```csharp
// 旧版本
try
{
    var port = ConfigStore.GetConfig<int>("server.port");
}
catch (KeyNotFoundException)
{
    // 处理配置不存在的情况
}

// 新版本
if (ConfigStoreV2.TryGet<int>("server.port", out var port))
{
    // 使用配置值
}
else
{
    // 处理配置不存在的情况
}
```

## 总结

`ConfigStoreV2` 通过引入配置项注册机制和多种获取方式，彻底解决了类型安全和默认值处理的问题，使配置管理更加可靠和易用。建议在新项目中使用 `ConfigStoreV2`，并逐步将现有项目从 `ConfigStore` 迁移过来。 