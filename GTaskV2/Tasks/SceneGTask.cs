using GodotServiceFramework.Context.Session;
using GodotServiceFramework.GConsole;
using GodotServiceFramework.GTaskV2.Base;
using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.GTaskV2.Util;

namespace GodotServiceFramework.GTaskV2.Tasks;

public class SceneGTask(GTaskModel model, GTaskFlow flow) : BaseGTask(model, flow)
{
    protected override async Task<int> Run()
    {
        var scene = this.GetArg("flowScene").ToString()!;

        this.EmitSessionSignal(new ConsoleMessage
        {
            Scene = scene,
            Args = Context
        });
        return 100;
    }
}