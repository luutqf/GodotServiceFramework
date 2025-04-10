using Godot;
using System;
using GodotServiceFramework.Nodes;

namespace GodotServiceFramework.GConsole;

[Tool]
[GlobalClass]
public partial class FWindow : Control
{
    [ChildNode("CloseButton")] private Button? _closeButton;
    [ChildNode("NameLabel")] private Label? _nameLabel;
    [ChildNode("Background")] private Panel? _background;

    [Export] private string Title { get; set; } = "";
    [Export] private float MiniY { get; set; } = 300;

    [Autowired]
    public override void _Ready()
    {
        _background!.SetCustomMinimumSize(new Vector2(0, MiniY));
        _closeButton!.Connect(BaseButton.SignalName.Pressed,
            Callable.From(() =>
            {
                GetParent().GetParent().RemoveChild(GetParent());
            }));

        _nameLabel!.SetText(Title);
    }
}