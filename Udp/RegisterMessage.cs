using AspectInjector.Broker;
using GodotServiceFramework.Proto;
using System;
using GodotServiceFramework.Context;
using GodotServiceFramework.Context.Service;

namespace GodotServiceFramework.Udp;

[AttributeUsage(AttributeTargets.Class)]
[Aspect(Scope.Global)]
[Injection(typeof(ServiceAttribute))]
public class RegisterMessageAttribute(Action<ServerResp>[] handlers) : Attribute
{
    private Action<ServerResp>[] Handlers { get; } = handlers;

    public RegisterMessageAttribute() : this([])
    {
    }

    [Advice(Kind.After)]
    public void After([Argument(Source.Name)] string name, [Argument(Source.Type)] Type hostType)
    {
        
        
    }

}