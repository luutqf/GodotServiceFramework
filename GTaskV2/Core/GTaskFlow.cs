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
public class GTaskFlow(GTaskContext context)
{
    public string Name { get; private set; } = SnowflakeIdGenerator.NextUId().ToString();

    public readonly GTaskContext Context = context;

    public BaseGTask? GTaskGraveyard { get; private set; }

    public List<BaseGTask> StartTasks { get; private set; } = [];

    /// <summary>
    /// 我们仍以flow作为入口
    /// </summary>
    public void Start()
    {
        Log.Info($"{Name} Started ContextId: {Context.Id}");

        foreach (var task in StartTasks)
        {
            Task.Run(() => task.Start());
        }

        Task.Run(() => GTaskGraveyard?.Start());
    }


    public void Stop()
    {
        StartTasks.Clear();
        // Context.Dispose();
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
        var gTasks = entity.Models.ModelToStartGTask(this, entity.FirstNodeId);

        AddStartTask(gTasks);

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
        GTaskGraveyard = factory.CreateTask(model, this);
        GTaskGraveyard.Flow = this;
        Context.CommonParameters.AddRange(entity.Parameters);
    }

    private void AddStartTask(List<BaseGTask> gTasks)
    {
        StartTasks = gTasks;
    }
}