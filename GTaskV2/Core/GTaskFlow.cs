using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Extensions;
using GodotServiceFramework.GTaskV2.Entity;
using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.GTaskV2.Tasks;
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

    public BaseGTask? FirstTask { get; private set; }

    public BaseGTask? GTaskGraveyard { get; private set; }

    /// <summary>
    /// 我们仍以flow作为入口
    /// </summary>
    public void Start()
    {
        Log.Info($"{Name} Started ContextId: {Context.Id}");
        foreach (var task in Context.GetStartTasks(Name))
        {
            Task.Run(() => task.Start());
        }

        Task.Run(() => GTaskGraveyard?.Start());
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
        var gTasks = entity.Models.ToStartGTask(context, entity.FirstNodeId);

        FirstTask = gTasks.First(task => task.Id == entity.FirstNodeId);
        FirstTask.Context.AddStartTask(Name, gTasks);

        foreach (var baseGTask in gTasks)
        {
            baseGTask.Flow = this;
        }


        var factory = Services.Get<GTaskFactory>()!;
        var model = new GTaskModel
        {
            Name = nameof(Tasks.GTaskGraveyard),
            NextIds = [],
            Parameters = new Dictionary<string, object>
            {
                ["flowName"] = Name,
            },
        };
        GTaskGraveyard = factory.CreateTask(model, FirstTask.Context);
        GTaskGraveyard.Flow = this;
        FirstTask.Context.CommonParameters.AddRange(entity.Parameters);
    }
}