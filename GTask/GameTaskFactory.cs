using Godot;
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
public partial class GameTaskFactory : AutoGodotService, ICloseable
{
    /// <summary>
    /// 这里缓存着任务名称和类型
    /// </summary>
    private readonly Dictionary<string, Type> _taskTypes = [];

    public SQLiteConnection? Db;

    private readonly Dictionary<ulong, GameTask> _tasks = [];

    private static readonly int[] DefaultArgs = [0, 0];

    public event Action<GameTask> OnTaskAdded = delegate { };


    public override void _Ready()
    {
        Log.Info("任务工厂已加载");
        var globalizePath = ProjectSettings.GlobalizePath("user://data/db.sqlite");
        Db = SqliteTool.Db(globalizePath, out _,
            initTables: [typeof(GameTaskEntity), typeof(GameTaskFlowEntity), typeof(GameTaskLoopEntity)]);
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
                    (GameTask)Activator.CreateInstance(type, null, DefaultArgs, new Dictionary<string, object>())!;
                _taskTypes.TryAdd(instance.Name, type);
                Log.Debug($"Task {instance.Name} has been found");
            }
        }
    }


    public GameTask? TaskFromId(ulong id)
    {
        return _tasks.GetValueOrDefault(id);
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
            Log.Error($"Task {taskName} not found");
            return null!;
        }

        var instance = Activator.CreateInstance(type, gameTaskWorkflow, index, args);

        var gameTask = (GameTask)instance!;
        gameTask.Title = title;

        _tasks.TryAdd(gameTask.Id, gameTask);
        OnTaskAdded.Invoke(gameTask);
        return gameTask;
    }

    public GameTaskLoop LoadGameTaskLoop(string name)
    {
        return Db!.Table<GameTaskLoopEntity>().FirstOrDefault(entity => entity.Name == name).ToTaskLoop();
    }

    public void Close()
    {
        _taskTypes.Clear();
        Db?.Dispose();
    }

    public void Remove(ulong id)
    {
        _tasks.Remove(id);
    }



    public override void Destroy()
    {
        Close();
    }
}