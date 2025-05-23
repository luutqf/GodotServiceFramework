using GodotServiceFramework.Context.Service;

namespace GodotServiceFramework.GTask.Rule;

public partial class DynamicRuleEngineManager : AutoGodotService
{
    public readonly DynamicLinqRuleEngine<GameTaskContext> Instance = new();


}