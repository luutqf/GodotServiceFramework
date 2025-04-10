using Godot;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Data;
using GodotServiceFramework.Nodes;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GConsole;

[GlobalClass]
public partial class MessageBox : VBoxContainer
{
    [Export] public int MaxSize { get; set; } = 100;

    private readonly Dictionary<string, TextLineControl?> _textLines = [];

    public override void _Ready()
    {
        Connect(Node.SignalName.ChildEnteredTree, Callable.From<Node>(node =>
        {
            if (GetChildCount() <= MaxSize) return;

            for (var i = 0; i < MaxSize / 5; i++)
            {
                var child = GetChild(i);
                RemoveChild(child);
            }
        }));
    }

    public void AddMessage(ConsoleMessage message)
    {
        if (GetParent() is MyScrollContainer scrollContainer)
        {
            scrollContainer.CallDeferred(MyScrollContainer.MethodName.ScrollToBottomDeferred);
        }

        TextLineControl? line = null;
        if (message.Level <= 1)
        {
            if (!string.IsNullOrEmpty(message.MessageId))
            {
                _textLines.TryGetValue(message.MessageId, out line);
                try
                {
                    line?.GetName();
                }
                catch (ObjectDisposedException)
                {
                    //TODO 这里要优化流程🤔
                    line = null;
                }
            }
        }


        switch (message.Type)
        {
            case "text":
            {
                // Logger.Info(message.Text);

                if (line == null)
                {
                    line = Services.Get<SceneStatsManager>()!.InstantiateScene<TextLineControl>(readyCallBack: @this =>
                    {
                        @this.Init(message);
                    });
                    AddChild(line);
                }
                else
                {
                    line.OverrideText(message);
                }

                break;
            }
            case "scene":
            {
                Logger.Info($"打开场景: {message.Scene}");

                //如果场景已存在, 则
                var children = GetChildren();
                foreach (var child in children)
                {
                    if (child.GetName().ToString() != message.Scene) continue;

                    if (child is IDataNode { IsSingleScene: false })
                    {
                        continue;
                    }

                    MoveChild(child, children.Count - 1);
                    return;
                }

                var instantiate = Services.Get<SceneStatsManager>()!.InstantiateScene(message.Scene, message.Args);

                AddChild(instantiate);
                break;
            }
            case "chat":
            {
                if (line == null)
                {
                    line = Services.Get<SceneStatsManager>()!.InstantiateScene<TextLineControl>(readyCallBack: @this =>
                    {
                        @this.Init(message);
                        @this.SetCharacter(message.User);
                    });
                    AddChild(line);
                }
                else
                {
                    line.OverrideText(message);
                }

                break;
            }
        }

        if (line == null || string.IsNullOrEmpty(message.MessageId)) return;

        _textLines[message.MessageId] = line;
        line.SetMeta("message", message.MessageId);
    }
}