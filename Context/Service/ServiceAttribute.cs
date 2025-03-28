using AspectInjector.Broker;
using Godot;

namespace GodotServiceFramework.Context.Service;

[AttributeUsage(AttributeTargets.All)]
[Aspect(Scope.Global)]
[Injection(typeof(ServiceAttribute))]
public class ServiceAttribute(bool persistent = false) : Attribute
{
    private bool Persistent { get; } = persistent;

    public ServiceAttribute() : this(false)
    {
    }

    [Advice(Kind.Before)]
    public void Before([Argument(Source.Triggers)] Attribute[] attributes,
        [Argument(Source.Type)] Type hostType, [Argument(Source.Instance)] object instance,
        [Argument(Source.Name)] string name)
    {
        if (name.Equals("_Process") || name.Equals("_PhysicsProcess")) return;

        if (attributes.First(v => v is ServiceAttribute) is not ServiceAttribute attribute) return;

        if (instance is not GodotObject godotObject) return;


        Services.Add(godotObject);
        Console.WriteLine($"service register::::{hostType} ");
    }
}