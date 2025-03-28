using AspectInjector.Broker;

namespace GodotServiceFramework.Context.Component;

[AttributeUsage(AttributeTargets.Class)]
[Aspect(Scope.Global)]
[Injection(typeof(AsyncComponentScanAttribute))]
public class AsyncComponentScanAttribute(Type[]? values = null) : Attribute
{
    public Type[]? Values { get; } = values;

    public AsyncComponentScanAttribute() : this([])
    {
    }

    [Advice(Kind.Before)]
    public void Before([Argument(Source.Triggers)] Attribute[] attributes,
        [Argument(Source.Type)] Type hostType, [Argument(Source.Instance)] object target,
        [Argument(Source.Name)] string name)
    {
    }

}