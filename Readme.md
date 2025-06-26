# GodotServiceFramework

一个基于 Godot 4.5 和 C# 的服务框架，提供配置管理、任务系统、依赖注入、数据绑定等功能。

## 概述

GodotServiceFramework 是一个专为 Godot 游戏引擎设计的服务框架，旨在简化游戏开发中的常见任务，提供企业级的开发体验。

## 核心功能

### 1. 配置管理

基于 YAML 的配置管理系统，支持配置的读取、写入、热更新和多环境管理。

```csharp
// 获取配置
string daemonImage = ConfigStore.Get<string>("docker.daemonImage");
int timeout = ConfigStore.Get<int>("database.timeout");

// 设置配置
ConfigStore.Set("database.timeout", 60);

// 监听配置变更
ConfigStore.OnConfigUpdated += (key, value) =>
{
    Console.WriteLine($"配置 {key} 已更新为 {value}");
};
```

### 2. 任务系统

支持任务链、条件分支、并行执行的任务编排系统。

#### GTask (第一代任务系统)
```csharp
public class MyTask : GameTask
{
    public override async Task<int> Execute(GameTaskContext context)
    {
        // 任务逻辑
        return 100; // 成功
    }
}
```

#### GTaskV2 (第二代任务系统)
```csharp
public class MyGTask : BaseGTask
{
    public override async Task<int> Execute(GTaskContext context)
    {
        // 任务逻辑
        return 100; // 成功
    }
}
```

### 3. 依赖注入

自动服务注册和依赖注入系统。

```csharp
[Autowired]
public override void _Ready()
{
    // 自动注入依赖
}

[ChildNode("Button")]
private Button? _button;
```

### 4. 数据绑定

支持数据绑定和缓存的数据管理系统。

```csharp
public class MyDataNode : Control, IDataNode
{
    public void InitBindData(IBinding data)
    {
        // 初始化绑定数据
    }
}
```

### 5. 节点管理

场景状态管理和页面管理系统。

```csharp
// 打开页面
var page = SceneStatsManager.Instance.OpenPage<MyPage>();

// 切换页面
SceneStatsManager.Instance.ChangePage(page);

// 监听页面变更
SceneStatsManager.OnPageChanged += (pageName) =>
{
    Console.WriteLine($"页面已切换到: {pageName}");
};
```

### 6. HTTP 服务

简单的 HTTP 客户端和服务器。

```csharp
// HTTP 客户端
var client = new SimpleHttpClient();
var response = await client.GetAsync("http://api.example.com/data");

// HTTP 服务器
var server = new SimpleHttpServer();
server.AddRoute("/api/data", (request) => "Hello World");
server.Start(8080);
```

### 7. 数据库工具

支持 SQLite 和 LiteDB 的数据库工具。

```csharp
// SQLite
var connection = SqliteTool.CreateConnection("Data Source=app.db");
var result = await SqliteTool.QueryAsync<User>("SELECT * FROM users");

// LiteDB
var database = LiteDbTool.CreateDatabase("app.db");
var collection = LiteDbTool.GetCollection<User>("users");
```

### 8. 缓存服务

内存缓存和融合缓存服务。

```csharp
// 内存缓存
var cache = new InMemoryCacheService();
cache.Set("key", "value", TimeSpan.FromMinutes(10));
var value = cache.Get<string>("key");

// 融合缓存
var fusionCache = new FusionCacheService();
await fusionCache.SetAsync("key", "value");
var value = await fusionCache.GetAsync<string>("key");
```

## 项目结构

```
GodotServiceFramework/
├── AI/                   # AI 相关功能
├── Cache/                # 缓存服务
├── Config/               # 配置管理
├── Console/              # 控制台系统
├── Context/              # 上下文管理
├── Db/                   # 数据库工具
├── GTask/                # 任务系统 V1
├── GTaskV2/              # 任务系统 V2
├── Http/                 # HTTP 服务
├── Nodes/                # 节点管理
└── Util/                 # 工具类
```

## 快速开始

### 1. 安装依赖

确保项目已安装以下依赖：

```xml
<PackageReference Include="YamlDotNet" Version="13.0.0" />
<PackageReference Include="Docker.DotNet" Version="3.125.15" />
<PackageReference Include="Microsoft.Data.Sqlite" Version="7.0.0" />
<PackageReference Include="LiteDB" Version="5.0.17" />
```

### 2. 配置自动加载

在 `project.godot` 中配置自动加载：

```ini
[autoload]
ConfigStore="*res://GodotServiceFramework/Config/ConfigStore.cs"
SceneStatsManager="*res://GodotServiceFramework/Nodes/SceneStatsManager.cs"
```

### 3. 初始化配置

```csharp
public override void _Ready()
{
    // 初始化配置
    ConfigStore.Init("database.connectionString", "Data Source=app.db");
    ConfigStore.Init("server.port", 8080);
    ConfigStore.Init("docker.daemonImage", "ubuntu:latest");
}
```

### 4. 创建服务

```csharp
[GodotComponent]
public class MyService : AutoGodotService
{
    [Autowired]
    public override void _Ready()
    {
        // 服务初始化
    }
    
    public void DoSomething()
    {
        // 业务逻辑
    }
}
```

### 5. 创建任务

```csharp
public class MyTask : BaseGTask
{
    public override async Task<int> Execute(GTaskContext context)
    {
        try
        {
            Log.Info("开始执行任务");
            
            // 任务逻辑
            await Task.Delay(1000);
            
            Log.Info("任务执行完成");
            return 100;
        }
        catch (Exception ex)
        {
            Log.Error($"任务执行失败: {ex.Message}");
            return -1;
        }
    }
}
```

## 配置说明

### 配置文件格式

```yaml
# 数据库配置
database:
  connectionString: "Data Source=app.db"
  timeout: 30

# Docker 配置
docker:
  daemonImage: "ubuntu:latest"
  baseRaspDir: "/tmp/sigmus/sophon/"

# 服务器配置
server:
  host: "localhost"
  port: 8080

# 安全配置
security:
  aes:
    key: "your-secret-key"
    initVector: "your-init-vector"
```

### 环境变量支持

```csharp
// 根据环境变量加载不同配置
var env = Environment.GetEnvironmentVariable("APP_ENV") ?? "dev";
var configFile = $"user://config/default.{env}.yaml";
```

## 最佳实践

### 1. 服务设计

- 使用 `AutoGodotService` 作为服务基类
- 通过 `[Autowired]` 特性实现依赖注入
- 使用 `[ChildNode]` 特性绑定子节点

### 2. 任务设计

- 继承 `BaseGTask` 创建任务
- 返回状态码：100 表示成功，-1 表示失败
- 使用 `Log` 类记录日志

### 3. 配置管理

- 使用点分隔的层次结构命名配置
- 为可选配置提供默认值
- 监听配置变更事件

### 4. 错误处理

```csharp
try
{
    // 业务逻辑
}
catch (Exception ex)
{
    Log.Error($"操作失败: {ex.Message}");
    // 错误处理
}
```

### 5. 性能优化

- 使用异步方法避免阻塞
- 合理使用缓存
- 及时释放资源

## 扩展开发

### 1. 自定义服务

```csharp
[GodotComponent]
public class CustomService : AutoGodotService
{
    public override void _EnterTree()
    {
        // 服务注册时的初始化
    }
    
    public override void _Ready()
    {
        // 依赖注入完成后的初始化
    }
}
```

### 2. 自定义任务

```csharp
public class CustomTask : BaseGTask
{
    public override async Task<int> Execute(GTaskContext context)
    {
        // 自定义任务逻辑
        return 100;
    }
}
```

### 3. 自定义配置提供者

```csharp
public interface IConfigProvider
{
    T Get<T>(string key);
    void Set<T>(string key, T value);
    bool TryGet<T>(string key, out T value);
}

public class CustomConfigProvider : IConfigProvider
{
    // 实现自定义配置提供者
}
```

## 故障排除

### 常见问题

1. **配置无法读取**
   - 检查配置文件路径和权限
   - 验证 YAML 格式

2. **服务无法注册**
   - 确保类继承自 `AutoGodotService`
   - 检查 `[GodotComponent]` 特性

3. **任务执行失败**
   - 检查任务参数
   - 查看错误日志

4. **依赖注入失败**
   - 确保使用 `[Autowired]` 特性
   - 检查服务注册顺序

### 调试技巧

```csharp
// 启用详细日志
Log.Debug("调试信息");

// 检查配置
var allConfig = ConfigStore._manager?.GetAll();
foreach (var kvp in allConfig)
{
    Log.Info($"{kvp.Key}: {kvp.Value}");
}

// 监控内存使用
var memory = GC.GetTotalMemory(false);
Log.Info($"内存使用: {memory / 1024 / 1024}MB");
```

## 版本历史

### v2.0.0
- 重构任务系统，引入 GTaskV2
- 优化配置管理
- 增强依赖注入
- 改进错误处理

### v1.0.0
- 初始版本
- 基础服务框架
- 配置管理
- 任务系统 V1

## 贡献指南

1. Fork 项目
2. 创建功能分支
3. 提交更改
4. 创建 Pull Request

## 许可证

本项目采用 MIT 许可证。

## 支持

如有问题或建议，请：
1. 查看 [常见问题](../docs/faq.md)
2. 提交 Issue
3. 联系开发团队 