using GodotServiceFramework.GTaskV2.Base;
using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GTaskV2.Tasks;

public class MessageGTask(GTaskModel model, GTaskFlow flow) : BaseGTask(model, flow)
{
    protected override async Task<int> Run()
    {
        Log.Info(Parameters.GetValueOrDefault("content", "GTask Message"));

        return 100;
    }
}