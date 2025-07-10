using System.Reflection;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GTaskV3;

/// <summary>
/// 这是任务方法工厂, 从assembly中获取方法字段
/// </summary>
[InjectService]
public class GTaskFuncFactory
{
    // 存储所有注册的任务函数，key为任务名称，value为对应的Func
    private readonly Dictionary<string, Func<GTaskModel, Task<int>>> _taskFuncs = new();

    public GTaskFuncFactory()
    {
        RegisterTaskFuncs();
    }

    /// <summary>
    /// 注册所有带有GTaskFuncAttribute的Func字段
    /// </summary>
    private void RegisterTaskFuncs()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(true).All(a => a.GetType() != typeof(GTaskFuncAttribute))) continue;

                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (field.GetCustomAttributes(true).Any(a => a.GetType() == typeof(GTaskFuncAttribute)))
                    {
                        RegisterTaskFunc(field);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 注册单个任务函数
    /// </summary>
    /// <param name="field">带有GTaskFuncAttribute的字段</param>
    private void RegisterTaskFunc(FieldInfo field)
    {
        // 检查字段类型是否为Func<GTaskContext, GTaskModel, Task<int>>
        if (field.FieldType == typeof(Func<GTaskModel, Task<int>>))
        {
            // 获取GTaskFuncAttribute来获取任务名称
            var attribute = field.GetCustomAttribute<GTaskFuncAttribute>();
            var taskName = attribute?.Name ?? field.Name;

            // 获取字段值（静态字段）
            var taskFunc = (Func<GTaskModel, Task<int>>?)field.GetValue(null);

            if (taskFunc != null)
            {
                _taskFuncs[taskName] = taskFunc;

                Log.Info($"register task func: {taskName}");
            }
        }
    }

    /// <summary>
    /// 检查任务是否存在
    /// </summary>
    /// <param name="taskName">任务名称</param>
    /// <returns>是否存在</returns>
    public bool HasTask(string taskName)
    {
        return _taskFuncs.ContainsKey(taskName);
    }

    /// <summary>
    /// 获取所有已注册的任务名称
    /// </summary>
    /// <returns>任务名称列表</returns>
    public IEnumerable<string> GetRegisteredTaskNames()
    {
        return _taskFuncs.Keys;
    }


    /// <summary>
    /// 通过名称获取任务函数
    /// </summary>
    /// <param name="taskName">任务名称</param>
    /// <returns>任务函数，如果不存在则返回null</returns>
    public Func<GTaskModel, Task<int>>? GetTaskFunc(string taskName)
    {
        return _taskFuncs.GetValueOrDefault(taskName);
    }

    /// <summary>
    /// 通过名称获取任务函数（带异常抛出）
    /// </summary>
    /// <param name="taskName">任务名称</param>
    /// <returns>任务函数</returns>
    /// <exception cref="ArgumentException">当任务不存在时抛出</exception>
    public Func<GTaskModel, Task<int>> GetTaskFuncOrThrow(string taskName)
    {
        if (_taskFuncs.TryGetValue(taskName, out var taskFunc))
        {
            return taskFunc;
        }

        throw new ArgumentException($"未找到任务函数: {taskName}");
    }
}