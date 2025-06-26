using GodotServiceFramework.GTaskV2.Base;
using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.GTaskV2.Util;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GTaskV2.Tasks;

public class DefaultTimerGTask(GTaskModel model, GTaskFlow flow) : BaseTimerGTask(model, flow)
{
    public override void BeforeStart()
    {
        if (this.GetArg("background") is bool background)
        {
            if (background)
            {
                Log.Info("Background task");
                Progress = TaskBackground;
            }
        }

        base.BeforeStart();
    }

    protected override Task OnTimeout()
    {
        this.InsertAndRun(nameof(MessageGTask),
            new Dictionary<string, object> { ["content"] = this.GetArg("content") + "123" });
        return Task.CompletedTask;
    }

    protected override void AfterTask()
    {
        this.InsertAfter(nameof(MessageGTask), new Dictionary<string, object> { ["content"] = "!!!!!!!!!!!!!!!!!!" });
    }
}