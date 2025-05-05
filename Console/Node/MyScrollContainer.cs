using Godot;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Util;


namespace GodotServiceFramework.GConsole;

[GlobalClass]
public partial class MyScrollContainer : ScrollContainer
{
    [Export] public bool AutoScrollEnabled = true;

    private bool _allowScroll = true;

    public override void _Ready()
    {
        AddToGroup("SingleScroll");
        Connect(Control.SignalName.MouseEntered, Callable.From(() =>
        {
            var nodesInGroup = GetTree().GetNodesInGroup("SingleScroll");
            foreach (var node in nodesInGroup)
            {
                if (node == this) continue;
                if (node is MyScrollContainer scrollContainer)
                {
                    scrollContainer._allowScroll = false;
                }
            }
        }));
        Connect(Control.SignalName.MouseExited, Callable.From(() =>
        {
            var nodesInGroup = GetTree().GetNodesInGroup("SingleScroll");
            foreach (var node in nodesInGroup)
            {
                if (node == this) continue;
                if (node is MyScrollContainer scrollContainer)
                {
                    scrollContainer._allowScroll = true;
                }
            }
        }));

        Connect(Control.SignalName.GuiInput, Callable.From<InputEvent>(@event =>
        {
            // 检查是否是在当前容器区域内
            var containerRect = GetGlobalRect();

            switch (@event)
            {
                // 处理鼠标滚轮事件
                case InputEventMouseButton mouseEvent when
                    containerRect.HasPoint(mouseEvent.Position):

                // 处理触摸板平移手势事件
                case InputEventPanGesture panEvent when
                    containerRect.HasPoint(panEvent.Position):

                // 处理触摸屏拖动事件
                case InputEventScreenDrag dragEvent when
                    containerRect.HasPoint(dragEvent.Position):
                {
                    if (!_allowScroll)
                    {
                        GetViewport().SetInputAsHandled();
                    }

                    break;
                }
            }
        }));

        //自动滚动到底部
        GetChild(0)?.Connect(Node.SignalName.ChildEnteredTree, Callable.From<Node>((node =>
        {
            if (AutoScrollEnabled)
            {
                // 获取垂直滚动条的最大值并设置
                CallDeferred(nameof(ScrollToBottomDeferred));
            }
        })));
    }

    private async void ScrollToBottomDeferred()
    {
        try
        {
            await Services.NextProcessFrame();
            var maxScroll = GetVScrollBar().MaxValue;
            SetVScroll((int)maxScroll);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }
}