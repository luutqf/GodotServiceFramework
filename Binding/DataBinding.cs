namespace GodotServiceFramework.Binding;

public enum DataModifyType
{
    Insert,
    Update,
    Delete,
    Property
}

public interface IBinding
{
    public int Id { get; set; }
    public string Name { get; set; }
}