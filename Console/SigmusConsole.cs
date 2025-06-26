using Godot;
using GodotServiceFramework.Context.Controller;
using GodotServiceFramework.Context.Session;
using GodotServiceFramework.Nodes;
using SigmusV2.Script.utils;


namespace GodotServiceFramework.GConsole;

/// <summary>
/// 
/// </summary>
[GlobalClass]
public partial class SigmusConsole : Control
{
    [ChildNode("MessageBox")] private MessageBox? _messageBox;


    [Signal]
    public delegate void SuperChatEventHandler(ConsoleMessage message);


    [SessionNode]
    [Autowired]
    public override void _Ready()
    {
        Connect(SignalName.SuperChat, Callable.From<ConsoleMessage>(_messageBox!.AddMessage));
    }

    public void MessageHandler(string text)
    {
        if (text.Equals("clear"))
        {
            Clear();
            return;
        }

        _history.Enqueue(text);

        //2. 检查是否是别名
        if (Controllers.HasAlias(text))
        {
            this.InvokeController(text);
            return;
        }


        //4. 再检查指令
        if (text.Contains('.'))
        {
            var strings = text.Split('.');
            if (strings.Length != 2) return;
            this.InvokeController(strings[0], strings[1]);
        }
        else if (text.Contains(','))
        {
        }
        else if (text.Contains(':'))
        {
            var strings = text.Split(':');
            if (strings.Length != 2) return;


            _messageBox!.AddMessage(new ConsoleMessage()
            {
                Text = strings[1],
                User = strings[0],
            });
        }
        else
        {
            _messageBox!.AddMessage(new ConsoleMessage()
            {
                Text = text,
            });
        }
    }

    public void Clear()
    {
        _messageBox?.Clear();
    }


    #region 输入历史

    private readonly BoundedQueue<string> _history = new(100);

    private string _searchText = string.Empty;

    private int _searchIndex;

    public void ResetSearch(string text)
    {
        _searchText = text;
        _searchIndex = 0;
    }

    public string SearchMessage(string text, int i)
    {
        _searchIndex += i;

        if (_searchIndex <= 0)
        {
            _searchIndex = 0;
            return _searchText;
        }

        var hints = string.IsNullOrEmpty(_searchText)
            ? _history.ToArray()
            : _history.Where(s => s.StartsWith(_searchText)).ToArray();

        if (hints.Length == 0)
        {
            return text;
        }

        if (hints.Length < _searchIndex)
        {
            _searchIndex = hints.Length;
        }


        var hintsLength = hints.Length - (_searchIndex);
        return hints[hintsLength];


        // try
        // {
        // }
        // catch (Exception e)
        // {
        //     _searchIndex = 0;
        //     _searchText = string.Empty;
        //     Logger.Error(e);
        //     return text;
        // }
    }

    #endregion
}