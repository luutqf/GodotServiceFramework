using System;

namespace GodotServiceFramework.Mapper;

/// <summary>
/// 默认的mapper是字符串, 还得添加其他的, 比如IntMapper, 用于强制转换.
/// </summary>
/// <param name="name"></param>
public class MapperAttribute(string name = "") : Attribute
{
    //这里是对应对象属性名
    public string Name { get; } = name;
}