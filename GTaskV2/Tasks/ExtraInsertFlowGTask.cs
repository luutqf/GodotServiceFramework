using GodotServiceFramework.Context.Service;
using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.GTaskV2.Service;
using GodotServiceFramework.GTaskV2.Util;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GTaskV2.Tasks;

/// <summary>
/// 启动一个额外的工作流, 工作流必须在数据库中存在
/// </summary>
/// <param name="model"></param>
public class ExtraInsertFlowGTask(GTaskModel model, GTaskFlow flow) : BaseGTask(model, flow)
{
    protected override Task<int> Run()
    {
        this.GetNextTasks();
        var name = this.GetArg("name").ToString()!;
        Log.Info("向后插入额外工作流: " + name);

        var flowEntity = Services.Get<GTaskEntityService>()!.GetFlowEntity(name);
        this.InsertAfterFlow(flowEntity);

        return Task.FromResult(100);
    }
}