using GodotServiceFramework.Context.Service;
using GodotServiceFramework.GTask;
using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GTaskV2;

/// <summary>
/// 任务工厂, 用于创建新的任务实例, 
/// </summary>
public partial class GTaskFactory : AutoGodotService
{
    /// <summary>
    /// 这里缓存着任务名称和类型
    /// </summary>
    private readonly Dictionary<string, Type> _taskTypes = [];

    public GTaskFactory()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            // 获取当前程序集
            var typesWithMyAttribute = assembly.GetTypes()
                .Where(type => type.IsSubclassOf(typeof(BaseGTask)));

            foreach (var type in typesWithMyAttribute)
            {
                if (type.IsAbstract || type.IsInterface) continue;
                var instance =
                    (BaseGTask)Activator.CreateInstance(type, GTaskModel.DefaultModel, GTaskContext.Empty)!;
                _taskTypes.TryAdd(instance.Name, type);
                Log.Debug($"Task {instance.Name} has been found");
            }
        }
    }

    // public BaseGTask CreateTask(string taskName, GTaskContext context)
    // {
    //     
    // }
    
    public BaseGTask CreateTask(GTaskModel model, GTaskContext context)
    {
        if (!_taskTypes.TryGetValue(model.Name, out var type))
        {
            Log.Error($"Task {model.Name} not found");
            return null!;
        }


        var task = (BaseGTask)Activator.CreateInstance(type, model, context)!;


        task.Model = model;
        task.Parameters = model.Parameters;

        task.Context = context;

        return task;
    }


}