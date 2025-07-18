using System.Reflection;
using Godot;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.GTask;

namespace GodotServiceFramework.Context;

/// <summary>
/// 我们在游戏内部, 设计一个消息总线.  服务于所有node和task.
///
/// 对于node, 如果标识了特定的注解属性, 包含队列类型和消息标签即可参与路由, 消费方式的话, 有若干固定方式, 通过在属性中声明,
/// 或者默认类型, 即通过接口.
///
/// 固定方式:  销毁, 播放动画, 播放声音
/// </summary>
[InjectService]
public partial class GameMessageBus : IDisposable
{
    private static readonly Dictionary<string, HashSet<WeakReference<IMessageConsumer>>> WeakChannelMap = [];

    private static readonly Queue<(string, WeakReference<IMessageConsumer>)> ClearChannelQueue = [];

    private static readonly HashSet<string> NodeGroupSet = [];


    /// <summary>
    /// 推送一条消息,包含通道路由和消息体
    /// </summary>
    /// <param name="channel">也可能是group</param>
    /// <param name="message"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static void Push(string channel, Variant message)
    {
        if (NodeGroupSet.Contains(channel))
        {
            foreach (var node in Services.GetSceneTree()!.GetNodesInGroup(channel)
                         .Where(node => node is IMessageConsumer).Cast<IMessageConsumer>())
            {
                node?.ReceiveMessage(message);
            }
        }

        if (WeakChannelMap.TryGetValue(channel, out var ids))
        {
            foreach (var reference in ids)
            {
                if (reference.TryGetTarget(out var target))
                {
                    Task.Run(() => target.ReceiveMessage(message));
                }
                else
                {
                    ClearChannelQueue.Enqueue((channel, reference));
                }
            }
        }


        while (ClearChannelQueue.Count > 0)
        {
            var (name, reference) = ClearChannelQueue.Dequeue();
            if (WeakChannelMap.TryGetValue(name, out var set))
            {
                set.Remove(reference);
            }
        }
    }

    /// <summary>
    /// 注册一个消费者
    /// </summary>
    /// <param name="obj"></param>
    public static void Register(object obj)
    {
        if (obj is not IMessageConsumer consumer)
        {
            return;
        }

        if (obj.GetType().GetCustomAttributes()
            .All(attribute => attribute.GetType() != typeof(ChannelAttribute))) return;

        var attribute = obj.GetType().GetCustomAttribute<ChannelAttribute>()!;

        foreach (var channel in attribute.Channels)
        {
            if (!WeakChannelMap.TryGetValue(channel, out var value))
            {
                value = [];
                WeakChannelMap[channel] = value;
            }

            value.Add(new WeakReference<IMessageConsumer>(consumer));
        }


        if (obj is Node node)
        {
            foreach (var group in node.GetGroups())
            {
                NodeGroupSet.Add(group.ToString());
            }
        }
    }


    public GameMessageBus()
    {
        if (Engine.GetMainLoop() is SceneTree sceneTree)
            sceneTree.NodeAdded += Register;

        if (Services.Has(nameof(GameTaskFactory)))
            Services.Get<GameTaskFactory>()!.OnTaskAdded += Register;
    }


    public void Dispose()
    {
        if (Engine.GetMainLoop() is SceneTree sceneTree)
            sceneTree.NodeAdded -= Register;

        if (Services.Has(nameof(GameTaskFactory)))
            Services.Get<GameTaskFactory>()!.OnTaskAdded -= Register;
    }
}