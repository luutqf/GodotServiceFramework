using System.Collections.Concurrent;
using System.Text;
using Godot;
using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.GTaskV2.Util;
using GodotServiceFramework.Util;
using SigmusV2.GodotServiceFramework.Util;

namespace GodotServiceFramework.GTaskV2;

/// <summary>
/// 任务上下文, 用于沟通整个任务链.
/// </summary>
public partial class GTaskContext : RefCounted
{
    //缓存已经创建的上下文
    public static GTaskContext? CurrentContext;

    //自动生成的Id
    public long Id { get; } = SnowflakeIdGenerator.NextId();

    public string Name { get; set; } = string.Empty;

    //标明当前上下文属于哪类任务, 普通任务流或任务流循环
    public TaskType TaskType { get; set; }

    //任务状态,如已启动,已停止
    public GTaskStatus GTaskStatus { get; set; } = GTaskStatus.Default;

    //任务流的进度历史, key为任务流Id
    // public readonly ConcurrentDictionary<string, Dictionary<string, int>> FlowHistory = [];

    //任务流的消息列表,key为任务流Id
    public readonly ConcurrentDictionary<string, List<string>> FlowMessage = [];

    //公共参数,每个任务都会从中获取
    public readonly ConcurrentDictionary<string, object> CommonParameters = [];


    public event Action<BaseGTask, ActionType, string> TaskAction = delegate { };


    /// <summary>
    /// SessionId主要用于将运行状态,定位到一个合适的会话
    /// </summary>
    public ulong SessionId { get; set; }

    #region Tags, 标签相关的操作, 涉及到事件传递

    public event Action<string, DateTime> OnTag = delegate { };
    public readonly Dictionary<string, DateTime> FlowTags = [];

    public bool HasTag(string tag)
    {
        return FlowTags.ContainsKey(tag);
    }

    public void PutTag(string tag)
    {
        var dateTime = DateTime.Now;
        FlowTags.Add(tag, dateTime);
        OnTag.Invoke(tag, dateTime);
    }

    public void PutTags(params string[] tags)
    {
        foreach (var tag in tags)
        {
            var dateTime = DateTime.Now;
            FlowTags.Add(tag, dateTime);
            OnTag.Invoke(tag, dateTime);
        }
    }

    #endregion

    public static readonly GTaskContext Empty = new();


    public void SendMessage(BaseGTask task, string message, ActionType actionType = ActionType.Info)
    {
        switch (actionType)
        {
            case ActionType.Error:
            {
                GTaskStatus = GTaskStatus.Error;
                break;
            }
        }

        TaskAction.Invoke(task, actionType, message);
    }


    public void UpdateProgress(BaseGTask task)
    {
        TaskAction.Invoke(task, ActionType.Progress, string.Empty);
    }

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

        TaskAction = delegate { };
    }


    public void Log()
    {
    }
}

public enum GTaskStatus
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

public enum ActionType
{
    Progress,
    Info,
    Success,
    Warn,
    Error,
    Destroy,
}