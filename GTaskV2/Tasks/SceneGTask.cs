using GodotServiceFramework.Context.Session;
using GodotServiceFramework.GConsole;
using GodotServiceFramework.GTaskV2.Base;
using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.GTaskV2.Util;

namespace GodotServiceFramework.GTaskV2.Tasks;

public class SceneGTask(GTaskModel model, GTaskContext context) : BaseGTask(model, context)
{
    protected override Task Run()
    {
        var scene = this.Get("flowScene").ToString()!;

        this.EmitSessionSignal(new ConsoleMessage
        {
            Scene = scene,
            Args = Context
        });
        return Task.CompletedTask;
    }
}