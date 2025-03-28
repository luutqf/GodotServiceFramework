using System.Collections;
using AspectInjector.Broker;

namespace GodotServiceFramework.Binding;

[AttributeUsage(AttributeTargets.Method)]
[Aspect(Scope.Global)]
[Injection(typeof(BindingCacheAttribute))]
public class BindingCacheAttribute : Attribute
{
    [Advice(Kind.After)]
    public void After([Argument(Source.ReturnValue)] object retValue)
    {
        switch (retValue)
        {
            case IBinding binding:
                DataStore.Set(binding);
                break;
            case IEnumerable enumerable:
            {
                // 获取元素类型
                foreach (var o in enumerable)
                {
                    if (o is IBinding binding)
                    {
                        DataStore.Set(binding);
                    }
                }

                break;
            }
        }
    }
}