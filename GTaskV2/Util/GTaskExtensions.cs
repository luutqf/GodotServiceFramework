using GodotServiceFramework.Context.Service;
using GodotServiceFramework.GTaskV2.Entity;
using GodotServiceFramework.GTaskV2.Model;

namespace GodotServiceFramework.GTaskV2.Util;

public static class GTaskExtensions
{
    /// <summary>
    /// 插入一个新任务, 并立即执行, 
    /// </summary>
    /// <param name="this"></param>
    /// <param name="name"></param>
    /// <param name="paramsDict"></param>
    public static BaseGTask InsertAndRun(this BaseGTask @this, string name, Dictionary<string, object> paramsDict)
    {
        var factory = Services.Get<GTaskFactory>()!;
        var model = new GTaskModel
        {
            Name = name,
            NextIds = [],
            Parameters = paramsDict,
        };
        var baseGTask = factory.CreateTask(model, @this.Context);
        _ = baseGTask.Start();
        return baseGTask;
    }

    public static void InsertAndRunFlow(this BaseGTask @this, GTaskFlowEntity entity)
    {
        var taskFlow = new GTaskFlow(context: @this.Context);
        taskFlow.Initialize(entity);
        taskFlow.Start();
    }

    /// <summary>
    /// 添加一个工作流, 该工作流会在当前任务完成后执行,工作流的最后一个任务会继承当前任务的后续任务
    /// </summary>
    /// <param name="this"></param>
    /// <param name="entity"></param>
    /// <param name="replace"></param>
    public static void InsertAfterFlow(this BaseGTask @this, GTaskFlowEntity entity, bool replace = true)
    {
        var taskFlow = new GTaskFlow(context: @this.Context);
        taskFlow.Initialize(entity);
        if (replace)
        {
            if (taskFlow.LastTask != null)
            {
                // taskFlow.LastTask.Model.NextIds = @this.Model.NextIds;
                taskFlow.LastTask.Model.NextModels = @this.Model.NextModels;
            }

            @this.NextTasks.Clear();
        }

        @this.NextTasks.Add(taskFlow.FirstTask!);
    }

    /// <summary>
    /// 向任务后方插入一个任务,如果replace, 则会替换当前任务的后续任务, 原本的后续任务会被添加到新任务的后续任务中
    /// </summary>
    /// <param name="this"></param>
    /// <param name="name"></param>
    /// <param name="paramsDict"></param>
    /// <param name="replace"></param>
    public static void InsertAfter(this BaseGTask @this, string name, Dictionary<string, object> paramsDict,
        bool replace = true)
    {
        var factory = Services.Get<GTaskFactory>()!;
        var model = new GTaskModel
        {
            Name = name,
            NextIds = [],
            Parameters = paramsDict,
        };
        var task = factory.CreateTask(model, @this.Context);


        if (replace)
        {
            task.NextTasks.AddRange(@this.NextTasks);
            @this.NextTasks.Clear();
        }

        @this.NextTasks.Add(task);
    }

    private static object GetByCommon(BaseGTask @this, string name)
    {
        if (@this.Context.CommonParameters.TryGetValue(name, out var value))
        {
            return value;
        }

        return string.Empty;
    }

    public static bool TryGet(this BaseGTask @this, string name, out object? value)
    {
        try
        {
            value = @this.Get(name);
            return true;
        }
        catch (Exception)
        {
            value = null;
            return false;
        }
    }

    /// <summary>
    /// 获取任务参数,如果任务内部没有, 则从上下文中获取
    /// </summary>
    /// <param name="this"></param>
    /// <param name="name"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static object Get(this BaseGTask @this, string name, object? defaultValue = null)
    {
        if (@this.Parameters.TryGetValue(name, out var value))
        {
            switch (value)
            {
                case string s when s == name:
                {
                    return GetByCommon(@this, name);
                }
                case string s when s.StartsWith("common:"):
                {
                    if (@this.Context.CommonParameters.TryGetValue(s[7..], out var commonValue))
                    {
                        return commonValue;
                    }

                    throw new Exception($"Unknown common parameter: {s[7..]}");
                }
                default:
                    return value;
            }
        }

        if (@this.Context.CommonParameters.TryGetValue(name, out value))
        {
            return value;
        }

        if (defaultValue == null)
            throw new Exception($"Unknown common parameter: {name}");

        return defaultValue;
    }

    /// <summary>
    /// model数组转换为任务数组
    /// </summary>
    /// <param name="this"></param>
    /// <param name="context"></param>
    /// <returns></returns>
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

    /// <summary>
    /// model转换为任务
    /// </summary>
    /// <param name="this"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public static BaseGTask ToGTask(this GTaskModel @this, GTaskContext? context = null)
    {
        context ??= GTaskContext.Empty;
        return Services.Get<GTaskFactory>()!.CreateTask(@this, context);
    }

    /// <summary>
    /// 获取下一个任务, 如果没有, 则判断model的NextModels, 如果存在, 则创建新的任务,并添加到NextTasks中,返回NextTasks
    /// </summary>
    /// <param name="this"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public static List<BaseGTask> GetNextTasks(this BaseGTask @this, GTaskContext? context)
    {
        context ??= GTaskContext.Empty;

        if (@this.NextTasks.Count == 0)
        {
            var factory = Services.Get<GTaskFactory>()!;
            foreach (var model in @this.Model.NextModels)
            {
                @this.NextTasks.Add(factory.CreateTask(model, context));
            }
        }

        return @this.NextTasks;
    }
}