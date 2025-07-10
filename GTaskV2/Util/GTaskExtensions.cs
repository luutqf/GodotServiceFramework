using GodotServiceFramework.Context.Service;
using GodotServiceFramework.GTaskV2.Entity;
using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.Util;

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
        var baseGTask = factory.CreateTask(model, @this.Flow);
        _ = baseGTask.Start();
        return baseGTask;
    }

    public static void InsertAndRunFlow(this BaseGTask @this, GTaskFlowEntity entity,
        Action<GTaskFlow>? callback = null)
    {
        var taskFlow = new GTaskFlow(context: @this.Context);
        taskFlow.Initialize(entity);

        if (taskFlow.GTaskGraveyard != null)
            taskFlow.GTaskGraveyard.OnComplete += () => { callback?.Invoke(taskFlow); };

        // taskFlow.GTaskGraveyard.OnComplete += () => { @this.Progress = 100; };
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
            if (taskFlow.GTaskGraveyard != null)
            {
                taskFlow.GTaskGraveyard.Model.NextModels = @this.Model.NextModels;
            }

            @this.NextTasks.Clear();
        }

        @this.NextTasks.AddRange(taskFlow.StartTasks);
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
        var task = factory.CreateTask(model, @this.Flow);


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

    public static bool TryGetArg(this BaseGTask @this, string name, out object? value)
    {
        try
        {
            value = @this.GetArg(name);
            return true;
        }
        catch (Exception)
        {
            value = null;
            return false;
        }
    }


    public static object? GetArgOrDefault(this BaseGTask @this, string name, object? defaultValue = null)
    {
        try
        {
            return @this.GetArg(name);
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 获取任务参数,如果任务内部没有, 则从上下文中获取
    /// </summary>
    /// <param name="this"></param>
    /// <param name="name"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static object GetArg(this BaseGTask @this, string name)
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

        // if (defaultValue == null)
        throw new Exception($"Unknown common parameter: {name}");

        // return defaultValue;
    }

    /// <summary>
    /// model数组转换为任务数组
    /// </summary>
    /// <param name="this"></param>
    /// <param name="flow"></param>
    /// <param name="entityFirstNodeId"></param>
    /// <returns></returns>
    public static List<BaseGTask> ModelToStartGTask(this GTaskModel[] @this, GTaskFlow flow,
        string entityFirstNodeId = "")
    {
        List<BaseGTask> gTasks = [];

        foreach (var t in @this)
        {
            //TODO 这里默认只留下了第一个, 实际上还可以有其他后台任务
            if (t.Id != entityFirstNodeId) continue;

            gTasks.Add(Services.Get<GTaskFactory>()!.CreateTask(t, flow));
        }


        return gTasks;
    }

    /// <summary>
    /// model转换为任务
    /// </summary>
    /// <param name="this"></param>
    /// <param name="flow"></param>
    /// <returns></returns>
    public static BaseGTask ModelToStartGTask(this GTaskModel @this, GTaskFlow flow)
    {
        return Services.Get<GTaskFactory>()!.CreateTask(@this, flow);
    }

    /// <summary>
    /// 获取下一个任务, 如果没有, 则判断model的NextModels, 如果存在, 则创建新的任务,并添加到NextTasks中,返回NextTasks
    /// </summary>
    /// <param name="this"></param>
    /// <returns></returns>
    public static List<BaseGTask> GetNextTasks(this BaseGTask @this)
    {
        if (@this.NextTasks.Count == 0)
        {
            // context ??= GTaskContext.Empty;
            var factory = Services.Get<GTaskFactory>()!;
            foreach (var model in @this.Model.NextModels)
            {
                var task = factory.CreateTask(model, @this.Flow);
                @this.NextTasks.Add(task);
            }
        }

        var flow = @this.Flow.StartTasks;
        flow.AddRange(@this.NextTasks);


        return @this.NextTasks;
    }


    public static void PutMessage(this BaseGTask @this, string message)
    {
        if (!@this.Context.FlowMessage.TryGetValue(@this.Flow.Name, out var progress))
        {
            progress = [];
            @this.Context.FlowMessage[@this.Flow.Name] = progress;
        }

        progress.Add(message);
    }

    public static void Info(this BaseGTask @this, string message, BbColor color = BbColor.Gray)
    {
        Log.Info(message, color);
        @this.Context.SendMessage(@this, message);
    }

    public static void SuccessMsg(this BaseGTask @this, string message, BbColor color = BbColor.Gray)
    {
        Log.Info(message, color);
        @this.Context.SendMessage(@this, message, ActionType.Success);
    }
    // public static void Debug(this BaseGTask @this, string message, BbColor color = BbColor.Gray)
    // {
    //     Log.Debug(message, color);
    //     // @this.Context.SendMessage(@this, message, color);
    // }

    public static void Error(this BaseGTask @this, object message)
    {
        Log.Error(message);
        @this.Context.SendMessage(@this, message.ToString()!, ActionType.Error);
    }

    public static void Warn(this BaseGTask @this, string message)
    {
        Log.Warn(message);
        @this.Context.SendMessage(@this, message, ActionType.Warn);
    }


    public static void UpdateProgress(this BaseGTask @this)
    {
        @this.Context.UpdateProgress(@this);
    }


    public static string GetTitle(this BaseGTask @this)
    {
        return @this.Parameters.TryGetValue("title", out var title) ? title.ToString()! : @this.Name;
    }
}