using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Extensions;
using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.GTaskV2.Util;
using GodotServiceFramework.Util;
using SigmusV2.GodotServiceFramework.GTaskV2;
using Timer = System.Threading.Timer;

namespace GodotServiceFramework.GTaskV2.Base;

/// <summary>
/// 后台任务一定是定时任务, 需要定时检查是否终止
/// </summary>
/// <param name="model"></param>
public abstract class BaseTimerGTask(GTaskModel model, GTaskFlow flow) : BaseGTask(model, flow)
{
    protected object LockObject { get; } = new();

    // private Timer? _timer;
    protected virtual int Delay { get; set; } = 1;

    protected virtual bool AutoFail { get; set; } = true;

    protected virtual int Count { get; set; } = 30;

    protected virtual bool Infinite { get; set; } = false;

    protected int CurrentCount;

    private bool _running = false;


    public override void BeforeStart()
    {
        if (Parameters.TryGetValue("delay", out var value))
        {
            if (value is int or long or float or double or decimal)
            {
                Delay = Convert.ToInt32(value);
            }
        }

        if (Parameters.TryGetValue("count", out var count))
        {
            if (count is int or long or float or double or decimal)
            {
                Count = Convert.ToInt32(count);
            }
        }

        if (Parameters.TryGetValue("autofail", out var autoFail))
        {
            AutoFail = Convert.ToBoolean(autoFail);
        }

        if (Parameters.TryGetValue("infinite", out var infinite))
        {
            Infinite = Convert.ToBoolean(infinite);
        }
    }

    protected override Task<int> Run()
    {
        // 第一个参数是回调方法，第二个是状态对象，第三个是启动延迟时间，第四个是间隔时间
        var timer = Services.Get<GTaskTimer>()!;
        var actionModel = new GTaskActionModel
        {
            Id = SingleId,
            Name = this.GetTitle(),
            Delay = Delay,
            Callback = TimerTask
        };
        timer.AddTimerAction(actionModel);
        return Task.FromResult(10);
    }


    private void TimerTask()
    {
        if (_running) return;
        _running = true;

        try
        {
            if (Progress is >= 100 and < CompleteLine)
            {
                Log.Info("Task already completed, no need to run timer task again.");
                return;
            }


            //如果不是无限循环, 则判断执行次数
            if (!Infinite)
            {
                if (CurrentCount >= Count)
                {
                    // _timer?.Dispose();
                    Services.Get<GTaskTimer>()!.StopTimerAction(SingleId);
                    if (AutoFail)
                    {
                        Log.Warn($"{this.GetTitle()} 次数超限");
                        Progress = -1;
                    }

                    else
                        Progress = 100;

                    return;
                }
            }


            CurrentCount++;

            OnTimeout().Wait();
        }
        finally
        {
            _running = false;
        }
    }

    protected abstract Task OnTimeout();


    protected override void Complete()
    {
        base.Complete();
        Progress = 100;
        // _timer?.Dispose();
        Services.Get<GTaskTimer>()!.StopTimerAction(SingleId);
    }
}