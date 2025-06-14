using GodotServiceFramework.GTaskV2.Base;
using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GTaskV2.Tasks;

public class MessageGTask(GTaskModel model, GTaskContext context) : BaseGTask(model, context)
{
    protected override async Task<int> Run()
    {
        Log.Info(Parameters.GetValueOrDefault("content", "GTask Message"));

        return 100;
    }
}