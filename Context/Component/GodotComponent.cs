namespace GodotServiceFramework.Context.Component;


/// <summary>
/// 标注了这个注解的类,会自动注册到Service中.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class GodotComponentAttribute : Attribute
{
}