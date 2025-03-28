using GodotServiceFramework.Util;

namespace GodotServiceFramework.Context.Service;

[AttributeUsage(AttributeTargets.Class)]
public class OrderAttribute(int index = Constants.DefaultOrderIndex) : Attribute
{
    public int Index { get; } = index;
}