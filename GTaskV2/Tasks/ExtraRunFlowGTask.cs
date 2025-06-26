using GodotServiceFramework.Context.Service;
using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.GTaskV2.Service;
using GodotServiceFramework.GTaskV2.Util;
using GodotServiceFramework.Util;
using Newtonsoft.Json.Linq;

namespace GodotServiceFramework.GTaskV2.Tasks;

/// <summary>
/// 启动一个额外的工作流, 工作流必须在数据库中存在
/// </summary>
/// <param name="model"></param>
public class ExtraRunFlowGTask(GTaskModel model, GTaskFlow flow) : BaseGTask(model, flow)
{
    private readonly Dictionary<string, int> _flowsProgress = [];

    protected override Task<int> Run()
    {
        if (this.TryGetArg("names", out var value))
        {
            var array = (JArray)value!;
            foreach (var jToken in array)
            {
                _flowsProgress[jToken.ToString()] = 0;
            }
        }

        if (this.TryGetArg("name", out value))
        {
            _flowsProgress[value!.ToString()!] = 0;
        }
        // var array = (JArray)this.Get("names");

        foreach (var kv in _flowsProgress)
        {
            Log.Info("开始执行额外工作流: " + kv.Key);
            var flowEntity = Services.Get<GTaskEntityService>()!.GetFlowEntity(kv.Key);
            this.InsertAndRunFlow(flowEntity, flow =>
            {
                Log.Info($"额外工作流已完成: {flow.Name}");

                if (_flowsProgress.ContainsKey(flow.Name))
                {
                    _flowsProgress[flow.Name] = 100;
                }

                if (_flowsProgress.All(pair => pair.Value == 100))
                {
                    Progress = 100;
                }
            });
        }

        return Task.FromResult(50);
    }
}