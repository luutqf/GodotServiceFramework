using Godot;
using GodotServiceFramework.GTaskV2.Model;

namespace GodotServiceFramework.GTaskV2;

/// <summary>
/// 任务上下文, 用于沟通整个任务链
/// </summary>
public partial class GTaskContext : RefCounted
{
    public TaskType TaskType { get; set; }

    public TaskStatus TaskStatus { get; set; } = TaskStatus.Default;

    public readonly HashSet<BaseGTask> RunningTasks = [];

    public readonly List<GTaskModel> TasksHistory = [];

    public readonly Dictionary<string, object> CommonParameters = [];

    public readonly Dictionary<string, int> ErrorCounts = [];

    public readonly Dictionary<string, int> FlowCounts = [];


    /// <summary>
    /// SessionId主要用于将运行状态,定位到一个合适的会话
    /// </summary>
    public ulong SessionId { get; set; }


    #region Tags, 标签相关的操作, 涉及到事件传递

    public event Action<string, int> OnTag = delegate { };

    public readonly HashSet<string> FlowTags = [];

    public bool HasTag(string tag)
    {
        return FlowTags.Contains(tag);
    }

    public void PutTag(int flowId, string tag)
    {
        FlowTags.Add(tag);
        OnTag.Invoke(tag, flowId);
    }

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
}

public enum TaskStatus
{
    Default,
    Running,
    Stop,
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