using System.Collections.Concurrent;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Util;
using System.Linq.Dynamic.Core;
using Godot;
using Godot.Collections;
using SigmusV3.Script;


namespace GodotServiceFramework.GTaskV3;

public sealed class GTaskContext : IDisposable
{
    public static GTaskContext? CurrentContext { get; set; }

    #region 基础属性

    public long Id { get; } = SnowflakeIdGenerator.NextId();

    //用于规则引擎的表达式
    public IQueryable Subject { get; }

    //用于传入可取消任务, 可以一次性取消, 一旦取消, 不可恢复.
    public CancellationTokenSource Cts { get; set; } = new();

    public readonly HashSet<GTaskSet> TaskSets = [];

    public event Action<TaskEvent, GTaskModel?, GTaskSet?, string> TaskAction = delegate { };


    //独属于任务整体的进度, 0为默认, 100为全部完成, 中间数目为计算得出, -1为失败
    public int Progress
    {
        get
        {
            if (TaskSets.Count == 0) return 0;

            // 如果任意一个TaskSet的Progress为-1，则返回-1
            if (TaskSets.Any(set => set.Progress == -1))
                return -1;

            return (int)TaskSets.Average(set => set.Progress);
        }
    }

    //公共参数, 添加任务流时更新, 可覆盖旧参数
    public System.Collections.Generic.Dictionary<string, object> CommonParameters { get; set; } = [];

    #endregion


    #region 种子

    public Queue<object> SeedQueue { get; } = new();

    public int SeedInt { get; set; }

    public void InvokeRuleEngine()
    {
        foreach (var set in TaskSets)
        {
            if (set.Progress is 0 or 100 && new Random().Next() % 3 == 0 && Evaluate(set.Condition))
            {
                //TODO 这里还要加随机,避免总是一个
                if (SeedQueue.TryDequeue(out var seed))
                {
                    Log.Info($"剩余seed:{SeedQueue.Count}");

                    //这里要把所有子任务的进度归零.
                    foreach (var model in set.Pods.SelectMany(pod => pod.Models))
                    {
                        model.Status.Clean();
                    }

                    set.OnComplete += () => { SeedQueue.Enqueue(seed); };
                    set.Start();
                }
            }
        }
    }

    #endregion


    #region tag

    //公共标签,
    private readonly HashSet<string> _tags = [];


    public bool HasTag(string tag)
    {
        return _tags.Contains(tag);
    }

    public void PutTag(string tag)
    {
        _tags.Add(tag);
    }

    #endregion


    public GTaskContext()
    {
        CurrentContext = this;
        Services.Get<GTaskPool>()!.OnTaskRelease += OnTaskRelease;
        PutTag("invited");

        Subject = new[] { this }.AsQueryable();

        for (var i = 0; i < 1; i++)
        {
            SeedQueue.Enqueue(i);
        }
    }

    public void OnTaskRelease(int size)
    {
        Log.Info($"TaskRelease: {size}", BbColor.Aqua);
    }


    #region 生命周期方法

    /// <summary>
    /// 插入一个任务集
    /// </summary>
    /// <param name="set"></param>
    public void Insert(GTaskSet set)
    {
        TaskSets.Add(set);
    }


    /// <summary>
    /// 停止
    /// </summary>
    public void Stop()
    {
    }

    /// <summary>
    /// 暂停
    /// </summary>
    public void Pause()
    {
    }

    /// <summary>
    /// 恢复
    /// </summary>
    public void Resume()
    {
    }

    #endregion

    public void Send(TaskEvent action, GTaskModel? model = null, GTaskSet? set = null, string msg = "",
        BbColor color = BbColor.Gray)
    {
        TaskAction.Invoke(action, model, set, msg);
        switch (action)
        {
            case TaskEvent.Progress:
            {
                if (model!.Progress == 100)
                {
                    Log.Info($"{model.Name} 已完成", BbColor.Green);
                }

                break;
            }

            case TaskEvent.TaskSetComplete:
            {
                Log.Info($"任务集已完成: {set?.Name}", BbColor.Green);
                break;
            }
            case TaskEvent.NextNothing:
            {
                lock (this)
                {
                    if (Progress == 100)
                    {
                        if (!Cts.IsCancellationRequested)
                        {
                            Log.Info($"{Id}->所有TaskSet进度已满, 清理状态", BbColor.Green);
                            Dispose();
                            // Log.Info($"任务池剩余:{Services.Get<GTaskPool>()?.GetUsableSize()}");
                        }
                    }
                }

                break;
            }
            case TaskEvent.Error:
                Log.Error($"{action}: {model?.Name} : {msg}", color);

                if (!Cts.IsCancellationRequested)
                {
                    Log.Info($"{Id}->任务错误, 清理状态", BbColor.Green);
                    Dispose();
                }

                break;
            case TaskEvent.Warning:
                Log.Warn($"{action}: {model?.Name} : {msg}", color);
                break;
            case TaskEvent.SuccessMessage:
                Log.Info($"{action}: {model?.Name} : {msg}", BbColor.Green);
                break;
            case TaskEvent.Info:
                Log.Info($"{action}: {model?.Name} : {msg}", color);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    public void Cancel()
    {
        Dispose();
    }

    public void Dispose()
    {
        CurrentContext = null;
        if (!Cts.IsCancellationRequested) Cts.Cancel();

        Services.Get<GTaskPool>()!.OnTaskRelease -= OnTaskRelease;

        try
        {
            foreach (var value in CommonParameters.Values)
            {
                if (value is IDisposable disposable)
                    disposable.Dispose();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            // throw;
        }

        CommonParameters.Clear();
        CommonRpc.Instance!.CallDeferred(Node.MethodName.Rpc, "ServerMessage", new Dictionary()
        {
            ["resource"] = "status",
            ["status"] = "任务终止",
            ["synonyms"] = true,
            ["users"] = string.Join(",", CommonRpc.OnlineUsers.Values)
        });
    }


    /// <summary>
    /// 规则引擎
    /// </summary>
    /// <param name="condition">触发条件</param>
    /// <returns></returns>
    public bool Evaluate(string condition)
    {
        if (string.IsNullOrEmpty(condition)) return false;
        try
        {
            return Subject.Any(condition);
        }
        catch (Exception e)
        {
            Log.Error($"规则评估错误: {e}");
            return false;
        }
    }
}

public enum TaskEvent
{
    Progress,
    Error,
    Warning,
    SuccessMessage,
    Info,
    NextNothing,
    TaskSetComplete
}