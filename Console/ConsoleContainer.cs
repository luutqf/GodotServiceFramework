using Godot;
using System;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Nodes;

namespace GodotServiceFramework.GConsole;

public partial class ConsoleContainer : Control
{
    [ChildNode("CommandLineEdit")] private LineEdit? _commandLineEdit;

    [ChildNode("ConsoleTabContainer")] public TabContainer? ConsoleTabContainer;

    [ChildNode("ConsoleTitleContainer")] public HBoxContainer? ConsoleTitleContainer;


    private int _titleIndex;

    void SetCaretColumn(int pos)
    {
        _commandLineEdit!.CallDeferred(Control.MethodName.GrabFocus);
        _commandLineEdit.CallDeferred(GodotObject.MethodName.Set, LineEdit.PropertyName.CaretColumn, pos + 1);
    }

    [Autowired]
    public override void _Ready()
    {
        _commandLineEdit!.Connect(LineEdit.SignalName.TextSubmitted, Callable.From<string>(text =>
        {
            if (ConsoleTabContainer!.GetCurrentTabControl() is not SigmusConsole console) return;

            console.MessageHandler(text);
            _commandLineEdit.Clear();

            SetCaretColumn(0);
        }));

        _commandLineEdit.Connect(LineEdit.SignalName.TextChanged, Callable.From<string>(text =>
        {
            if (ConsoleTabContainer!.GetCurrentTabControl() is not SigmusConsole console) return;

            console.ResetSearch(text);
        }));

        _commandLineEdit.Connect(Control.SignalName.GuiInput, Callable.From<InputEvent>(@event =>
        {
            if (ConsoleTabContainer!.GetCurrentTabControl() is not SigmusConsole console) return;

            if (@event is InputEventKey eventKey && eventKey.IsPressed())
            {
                switch (eventKey.Keycode)
                {
                    case Key.Up:
                        _commandLineEdit.Text = console.SearchMessage(_commandLineEdit.Text, 1);
                        break;
                    case Key.Down:
                        _commandLineEdit.Text = console.SearchMessage(_commandLineEdit.Text, -1);
                        break;
                }

                SetCaretColumn(_commandLineEdit.Text.Length);
            }
        }));

        SetCaretColumn(0);

        //初始化consoleTitle
        foreach (var child in ConsoleTabContainer!.GetChildren())
        {
            if (child is SigmusConsole sigmusConsole)
            {
                var consoleTitleButton =
                    Services.Get<SceneStatsManager>()!.InstantiateScene<ConsoleTitleButton>(readyCallBack: @this =>
                    {
                        @this.Init(sigmusConsole, _titleIndex++);
                    });
                ConsoleTitleContainer!.AddChild(consoleTitleButton);
            }
        }
    }

    /// <summary>
    /// 打开一个会话,如果已经是当前会话, 则返回. 
    /// </summary>
    /// <param name="sigmusConsole"></param>
    public void OpenSigmusConsole(SigmusConsole sigmusConsole)
    {
        var currentTabControl = ConsoleTabContainer!.GetCurrentTabControl();
        if (currentTabControl == sigmusConsole)
        {
            return;
        }

        if (ConsoleTabContainer.GetChildren().Contains(sigmusConsole))
        {
            ConsoleTabContainer.CurrentTab = sigmusConsole.GetIndex();
        }

        _commandLineEdit!.Text = string.Empty;
    }

    /// <summary>
    /// 当控制台会话按钮按下时,打开它
    /// </summary>
    private void _on_add_console_button_pressed()
    {
        var console = Services.Get<SceneStatsManager>()!.InstantiateScene<SigmusConsole>();
        var title = Services.Get<SceneStatsManager>()!.InstantiateScene<ConsoleTitleButton>(readyCallBack: button =>
        {
            button.Init(console, _titleIndex++);
        });
        ConsoleTabContainer!.AddChild(console);
        ConsoleTabContainer.CurrentTab = console.GetIndex();

        ConsoleTitleContainer!.AddChild(title);
        _commandLineEdit!.Text = string.Empty;
    }

    /// <summary>
    /// 关闭一个会话,如果只剩下一个会话, 则无法关闭
    /// </summary>
    /// <param name="sigmusConsole"></param>
    /// <returns></returns>
    public bool CloseSigmusConsole(SigmusConsole sigmusConsole)
    {
        if (ConsoleTabContainer!.GetChildCount() == 1) return false;
        foreach (var child in ConsoleTabContainer.GetChildren())
        {
            if (child == sigmusConsole)
            {
                ConsoleTabContainer.RemoveChild(sigmusConsole);
                sigmusConsole.QueueFree();
            }
        }

        return true;
    }
}