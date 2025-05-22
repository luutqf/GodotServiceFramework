using GodotServiceFramework.GTaskV2.Model;
using Timer = System.Threading.Timer;

namespace GodotServiceFramework.GTaskV2.Base;

/// <summary>
/// 后台任务一定是定时任务, 需要定时检查是否终止
/// </summary>
/// <param name="model"></param>
/// <param name="context"></param>
public abstract class BaseTimerGTask(GTaskModel model, GTaskContext context) : BaseGTask(model, context)
{
    private Timer? _timer;
    private int Delay { get; set; } = 1;

    private bool AutoFail { get; set; } = true;

    private int Count { get; set; } = 15;

    private int _count;

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
    }

    protected override Task Run()
    {
        // 第一个参数是回调方法，第二个是状态对象，第三个是启动延迟时间，第四个是间隔时间
        _timer = new Timer(
            callback: TimerTask!,
            state: null,
            dueTime: 1000,
            period: Delay * 1000);
        return Task.CompletedTask;
    }


    private void TimerTask(object state)
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

        _count++;
        OnTimeout();
    }

    protected abstract void OnTimeout();


    protected override void Complete()
    {
        Progress = 100;
        _timer?.Dispose();
    }
}