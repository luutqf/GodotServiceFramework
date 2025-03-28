using Godot;
using GodotServiceFramework.Nodes;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GConsole;

public partial class TextLineControl : VBoxContainer
{
    [ChildNode("TextLine")] private MyRichTextLabel? _textLine;

    [ChildNode("ButtonBox")] private HBoxContainer? _buttonBox;

    [ChildNode("VBox")] private VBoxContainer? _vBox;

    [ChildNode("Avatar")] private TextureRect? _avatar;

    [ChildNode("ButtonScrollContainer")] private ScrollContainer? _buttonScrollContainer;


    [Autowired]
    public override void _Ready()
    {
        _avatar!.Visible = false;
    }

    public void OverrideText(ConsoleMessage message)
    {
        try
        {
            _textLine!.SetText(message.Text, message.Speed, message.AutoClear);
        }
        catch (ObjectDisposedException)
        {
            //TODO 这里先不处理, 后面再说
            return;
        }
    }

    public void Init(ConsoleMessage message)
    {
        // var array = args.AsGodotArray<Dictionary>();
        _textLine!.SetText(message.Text, message.Speed, message.AutoClear, message.ClearDelay);
        _buttonBox!.RemoveAllChild();
        _avatar!.SetCustomMinimumSize(Vector2.Zero);


        // array?.ForEach(tuple => _buttonBox.AddChild(new Button
        // {
        //     Text = tuple["show"].AsString(),
        // }));

        if (_buttonBox!.GetChildren().Count > 0)
        {
            _buttonScrollContainer!.Visible = true;
            _buttonScrollContainer.CustomMinimumSize = new Vector2(0, 32);
        }
        else
        {
            _buttonScrollContainer!.Visible = false;
            _buttonScrollContainer.CustomMinimumSize = Vector2.Zero;

            _buttonScrollContainer.SetSize(Vector2.Zero);
            SetCustomMinimumSize(new Vector2(0, 20));
            // SetSize(_textLine.Size);
        }
    }

    public void SetCharacter(string name)
    {
        _avatar!.Visible = true;
        _avatar.SetCustomMinimumSize(new Vector2(50, 50));
    }


    public void Close()
    {
        GetParent().RemoveChild(this);
        QueueFree();
    }
}