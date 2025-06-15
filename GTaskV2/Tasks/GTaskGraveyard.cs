using GodotServiceFramework.GTaskV2.Base;
using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.GTaskV2.Util;
using GodotServiceFramework.Util;
using SigmusV2.Script.task_v2;

namespace GodotServiceFramework.GTaskV2.Tasks;

/// <summary>
/// 任务坟场, 针对一个工作流, 在初始化时自动插入, 任务流中所有任务结束后, 填满坟场, 此任务才算结束.
/// </summary>
/// <param name="model"></param>
/// <param name="context"></param>
public class GTaskGraveyard(GTaskModel model, GTaskContext context) : BaseTimerGTask(model, context)
{
    private string _flowName = null!;

    //无限运行
    protected override bool Infinite => true;

    public override void BeforeStart()
    {
        base.BeforeStart();
        if (!this.TryGet("flowName", out var flowName))
            throw new Exception("flowName is not set");
        _flowName = flowName!.ToString()!;
    }

    protected override Task OnTimeout()
    {
        var flow = Context.GetStartTasks(_flowName);

        // !flow.All(task => task.Progress >= 100)) return Task.CompletedTask;
        if (flow.Any(task => task.Progress < 100)) return Task.CompletedTask;


        if (!flow.All(ChainComplete)) return Task.CompletedTask;

        foreach (var task in flow)
        {
            Log.Info(task.GetTitle());
            // ChainComplete(task)
        }

        Log.Info(Model.NextModels.Length > 0 ? $"{_flowName} 任务流已全部完成,还有其他后续任务" : $"{_flowName} 任务流已全部完成");

        Log.Info("====================================");
        Log.Info($"{Context.Id}");
        GTaskContext.Contexts.TryAdd(Context.Id, Context);
        Complete();
        foreach (var baseGTask in flow)
        {
            if (baseGTask.Progress > 100)
            {
                baseGTask.Stop();
            }
        }

        return Task.CompletedTask;
    }


    private bool ChainComplete(BaseGTask task)
    {
        if (task.Progress < 100)
        {
            return false;
        }

        return task.NextTasks.Count switch
        {
            0 when task.Model.NextModels.Length == 0 => true,
            0 when task.Model.NextModels.Length != 0 => false,
            _ => task.NextTasks.All(ChainComplete)
        };
    }

    protected override void Complete()
    {
        base.Complete();
    }
}