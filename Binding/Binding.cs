
namespace GodotServiceFramework.Binding;

/// <summary>
/// 用于具体字段的数据绑定, 但还是倾向于在初始化方法中统一处理
/// </summary>
/// <param name="name"></param>
/// <param name="type"></param>
[Obsolete("暂时没啥用")]
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