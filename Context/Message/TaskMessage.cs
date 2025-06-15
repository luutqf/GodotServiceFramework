using Godot;

namespace GodotServiceFramework.Context;

public partial class TaskMessage : RefCounted
{
    public string Content { get; set; } = string.Empty;

    public string TaskTitle { get; set; } = string.Empty;
}