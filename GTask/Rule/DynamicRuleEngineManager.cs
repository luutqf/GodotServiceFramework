using GodotServiceFramework.Context.Service;

namespace GodotServiceFramework.GTask.Rule;

public partial class DynamicRuleEngineManager : AutoGodotService
{
    public DynamicLinqRuleEngine<GameTaskContext> Instance = null!;

    public override void Init()
    {
        Instance = new DynamicLinqRuleEngine<GameTaskContext>();
    }

    public override void Destroy()
    {
    }
}