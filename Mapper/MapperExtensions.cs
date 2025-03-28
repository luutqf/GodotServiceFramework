using System.Reflection;
using Godot;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.Mapper;

public static class MapperExtensions
{
    /// <summary>
    /// 设想是node to entity， TODO 还需要完善
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T NodeToEntity<T>(this Node source)
    {
        var runtimeFields = source.GetType().GetRuntimeFields();
        var targetType = typeof(T);
        var instance = (T)Activator.CreateInstance(targetType)!;

        foreach (var runtimeField in runtimeFields)
        {
            var mapper = runtimeField.GetCustomAttribute<MapperAttribute>();
            if (mapper == null) continue;

            var propertyInfo = targetType.GetRuntimeProperty(mapper.Name);
            if (propertyInfo == null) continue;


            var value = runtimeField.GetValue(source);
            if (value is not Node node) continue;

            propertyInfo.SetValue(instance,
                propertyInfo.PropertyType == typeof(string) ? node.GetNodeValue()!.ToString() : node.GetNodeValue());
        }

        return instance;
    }
}