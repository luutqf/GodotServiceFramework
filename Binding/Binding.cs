
namespace GodotServiceFramework.Binding;

[AttributeUsage(AttributeTargets.All)]
public class BindingAttribute(string name, DataType type = DataType.String) : Attribute
{
    public string Name { get; } = name;

    public DataType Type { get; } = type;

    public BindingAttribute() : this("", DataType.String)
    {
    }
}

public enum DataType
{
    Int,
    Float,
    String
}