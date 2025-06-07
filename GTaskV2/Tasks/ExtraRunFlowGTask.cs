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
/// <param name="context"></param>
public class ExtraRunFlowGTask(GTaskModel model, GTaskContext context) : BaseGTask(model, context)
{
    protected override Task<int> Run()
    {
        var name = this.Get("name").ToString()!;
        Log.Info("开始执行额外工作流: " + name);
        var flowEntity = Services.Get<GTaskEntityService>()!.GetFlowEntity(name);
        this.InsertAndRunFlow(flowEntity);
        return Task.FromResult(50);
    }
}