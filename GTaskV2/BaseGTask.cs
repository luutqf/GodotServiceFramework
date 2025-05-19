using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.GTaskV2.Util;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GTaskV2;

/// <summary>
/// 默认抽象task, 这里task会作为链表的节点, 但后置节点是多个, 所以
///
/// 我们要把任务系统设计成什么样子?
/// 两方面, 一方面是轻量化使用,如角色的移动, 首先设定一个目标,角色朝目标移动,每次移动一格,
/// 如果检查当前未能移动到指定位置, 则为自己增加一个后续移动任务,目标仍为之前的设置.
///
/// 还应该有一种,角色待命任务,这个任务会持续处于挂起状态, 当特定状态触发时(如新回合阶段), 待命任务会执行,任务会先整理角色当前状态,
/// 然后决策如何进行下一步行动,
///
/// 对于测试开发,任务的流程原则上是固定的, 如各种靶场用例. 也有不固定的, 比如安装sophon, 任务会检测是否已完成前置
/// </summary>
public abstract class BaseGTask(GTaskModel model, GTaskContext context) : IGTask
{
    public virtual string Name => GetType().Name;

    private int _progress;

    public int Progress
    {
        get => _progress;
        set
        {
            //如果传入的值和实际值相等,则忽略
            if (value == _progress)
            {
                return;
            }

            if (_progress is < 0 or >= 100)
            {
                Log.Warn("任务进度无法修改");
                return;
            }

            _progress = value;
            ProgressHandle(value);
        }
    }

    /// <summary>
    /// 处理任务自己的进度,在适当的时候进入下一个任务
    /// </summary>
    /// <param name="progress"></param>
    private void ProgressHandle(int progress)
    {
        switch (progress)
        {
            case TaskDefault:
            {
                break;
            }

            case TaskStart:
            {
                Context.CurrentTasks.Add(this);
                break;
            }

            case TaskComplete:
            {
                StartNext();

                Destroy();
                break;
            }

            case > TaskComplete:
            {
                Destroy();
                break;
            }

            case TaskError:
            {
                Log.Error("");
                Destroy();
                break;
            }

            case TaskSelfSkip:
            case TaskTagSkip:
            {
                StartNext();
                Destroy();
                break;
            }
            case SkipAllTask:
            {
                Destroy();
                break;
            }
        }
    }

    private void StartNext()
    {
        var nextTasks = this.GetNextTasks(Context);
        foreach (var nextTask in nextTasks)
        {
            _ = nextTask.Start();
        }
    }

    public Dictionary<string, object> Parameters { get; set; } = [];


    public object? GetParam(string name)
    {
        if (Parameters.TryGetValue(name, out var value)
            || Context.CommonParameters.TryGetValue(name, out value))
        {
            return value;
        }

        return null;
    }

    public GTaskModel Model { get; set; } = model;

    public readonly List<BaseGTask> NextTasks = [];

    public GTaskContext Context { get; set; } = context;

    public long Id => Model.Id;


    /// <summary>
    /// 启动
    /// </summary>
    /// <returns></returns>
    public async Task<bool> Start()
    {
        switch (Context.TaskStatus)
        {
            case TaskStatus.Pause or TaskStatus.Stop:
            case TaskStatus.Complete:
            {
                return false;
            }
        }

        try
        {
            switch (Progress)
            {
                case TaskDefault:
                {
                    Progress = TaskStart;
                    await Run();
                    return true;
                }
                case > TaskDefault and < TaskComplete:
                {
                    Log.Warn("任务正在运行");
                    return false;
                }
                case TaskError:
                {
                    Log.Warn("任务处于错误状态");
                    return false;
                }
                case >= TaskComplete or TaskSelfSkip or TaskTagSkip:
                {
                    StartNext();
                    return true;
                }
                case SkipAllTask:
                    Log.Warn("已跳过所有任务, 不可以启动");
                    return false;
                default:
                    return false;
            }
        }
        catch (Exception e)
        {
            Progress = TaskError;
            Log.Error(e);
            throw;
        }
    }

    protected abstract Task Run();

    public bool Stop()
    {
        return true;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        Parameters = parameters ?? [];
    }

    public void Pause()
    {
        Context.TaskStatus = TaskStatus.Pause;
    }

    public void Resume()
    {
        Context.TaskStatus = TaskStatus.Running;
        _ = Start();
    }

    public void Reset()
    {
    }

    public void Rollback()
    {
    }

    protected void Complete()
    {
        Progress = 100;
    }


    private void Destroy()
    {
        Context.CurrentTasks.Remove(this);
    }

    
    //发生错误
    public const int TaskError = -1;

    //已启动
    public const int TaskStart = 1;

    //默认状态
    public const int TaskDefault = 0;

    //已完成
    public const int TaskComplete = 100;

    //自行跳过
    public const int TaskSelfSkip = -5;

    //标签跳过
    public const int TaskTagSkip = -3;

    //跳过所有
    public const int SkipAllTask = -100;
}