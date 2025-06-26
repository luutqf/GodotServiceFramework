using GodotServiceFramework.GTaskV2.Base;
using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.GTaskV2.Util;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GTaskV2.Tasks;

/// <summary>
/// 任务坟场, 针对一个工作流, 在初始化时自动插入, 任务流中所有任务结束后, 填满坟场, 此任务才算结束.
/// </summary>
/// <param name="model"></param>
public class GTaskGraveyard(GTaskModel model, GTaskFlow flow) : BaseTimerGTask(model, flow)
{
    // private string _flowName = null!;

    //无限运行
    protected override bool Infinite => true;


    protected override Task OnTimeout()
    {
        var flow = Flow!.StartTasks;

        if (flow.Any(task => task.Progress < 100) ||
            !flow.All(ChainComplete)) return Task.CompletedTask;


        foreach (var task in flow)
        {
            Log.Debug(task.GetTitle());
            // ChainComplete(task)
        }

        Log.Info(Model.NextModels.Length > 0 ? $"{Flow.Name} 任务流已全部完成,还有其他后续任务" : $"{Flow.Name} 任务流已全部完成",
            BbColor.Aqua);

        Log.Debug("====================================");
        Log.Debug($"{Context.Id}");
        GTaskContext.Contexts.TryAdd(Context.Id, Context);
        Complete();
        foreach (var baseGTask in flow)
        {
            if (baseGTask.Progress > 100)
            {
                baseGTask.Stop();
            }
        }

        Flow.Stop();
        return Task.CompletedTask;
    }


    /// <summary>
    /// 检查任务链的下一个节点是否全部完成
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
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
}