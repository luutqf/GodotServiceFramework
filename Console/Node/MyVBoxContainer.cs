using Godot;

namespace GodotServiceFramework.GConsole;

public partial class MyVBoxContainer : VBoxContainer
{
    [Export] public int MaxSize { get; set; } = 100;


    public override void _Ready()
    {
        Connect(Node.SignalName.ChildEnteredTree, Callable.From<Node>(node =>
        {
            if (GetChildCount() <= MaxSize) return;

            for (var i = 0; i < MaxSize / 5; i++)
            {
                var child = GetChild(i);
                RemoveChild(child);
                child.QueueFree();
            }
        }));
    }
}