using GodotServiceFramework.Util;

namespace GodotServiceFramework.GTask;

public class DelayTask(GameTaskWorkflow gameTaskWorkflow, int[] index, Dictionary<string, object> args)
    : GameTask(gameTaskWorkflow, index, args)
{
    public override string Name => "DelayTask";


    public override void Init()
    {
        Log.Debug("DelayTask hooked");
    }

    public override void Destroy()
    {
        Log.Debug("DelayTask unhooked");
    }

    public override async Task<int> Start()
    {
        Log.Info($"DelayTask started : {Args["sleep"]}");
        // if (Args["sleep"] is not int i) return -1;
        var i = int.Parse(Args["sleep"].ToString()!);

        var delta = 100 / (float)i;

        for (var j = 1; j < i; j++)
        {
            await Task.Delay(1000);
            Progress = (int)(j * delta);
            // this.EmitSessionSignal("SuperChat")
        }

        Log.Debug("DelayTask sleep");

        // Thread.Sleep((int)(1000 * i));
        return 100;
    }
}