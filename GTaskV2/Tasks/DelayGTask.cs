using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GTaskV2.Tasks;

public class DelayGTask(GTaskModel model, GTaskContext context) : BaseGTask(model, context)
{
    protected override async Task Run()
    {
        if (Parameters.TryGetValue("delay", out var value))
        {
            if (value is int or long or float or double or decimal)
            {
                Log.Info($"Starting delay task -> {value}");

                await Task.Delay(Convert.ToInt32(value) * 1000);
            }
        }

        Log.Info("Delay task finished");
        Complete();
    }
}