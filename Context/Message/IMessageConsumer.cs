using Godot;

namespace SigmusV2.GodotServiceFramework.Context;

/// <summary>
/// 每个消费者必须实现这个接口,同时使用下面的注解属性才能完成注册
/// </summary>
public interface IMessageConsumer
{
    public void ReceiveMessage(Variant message)
    {
    }

}

[AttributeUsage(AttributeTargets.Class)]
public class ChannelAttribute(string[] channels, string[]? tags = null) : Attribute
{
    public readonly string[] Channels = channels;

    public string[]? Tags = tags;
}