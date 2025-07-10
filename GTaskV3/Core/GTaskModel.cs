using System.Collections.Concurrent;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GTaskV3;

public class GTaskModel
{
    //定时任务的延迟时间, 非定时任务没有延迟
    public int Delay { get; set; } = 1;

    public int RetryCount { get; set; } = 0;

    public bool Daemon { get; set; } = true;


    //任务类型, 任务工厂从任务池中获取,或者创建一个新的实例.
    public required string TaskType { get; set; }

    //任务名称, 用于展示
    public string Name
    {
        get => string.IsNullOrEmpty(field) ? TaskType : field;
        init;
    } = string.Empty;

    //任务描述
    public string Description { get; set; } = string.Empty;

    //这类事件永远是一次性的, 执行之后就会重置
    public event Action OnCompleted = delegate { };

    //任务参数
    public Dictionary<string, object> Parameters { get; set; } = [];

    // public Dictionary<string, object> Cache { get; set; } = [];

    public readonly dynamic Cache = new Dictionary<string, object>();

    #region 运行时的状态

    public long Id { get; } = SnowflakeIdGenerator.NextId();

    public GTaskPod Pod { get; set; } = null!;

    public GTaskStatus Status { get; } = new();

    public GTaskContext Context { get; set; } = null!;

    public Dictionary<string, object> CommonParameters => Context.CommonParameters;

    // //任务进度
    public int Progress
    {
        get => Status.Progress;
        set
        {
            switch (Status.Progress, value)
            {
                //当进度重复时, 不触发逻辑
                case (var p and > 0, var v) when p == v:
                {
                    break;
                }

                //当任务进度已经完成时, 不允许再修改状态
                case (100, _):
                {
                    break;
                }
                //报错会优先处理,但肯定也是只处理一次
                case (_, < 0):
                {
                    Status.Progress = value;
                    Context.Send(TaskEvent.Error, this);
                    //报错了也要调用完成事件
                    OnCompleted.Invoke();
                    OnCompleted = delegate { };
                    break;
                }

                //当任务处于后台定时时, 下达完成, 直接修改为100, 待task二次检查.
                case (101, 100):
                case (102, 100):
                {
                    Status.Progress = value;
                    OnCompleted.Invoke();
                    OnCompleted = delegate { };
                    Cache.Clear();
                    break;
                }
                //当任务处于非后台定时任务状态时, 如静默后台,或阻塞等状态, 如果修改为100, 视为完成任务,执行Completed操作
                //如果是其他状态, 目前是不允许修改
                case (>= 100, var v) when v != 100:
                {
                    break;
                }
                case (_, 100): //如果当前进度小于100, 直接完成
                {
                    Status.Progress = value;
                    Pod.CheckProgress();
                    OnCompleted.Invoke();
                    OnCompleted = delegate { };
                    Cache.Clear();
                    break;
                }
                //如果是从任意普通状态,转为后台定时, 则直接进入下一任务, 但不会触发完成事件
                case (>= 0, 102):
                {
                    Status.Progress = value;

                    //如果不是守护任务, 就需要在当前任务无限循环, 直到自己修改为100进度.
                    if (Daemon)
                        Pod.CheckProgress();

                    break;
                }

                case (>= 0, 101):
                {
                    Status.Progress = value;
                    Pod.CheckProgress();
                    break;
                }


                case (_, 104): //取消
                case (_, 105): //阻塞, 不进行其他操作
                {
                    Status.Progress = value;
                    break;
                }


                //默认就改了
                default:
                    Status.Progress = value;
                    break;
            }
        }
    }

    #endregion
}