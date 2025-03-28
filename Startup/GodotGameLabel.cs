namespace GodotServiceFramework;

[AttributeUsage(AttributeTargets.Assembly)]
public class GodotGameAttribute(string label) : Attribute
{
    public string Label { get; } = label;
}