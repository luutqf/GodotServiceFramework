using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Db;
using GodotServiceFramework.GTaskV3.Entity;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GTaskV3.Util;

public static class GTaskModelExtensions
{
    /// <summary>
    /// 获取参数, 优先获取自己的, 或者是set的, 最后是公共的
    /// </summary>
    /// <param name="this"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static object GetParam(this GTaskModel @this, string name)
    {
        if (@this.Parameters.TryGetValue(name, out var value) ||
            @this.Pod.Set.Parameters.TryGetValue(name, out value) ||
            @this.Context.CommonParameters.TryGetValue(name, out value))
        {
            return value;
        }

        throw new Exception($"未找到 {name} 参数");
    }

    public static object? GetParamOrDefault(this GTaskModel @this, string name, object? defaultValue = null)
    {
        if (@this.Parameters.TryGetValue(name, out var value) ||
            @this.Pod.Set.Parameters.TryGetValue(name, out value) ||
            @this.Context.CommonParameters.TryGetValue(name, out value))
        {
            return value;
        }

        return defaultValue;
    }

    public static bool TryGetParam(this GTaskModel @this, string name, out object? value)
    {
        try
        {
            var result = @this.GetParam(name);
            value = result;
            return true;
        }
        catch (Exception)
        {
            value = null;
            return false;
        }
    }


    public static void InsertTaskSet(this GTaskModel @this, string name, Action? callback = null)
    {
        var entity = SqliteManager.FindByName<GTaskSetEntity>(name);
        if (entity == null)
        {
            throw new Exception($"{name} taskSet not found");
        }

        var set = new GTaskSet(@this.Context, entity);
        set.Start();
        if (callback == null)
        {
            set.OnComplete += () => { @this.Progress = 100; };
        }
        else
        {
            set.OnComplete += callback;
        }
    }

    public static void InsertTask(this GTaskModel @this, GTaskModel model, Action? callback = null)
    {
        var task = Services.Get<GTaskPool>()!.GetTask();
        model.Context = @this.Context;
        model.Pod = @this.Pod;
        task.Init(model);
        if (callback == null)
        {
            model.OnCompleted += () =>
            {
                // Log.Info("??");
            };
        }
        else
        {
            model.OnCompleted += callback;
        }

        _ = task.Start();
    }


    public static void Info(this GTaskModel @this, string msg, BbColor color = BbColor.Gray)
    {
        @this.Context.Send(TaskEvent.Info, @this, msg: msg, color: color);
    }


    public static void Warn(this GTaskModel @this, string msg, BbColor color = BbColor.Orange)
    {
        @this.Context.Send(TaskEvent.Warning, @this, msg: msg, color: color);
    }

    public static void Error(this GTaskModel @this, string msg, BbColor color = BbColor.Red)
    {
        @this.Context.Send(TaskEvent.Error, @this, msg: msg, color: color);
    }

    public static void Success(this GTaskModel @this, string msg, BbColor color = BbColor.Green)
    {
        @this.Context.Send(TaskEvent.SuccessMessage, @this, msg: msg);
    }
}