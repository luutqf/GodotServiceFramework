using GodotServiceFramework.GTaskV2.Base;
using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.GTaskV2.Util;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GTaskV2.Tasks;

public class DefaultTimerGTask(GTaskModel model, GTaskContext context) : BaseTimerGTask(model, context)
{
    // protected override int Delay => 1;
    //
    // protected override bool AutoFail => false;

    public override void BeforeStart()
    {
        if (this.Get("background") is bool background)
        {
            if (background)
            {
                Log.Info("Background task");
                Progress = TaskBackground;
            }
        }

        base.BeforeStart();
    }

    protected override void OnTimeout()
    {
        Log.Info(this.Get("content"));
    }

    protected override void AfterTask()
    {
        this.InsertAfter("MessageGTask", new Dictionary<string, object> { ["content"] = "!!!!!!!!!!!!!!!!!!" });
    }
}