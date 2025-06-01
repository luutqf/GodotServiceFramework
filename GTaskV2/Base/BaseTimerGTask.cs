using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.Util;
using Timer = System.Threading.Timer;

namespace GodotServiceFramework.GTaskV2.Base;

/// <summary>
/// 后台任务一定是定时任务, 需要定时检查是否终止
/// </summary>
/// <param name="model"></param>
/// <param name="context"></param>
public abstract class BaseTimerGTask(GTaskModel model, GTaskContext context) : BaseGTask(model, context)
{
    protected object LockObject { get; } = new();

    private Timer? _timer;
    protected virtual int Delay { get; set; } = 1;

    protected virtual bool AutoFail { get; set; } = true;

    protected virtual int Count { get; set; } = 15;

    protected virtual bool Infinite { get; set; } = false;

    private int _count;

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

    protected override async Task<int> Run()
    {
        // 第一个参数是回调方法，第二个是状态对象，第三个是启动延迟时间，第四个是间隔时间
        _timer = new Timer(
            callback: TimerTask!,
            state: null,
            dueTime: 1000,
            period: Delay * 1000);
        return 10;
    }


    private void TimerTask(object state)
    {
        // if (!Monitor.TryEnter(LockObject))
        // {
        //     return;
        // }

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
                if (_count >= Count)
                {
                    _timer?.Dispose();
                    if (AutoFail)
                        Progress = -1;
                    else
                        Progress = 100;
                    return;
                }
            }


            _count++;

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
        Progress = 100;
        _timer?.Dispose();
    }
}