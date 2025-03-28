namespace GodotServiceFramework.Context.Component;

[AttributeUsage(AttributeTargets.Class)]
public class ComponentScanAttribute(Type[]? values = null) : Attribute
{
    public Type[]? Values { get; } = values;

    public ComponentScanAttribute() : this([])
    {
    }

   
}