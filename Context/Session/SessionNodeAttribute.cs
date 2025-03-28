using AspectInjector.Broker;
using Godot;

namespace GodotServiceFramework.Context.Session;

/// <summary>
/// 这个用于标记sessionNode, 用于向不同会话发送信号
/// </summary>
///
[AttributeUsage(AttributeTargets.All)]
[Aspect(Scope.Global)]
[Injection(typeof(SessionNodeAttribute))]
public class SessionNodeAttribute : Attribute
{
    [Advice(Kind.Before)]
    public void Before([Argument(Source.Triggers)] Attribute[] attributes,
        [Argument(Source.Type)] Type hostType, [Argument(Source.Instance)] object instance,
        [Argument(Source.Name)] string name)
    {
        if (instance is Node node)
        {
            SessionManager.SessionIdMap[node.GetInstanceId()] = node.GetInstanceId();
            SessionManager.MainSessionIds.Add(node.GetInstanceId());
        }
    }
}