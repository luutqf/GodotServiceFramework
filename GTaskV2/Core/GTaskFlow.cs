using GodotServiceFramework.Extensions;
using GodotServiceFramework.GTaskV2.Entity;
using GodotServiceFramework.GTaskV2.Util;
using GodotServiceFramework.Util;
using SigmusV2.GodotServiceFramework.Util;

namespace GodotServiceFramework.GTaskV2;

/// <summary>
/// 这是一个任务流, 内含一个任务树, 以链表的形式,初始化后只保留跟节点.
/// </summary>
public class GTaskFlow(GTaskContext? context)
{
    public string Name { get; private set; } = SnowflakeIdGenerator.NextUId().ToString();

    public readonly GTaskContext Context = context ?? new GTaskContext();

    public BaseGTask? FirstTask { get; set; }
    public BaseGTask? LastTask { get; set; }

    /// <summary>
    /// 我们仍以flow作为入口
    /// </summary>
    public void Start()
    {
        Task.Run(() => FirstTask?.Start());
    }

    public void Stop()
    {
    }

    public void Pause()
    {
    }

    public void Resume()
    {
    }

    public void Initialize(GTaskFlowEntity entity)
    {
        Name = entity.Name;
        var gTasks = entity.Models.ToGTask(context);
        FirstTask = gTasks.First(task => task.Id == entity.FirstNodeId);
        //TODO 如果未设置, 可以自行检索哪个是最后一个
        if (entity.LastNodeId != 0)
            LastTask = gTasks.First(task => task.Id == entity.LastNodeId);
        // FirstTask.Context.LastTaskId = entity.LastNodeId;
        FirstTask.Context.CommonParameters.AddRange(entity.Parameters);
    }
}