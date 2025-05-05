using System.Reflection;
using AspectInjector.Broker;
using Godot;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.Nodes;

/// <summary>
/// 自动注入子节点
/// </summary>
[AttributeUsage(AttributeTargets.All)]
[Aspect(Scope.Global)]
[Injection(typeof(AutowiredAttribute))]
public class AutowiredAttribute : Attribute
{
    [Advice(Kind.Before)]
    public void Before([Argument(Source.Triggers)] Attribute[] attributes,
        [Argument(Source.Type)] Type hostType, [Argument(Source.Instance)] object instance,
        [Argument(Source.Name)] string name)
    {
        if (name != "_Ready") return;

        foreach (var fieldInfo in hostType.GetRuntimeFields())
        {
            if (fieldInfo.GetCustomAttributes().FirstOrDefault(attr => attr is ChildNodeAttribute) is
                ChildNodeAttribute attribute)
            {
                var findChild = ((Node)instance).FindChild(attribute.Name);
                if (findChild == null) continue;

                try
                {
                    fieldInfo.SetValue(instance, findChild);
                }
                catch (Exception e)
                {
                    Log.Warn(e);
                }
            }

            if (fieldInfo.GetCustomAttributes().FirstOrDefault(attr => attr is LoadPackAttribute) is
                LoadPackAttribute loadPackAttribute)
            {
                if (!string.IsNullOrEmpty(loadPackAttribute.Path))
                {
                    if (fieldInfo.FieldType == typeof(PackedScene))
                    {
                        var sceneStatsManager = Services.Get<SceneStatsManager>();
                        if (sceneStatsManager != null)
                        {
                            var packedScene = sceneStatsManager.LoadScene(loadPackAttribute.Path);
                            fieldInfo.SetValue(instance, packedScene);
                        }
                    }
                }
            }
        }
    }
}