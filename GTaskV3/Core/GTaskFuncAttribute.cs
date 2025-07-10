namespace GodotServiceFramework.GTaskV3;

[AttributeUsage(AttributeTargets.All)]
public class GTaskFuncAttribute(string name = "") : Attribute
{
    public string Name { get; } = name;
}