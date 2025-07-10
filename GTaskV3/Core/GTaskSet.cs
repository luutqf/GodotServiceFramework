using System.Diagnostics.CodeAnalysis;
using GodotServiceFramework.Binding;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.GTaskV3.Entity;
using GodotServiceFramework.Util;
using Newtonsoft.Json;
using SQLite;

namespace GodotServiceFramework.GTaskV3;

/// <summary>
/// 任务集, 用于记录一个可执行的任务序列, 序列是顺序的, 节点可能会嵌入其他序列,作为任务分支
///
/// 这里强调, 任务集一定是顺序的, 或有触发条件的
/// </summary>
public class GTaskSet(GTaskContext context, GTaskSetEntity entity)
{
    public GTaskSetEntity Entity { get; set; } = entity;

    public long Id { get; set; } = SnowflakeIdGenerator.NextId();

    public string Name { get; } = entity.Name;

    public GTaskContext Context { get; set; } = context;

    //条件用于触发任务流的执行, 
    //当因为资源不足无法运行时, 应当可以设置一个状态, 重新激活此任务集
    public string Condition { get; set; } = entity.Condition;


    [field: AllowNull, MaybeNull]
    public Dictionary<string, object> Parameters =>
        field ??= JsonConvert.DeserializeObject<Dictionary<string, object>>(Entity.ParametersJson)!;


    [field: AllowNull, MaybeNull]
    public List<GTaskPod> Pods
    {
        get
        {
            field ??= JsonConvert.DeserializeObject<List<GTaskPod>>(Entity.TaskContentJson)!;

            // 按顺序为每个Pod的Next字段赋值
            for (var i = 0; i < field.Count - 1; i++)
            {
                field[i].Next = field[i + 1];
            }

            foreach (var pod in field)
            {
                pod.Context = Context;
                pod.Set = this;
            }

            return field;
        }
    }

    public event Action OnComplete = delegate { };

    //计算进度
    public int Progress
    {
        get
        {
            if (Pods.Count == 0) return 0;

            // 如果任意一个pod的Progress为-1，则返回-1
            if (Pods.Any(pod => pod.Progress == -1))
                return -1;

            return (int)Pods.Average(pod => pod.Progress);
        }
    }

    /// <summary>
    /// 重置任务集
    /// </summary>
    /// <returns></returns>
    public bool Reset()
    {
        return false;
    }

    public void RuleStart()
    {
        var b = Context.Evaluate(Condition);
        Log.Info($"b:{b}", BbColor.Green);
        if (b)
        {
            Start();
        }
    }

    public void Start()
    {
        Context.Insert(this);
        Pods.FirstOrDefault()?.Start();
    }

    public void CheckProgress()
    {
        if (Progress == 100)
        {
            Context.Send(TaskEvent.TaskSetComplete, set: this);
            OnComplete.Invoke();

            OnComplete = delegate { };

            Services.Get<GTaskPool>()?.Clear(Id);
        }
    }
}