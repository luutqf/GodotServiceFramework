using Godot;
using GodotServiceFramework.Nodes;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GConsole;

public partial class ConsoleTitleButton : HBoxContainer
{
    public SigmusConsole? SigmusConsole { get; set; }


    [ChildNode("ConsoleTitle")] private Button? _consoleTitleButton;
    [ChildNode("CloseButton")] private Button? _closeButton;

    [Autowired]
    public override void _Ready()
    {
        Visible = true;

        _consoleTitleButton?.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
        {
            if (SigmusConsole != null)
                this.GetRoot<ConsoleContainer>()!.OpenSigmusConsole(SigmusConsole);
            else
            {
                Log.Info("还没设置Console");
            }
        }));

        _closeButton?.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
        {
            if (SigmusConsole != null)
            {
                if (this.GetRoot<ConsoleContainer>()!.CloseSigmusConsole(SigmusConsole))
                {
                    GetParent().RemoveChild(this);
                    CallDeferred(Node.MethodName.QueueFree);
                }
            }
        }));
    }

    public void Init(SigmusConsole sigmusConsole, int index)
    {
        SigmusConsole = sigmusConsole;
        _consoleTitleButton!.Text = sigmusConsole.GetType().Name + " " + index;
    }
}