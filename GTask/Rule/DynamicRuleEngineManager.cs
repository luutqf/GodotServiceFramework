using GodotServiceFramework.Context.Service;

namespace GodotServiceFramework.GTask.Rule;

// [InjectService]
public partial class DynamicRuleEngineManager
{
    public readonly DynamicLinqRuleEngine<GameTaskContext> Instance = new();
}