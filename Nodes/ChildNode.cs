using System;
using AspectInjector.Broker;

namespace GodotServiceFramework.Nodes;

[AttributeUsage(AttributeTargets.Field)]
public class ChildNodeAttribute(string name = "") : Attribute
{
    public string Name { get; } = name;

    public ChildNodeAttribute() : this("")
    {
    }
}