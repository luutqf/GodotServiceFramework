using GodotServiceFramework.GTaskV2.Base;
using GodotServiceFramework.GTaskV2.Model;

namespace GodotServiceFramework.GTaskV2.Tasks;

public class SoundGTask(GTaskModel model, GTaskContext context) : BaseGTask(model, context)
{
    protected override async Task<int> Run()
    {
        return 100;
    }
}