using Godot;
using GodotServiceFramework.Binding;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Context.Session;
using GodotServiceFramework.Db;
using GodotServiceFramework.Extensions;
using GodotServiceFramework.GConsole;
using GodotServiceFramework.GTask.Entity;
using Newtonsoft.Json;
using SQLite;

namespace GodotServiceFramework.GTask.Service;

[InjectService]
public partial class TaskWorkflowService
{
    public TaskWorkflowService()
    {
        SqliteManager.Instance.AddTableTypes([
            typeof(GameTaskFlowEntity), typeof(StepTaskEntity),
            typeof(TaskWorkflowChainEntity)
        ]);
    }


    /// <summary>
    /// 就先预先创建就好,名字不允许重复
    /// </summary>
    /// <param name="taskWorkflow"></param>
    /// <returns></returns>
    public bool CreateTaskWorkflowEntity(GameTaskFlowEntity taskWorkflow)
    {
        return SqliteManager.Instance.Insert(taskWorkflow);
    }


    public bool DeleteTaskWorkflow(GameTaskFlowEntity taskWorkflow)
    {
        return SqliteManager.Instance.Delete(taskWorkflow);
    }

    public bool UpdateTaskWorkflow(GameTaskFlowEntity taskWorkflow)
    {
        return SqliteManager.Instance.Update(taskWorkflow);
    }

    [BindingCache]
    public List<GameTaskFlowEntity> ListTaskWorkflows()
    {
        return SqliteManager.Table<GameTaskFlowEntity>().ToList();
    }

    [BindingCache]
    public GameTaskFlowEntity GetTaskWorkflowEntityByName(string name)
    {
        var flowEntity = SqliteManager.Table<GameTaskFlowEntity>().FirstOrDefault(entity => entity.Name == name);
        if (flowEntity == null) throw new Exception($"<UNK>{name}<UNK>");
        InitTaskFlowEntities(flowEntity);

        return flowEntity;
    }

    [BindingCache]
    public GameTaskWorkflow GetTaskWorkflowByEntityName(string name)
    {
        return EntityToFlow(GetTaskWorkflowEntityByName(name));
    }

    private void InitTaskFlowEntities(GameTaskFlowEntity flowEntity)
    {
        if (flowEntity.TaskIds != null && flowEntity.TaskEntities.Count == 0)
        {
            List<List<GameTaskEntity>> taskLists = [];
            foreach (var list in flowEntity.TaskIds)
            {
                List<GameTaskEntity> tasks = [];
                //TODO 这里考虑要不要分散到具体节点上操作
                tasks.AddRange(list.Select(i => SqliteManager.Table<GameTaskEntity>().FirstOrDefault(e => e.Id == i)));
                taskLists.Add(tasks);
            }

            flowEntity.TaskEntities = taskLists;
        }
    }


    [BindingCache]
    public GameTaskWorkflow EntityToFlow(GameTaskFlowEntity flowEntity)
    {
        InitTaskFlowEntities(flowEntity);

        if (flowEntity.TaskArgs != null)
        {
            for (var i = 0; i < flowEntity.TaskArgs.Count; i++)
            {
                for (var j = 0; j < flowEntity.TaskArgs[i].Count; j++)
                {
                    flowEntity.TaskEntities[i][j].OverrideArgs =
                        JsonConvert.DeserializeObject<Dictionary<string, object>>(flowEntity.TaskArgs[i][j])!;
                }
            }
        }


        var workflow = new GameTaskWorkflow
        {
            //这里是默认名称
            Name = $"{flowEntity.Name}",
            SessionId = flowEntity.SessionId,
            TaskEntities = flowEntity.TaskEntities,
            WithSceneName = flowEntity.WithScene
        };

        flowEntity.FlowId = workflow.Id;

        return workflow;
    }

    [BindingCache]
    public GameTaskWorkflow FlowToFlow(GameTaskWorkflow source)
    {
        return new GameTaskWorkflow
        {
            Name = source.Name,
            SessionId = source.SessionId,
            TaskEntities = source.TaskEntities,
        };
    }


    /// <summary>
    /// 获取所有已加载的任务类型, 后续可能考虑外部导入
    /// </summary>
    /// <returns></returns>
    public List<string> ListTasks()
    {
        return [];
    }


    public StepTaskEntity GetStepTaskEntityByName(string name)
    {
        return SqliteManager.Table<StepTaskEntity>().FirstOrDefault(entity => entity.Name == name);
    }

    /// <summary>
    /// 启动一个任务流
    /// </summary>
    /// <param name="taskContext"></param>
    /// <param name="flowEntity"></param>
    /// <param name="commonArgs"></param>
    /// <param name="callFinally"></param>
    /// <param name="onStart"></param>
    /// <returns></returns>
    public GameTaskWorkflow RunTaskWorkflow(GameTaskContext? taskContext, GameTaskFlowEntity flowEntity,
        Dictionary<string, object> commonArgs,
        Callable callFinally, Callable onStart = default)
    {
        var workflow = Services.Get<TaskWorkflowService>()!.EntityToFlow(flowEntity);
        workflow.CommonArgs.AddRange(commonArgs);
        workflow.OnComplete += (s, i) => callFinally.CallDeferred(true);
        workflow.OnError += (s, i) => callFinally.CallDeferred(false);
        workflow.OnBeforeStart += (s, i) => onStart.CallDeferred();
        if (taskContext != null) workflow.Context = taskContext;

        return workflow;
    }

    public GameTaskWorkflow GetTaskWorkflow(string taskEntityName)
    {
        return GetTaskWorkflowByEntityName(taskEntityName);
    }
}