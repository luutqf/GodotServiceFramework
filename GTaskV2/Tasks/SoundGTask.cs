using GodotServiceFramework.GTaskV2.Base;
using GodotServiceFramework.GTaskV2.Model;

namespace GodotServiceFramework.GTaskV2.Tasks;

public class SoundGTask(GTaskModel model, GTaskFlow flow) : BaseGTask(model, flow)
{
    protected override async Task<int> Run()
    {
        return 100;
    }
}