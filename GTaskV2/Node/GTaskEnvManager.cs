using GodotServiceFramework.Context.Service;

namespace SigmusV2.GodotServiceFramework.GTaskV2;

/// <summary>
/// 这个类用于接收,管理各种环境信息, 比如接收某些事件, 并传递给task.
///
/// Timer也会放在这里,作为子节点
/// </summary>
public partial class GTaskEnvManager : AutoGodotService
{
    public override void Init()
    {
    }

    public override void Destroy()
    {
    }
}