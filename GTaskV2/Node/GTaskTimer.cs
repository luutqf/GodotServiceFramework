using Timer = Godot.Timer;

namespace SigmusV2.GodotServiceFramework.GTaskV2.Nodes;

/// <summary>
/// 
/// </summary>
public partial class GTaskTimer(GTaskTemplate taskTemplate) : Timer
{
    public GTaskTemplate TaskTemplate { get; set; } = taskTemplate;
}