namespace GodotServiceFramework.Nodes;

[AttributeUsage(AttributeTargets.Field)]
public class LoadPackAttribute(string path = "") : Attribute
{
    public string Path { get; } = path;
}