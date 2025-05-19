using GodotServiceFramework.Context.Service;
using GodotServiceFramework.GTaskV2.Model;

namespace GodotServiceFramework.GTaskV2.Util;

public static class GTaskExtensions
{
    public static BaseGTask[] ToGTask(this GTaskModel[] @this, GTaskContext? context = null)
    {
        var result = new BaseGTask[@this.Length];
        for (var i = 0; i < @this.Length; i++)
        {
            context ??= GTaskContext.Empty;

            result[i] = Services.Get<GTaskFactory>()!.CreateTask(@this[i], context);
        }


        return result;
    }

    public static BaseGTask ToGTask(this GTaskModel @this, GTaskContext? context = null)
    {
        context ??= GTaskContext.Empty;
        return Services.Get<GTaskFactory>()!.CreateTask(@this, context);
    }

    public static List<BaseGTask> GetNextTasks(this BaseGTask @this, GTaskContext? context)
    {
        context ??= GTaskContext.Empty;

        if (@this.NextTasks.Count == 0)
        {
            var factory = Services.Get<GTaskFactory>()!;
            foreach (var model in @this.Model!.NextModels)
            {
                @this.NextTasks.Add(factory.CreateTask(model, context));
            }
        }

        return @this.NextTasks;
    }
}