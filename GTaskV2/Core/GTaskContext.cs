using System.Collections.Concurrent;
using System.Text;
using Godot;
using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.Util;
using SigmusV2.GodotServiceFramework.Util;

namespace GodotServiceFramework.GTaskV2;

/// <summary>
/// 任务上下文, 用于沟通整个任务链
/// </summary>
public partial class GTaskContext : RefCounted
{
    public static readonly ConcurrentDictionary<long, GTaskContext> Contexts = new();

    public long Id { get; set; } = SnowflakeIdGenerator.NextId();

    public TaskType TaskType { get; set; }

    public TaskStatus TaskStatus { get; set; } = TaskStatus.Default;

    // public readonly HashSet<BaseGTask> RunningTasks = [];

    // public readonly Dictionary<BaseGTask,List<string>> Task

    public readonly ConcurrentDictionary<string, Dictionary<string, int>> FlowHistory = [];

    public readonly ConcurrentDictionary<string, StringBuilder> FlowMessage = [];
    
    public readonly ConcurrentDictionary<string, DateTime> EventAndTime = [];

    public readonly ConcurrentDictionary<string, object> CommonParameters = [];

    public readonly Dictionary<string, int> ErrorCounts = [];

    public readonly Dictionary<string, int> FlowCounts = [];

    // public readonly List<string> Messages = [];

    // public long LastTaskId { get; set; } = -1;

    private readonly ConcurrentDictionary<string, List<BaseGTask>> StartTasks = [];

    public void AddStartTask(string name, List<BaseGTask> tasks)
    {
        StartTasks[name] = tasks;
    }

    public List<BaseGTask> GetStartTasks(string name)
    {
        return StartTasks[name];
    }

    /// <summary>
    /// SessionId主要用于将运行状态,定位到一个合适的会话
    /// </summary>
    public ulong SessionId { get; set; }


    #region Tags, 标签相关的操作, 涉及到事件传递

    public event Action<string, int> OnTag = delegate { };

    public readonly List<string> FlowTags = [];

    public bool HasTag(string tag)
    {
        return FlowTags.Contains(tag);
    }

    // public void PutTag(int flowId, string tag)
    // {
    //     FlowTags.Add(tag);
    //     OnTag.Invoke(tag, flowId);
    // }

    public void PutTag(string tag)
    {
        FlowTags.Add(tag);
        OnTag.Invoke(tag, -1);
    }

    public void PutTags(int flowId, params string[] tags)
    {
        foreach (var tag in tags)
        {
            FlowTags.Add(tag);
            OnTag.Invoke(tag, flowId);
        }
    }

    public void PutTags(params string[] tags)
    {
        foreach (var tag in tags)
        {
            FlowTags.Add(tag);
            OnTag.Invoke(tag, -1);
        }
    }

    #endregion

    public static readonly GTaskContext Empty = new();


    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        foreach (var value in CommonParameters.Values)
        {
            switch (value)
            {
                case IDisposable disposableValue:
                    disposableValue.Dispose();
                    break;
                case ICloseable closeable:
                    closeable.Close();
                    break;
            }
        }
    }
}

public enum TaskStatus
{
    Default,
    Running,
    Stop,
    Error,
    Pause,
    Complete
}

public enum TaskType
{
    //flow代表当前上下文只存在一个线性任务流
    FLow,

    //loop代表当前上下文存在多个线性任务流,根据某些条件触发.
    Loop
}