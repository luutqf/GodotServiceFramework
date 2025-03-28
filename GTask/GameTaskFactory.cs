using Godot;
using GodotServiceFramework.Context;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Db;
using GodotServiceFramework.GTask.Entity;
using GodotServiceFramework.Util;
using SQLite;

namespace GodotServiceFramework.GTask;

/// <summary>
/// 任务工厂
/// </summary>
// [AutoGlobalService]
// ReSharper disable once ClassNeverInstantiated.Global
public partial class GameTaskFactory : Node, ICloseable, IService
{
    /// <summary>
    /// 这里缓存着任务名称和类型
    /// </summary>
    private readonly Dictionary<string, Type> _taskTypes = [];

    public SQLiteConnection? Db;

    public override void _Ready()
    {
        GD.Print("任务工厂已加载");
        var globalizePath = ProjectSettings.GlobalizePath("user://data/db.sqlite");
        Db = SqliteTool.Db(globalizePath, out _, initTables: [typeof(GameTaskEntity), typeof(GameTaskFlowEntity)]);
    }

    public GameTaskFactory()
    {
        // 这里会收集所有实现GameTask的类, 需要取出时,就实例化一个.
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            // 获取当前程序集

            var typesWithMyAttribute = assembly.GetTypes()
                .Where(type => type.IsSubclassOf(typeof(GameTask)));

            foreach (var type in typesWithMyAttribute)
            {
                if (type.IsAbstract || type.IsInterface) continue;
                var instance =
                    (GameTask)Activator.CreateInstance(type, null, new[] { 0, 0 }, new Dictionary<string, object>())!;
                _taskTypes.TryAdd(instance.Name, type);
                Logger.Debug($"Task {instance.Name} has been found");
            }
        }
    }


    /// <summary>
    /// 创建一个任务实例, 静态
    /// </summary>
    /// <param name="taskName"></param>
    /// <param name="gameTaskWorkflow"></param>
    /// <param name="index"></param>
    /// <param name="args"></param>
    /// <param name="title"></param>
    /// <returns></returns>
    public GameTask CreateGameTask(string taskName, GameTaskWorkflow gameTaskWorkflow, int[] index,
        IDictionary<string, object>? args, string title = "")
    {
        if (!_taskTypes.TryGetValue(taskName, out var type))
        {
            Logger.Error($"Task {taskName} not found");
            return null!;
        }

        var instance = Activator.CreateInstance(type, gameTaskWorkflow, index, args);

        var gameTask = (GameTask)instance!;
        gameTask.Title = title;
        return gameTask;
    }

    public void Close()
    {
        _taskTypes.Clear();
        Db?.Dispose();
    }

    public void Init()
    {
    }

    public void Destroy()
    {
        Close();
    }
}