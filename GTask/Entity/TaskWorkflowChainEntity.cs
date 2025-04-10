using Godot;
using GodotServiceFramework.Binding;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Context.Session;
using GodotServiceFramework.Extensions;
using GodotServiceFramework.GTask.Service;
using GodotServiceFramework.Util;
using Newtonsoft.Json;
using SQLite;

namespace GodotServiceFramework.GTask.Entity;

/// <summary>
/// 在实际使用中, 会存在组合多个任务流的场景, 那么任务流的上下文切换就需要重视起来了, 我们需要一些状态共享
///
/// 首先,任务流的执行不存在顺序, 而是执行条件.  经典的就是标签,只有当标签满足条件时, 才会
/// </summary>
[Table("game_task_flow_chain_entity")]
public partial class TaskWorkflowChainEntity : RefCounted, IBinding
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")] public string Name { get; set; } = string.Empty;

    [Column("flow_conditions")] public string FlowConditionsJson { get; set; } = string.Empty;

    [Ignore]
    public List<FlowCondition> FlowConditions
    {
        get => JsonConvert.DeserializeObject<List<FlowCondition>>(FlowConditionsJson)!;

        set => FlowConditionsJson = JsonConvert.SerializeObject(value);
    }


    public readonly HashSet<string> FlowTags = [];

    public Dictionary<string, object> CommonArgs = [];

    private Dictionary<GameTaskWorkflow, FlowCondition>? _flowsDict;


    public event Action<string, int> OnTag = delegate { };

    public event Action<string, GameTaskWorkflow> OnFlowStart = delegate { };

    public event Action<string, int[], string, string, int> OnResultLine = delegate { };


    private void PutTag(string tag, int flowId)
    {
        FlowTags.Add(tag);
        foreach (var (taskFlow, condition) in _flowsDict!)
        {
            if (taskFlow.IsStarted || taskFlow.IsDestroyed)
            {
                continue;
            }

            //这里意味着总需要有一个无条件执行的任务流,驱动后续的任务流执行
            if (!condition.Matches(this)) continue;

            //TODO 这是个硬编码, 再想想吧
            if (CommonArgs.TryGetValue("targetApp", out var targetApp))
            {
                taskFlow.Name = taskFlow.Name + "+" + targetApp;
            }

            taskFlow.Context.OnTag += PutTag;
            taskFlow.OnComplete += (name, id) => PutTag($"completed", id);
            taskFlow.OnAfterStart += (name, id) => PutTag($"started", id);
            taskFlow.OnError += (name, id) => PutTag($"failed", id);


            taskFlow.SessionId = this.GetSessionId(out _);

            taskFlow.CommonArgs.AddRange(condition.CommonArgs);
            taskFlow.CommonArgs.AddRange(CommonArgs);

            taskFlow.OnResultLine += (ints, s, arg3, arg4) =>
            {
                if (!condition.ResultOutline) return;
                OnResultLine.Invoke(taskFlow.Name, ints, s, arg3, arg4);
            };
            Logger.Error($"请注意,这个任务所在的Session {taskFlow.SessionId}");

            taskFlow.Start();
            OnFlowStart.Invoke(condition.FlowEntityName, taskFlow);
        }

        OnTag.Invoke(tag, flowId);

        if (!_flowsDict.Keys.All(workflow => workflow.IsDestroyed)) return;

        OnTag = delegate { };
        OnFlowStart = delegate { };
        OnResultLine = delegate { };
        Logger.Error("现在所有任务都完成了");
    }


    public void Start(
        Dictionary<string, object>? commonArgs)
    {
        if (commonArgs != null)
        {
            CommonArgs = commonArgs;
        }

        var flowsDict = ParseFlowEntity(FlowConditions);
        if (flowsDict.Count == 0) return;
        _flowsDict = flowsDict;


        PutTag("chainStarted", 0);
    }

    private Dictionary<GameTaskWorkflow, FlowCondition> ParseFlowEntity(List<FlowCondition> list)
    {
        {
            Dictionary<GameTaskWorkflow, FlowCondition> flowDict = [];
            foreach (var flowCondition in list)
            {
                var workflow =
                    Services.Get<TaskWorkflowService>()!.GetTaskWorkflowByEntityName(flowCondition.FlowEntityName);
                flowDict[workflow] = flowCondition;
            }

            return flowDict;
        }
    }

    public void Stop()
    {
        if (_flowsDict == null) return;
        foreach (var (taskFlow, value) in _flowsDict)
        {
            if (taskFlow is not { IsStarted: true, IsDestroyed: false }) continue;
            taskFlow.StopWorkflow(true);
            taskFlow.Destroy();
        }
    }
}

public class FlowCondition
{
    public string FlowEntityName { get; set; } = string.Empty;

    public bool ResultOutline { get; set; } = true;

    public Dictionary<string, object> CommonArgs { get; set; } = [];

    /// <summary>
    /// 0代表必须, 其他数字代表相同数字的任意tag满足即可
    /// </summary>
    public Dictionary<string, int> FlowTags { get; set; } = [];

    public bool Matches(TaskWorkflowChainEntity chainEntity)
    {
        var matches = new Dictionary<string, bool>();
        foreach (var (key, value) in FlowTags)
        {
            switch (value)
            {
                //0代表必须有, -1代表必须没有
                case -1 when chainEntity.FlowTags.Contains(key):
                case 0 when !chainEntity.FlowTags.Contains(key):
                    return false;

                //大于0的分类,满足任一可通行
                case > 0 when chainEntity.FlowTags.Contains(key):
                    matches[key] = true;
                    break;
                //未满足则暂定为false
                case > 0:
                    matches.TryAdd(key, false);
                    break;
            }
        }

        return matches.Count == 0 || matches.All(pair => pair.Value);
    }
}