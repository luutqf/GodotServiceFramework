using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GTaskV3;

/// <summary>
/// POD用于存放并行任务
///
/// 我们考虑一下, pod是否可以根据规则表达式自动执行, 我们认为不可以, 规则驱动的最小单元应当是任务集, pod的状态应当继承自任务集.
///
/// 我们也可以保留这个Condition, 但干嘛用再说.
/// </summary>
public sealed class GTaskPod
{
    public long Id { get; } = SnowflakeIdGenerator.NextId();

    public GTaskPod? Next { get; set; }

    public GTaskSet Set { get; set; } = null!;

    //传递上下文
    public GTaskContext Context { get; set; } = null!;

    //这里的任务是并行的, 一次全执行
    public GTaskModel[] Models
    {
        get;
        init
        {
            foreach (var model in value)
            {
                model.Pod = this;
            }

            field = value;
        }
    } = [];

    public void CheckProgress()
    {
        if (Progress < 100) return;

        if (!TryNext())
        {
            //没有下一个了, 上层检查 
            // Log.Info($"现在位置是:{this.Models.First().Name}");
            Set.CheckProgress();
        }
    }


    //计算进度
    public int Progress
    {
        get
        {
            if (Models.Length == 0) return 0;

            // 如果任意一个model的Progress为-1，则返回-1
            if (Models.Any(model => model.Progress == -1))
                return -1;

            return Math.Min(100, Models.Sum(model => Math.Min(100, model.Progress)) / Models.Length);
        }
    }


    public void Start()
    {
        var queue = Services.Get<GTaskPool>()!.GetTasks(Models.Length);

        foreach (var model in Models)
        {
            model.Context = Context;
            var task = queue.Dequeue();
            task.Init(model);
            Task.Run(() => task.Start());
        }
    }

    public bool TryNext()
    {
        if (Next != null)
        {
            Next.Start();
            return true;
        }
        else
        {
            //TODO 这里把生命周期为Set的后台任务终止
            Context?.Send(TaskEvent.NextNothing, null);
            return false;
        }
    }
}