using Godot;
using System;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.GConsole;
using GodotServiceFramework.Nodes;
using GodotServiceFramework.Util;

public partial class DefaultSigmusConsole : HBoxContainer
{
    [ChildNode("CommandLineEdit")] private LineEdit _commandLineEdit;

    private SigmusConsole? _sigmusConsole;


    [Autowired]
    public override void _Ready()
    {
        _sigmusConsole = this.GetRoot<SigmusConsole>()!;
        _commandLineEdit.Connect(LineEdit.SignalName.TextSubmitted,
            Callable.From<string>(text =>
            {
                _sigmusConsole?.MessageHandler(text);
                _commandLineEdit.Clear();
            }));
    }
}