using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Util;
using Timer = Godot.Timer;

namespace GodotServiceFramework.GTask;

public partial class MyTimer : Timer
{
    public readonly Dictionary<string, XTimer> Timers = [];

    [Service]
    public override void _Ready()
    {
    }

    public void Create(string id, Action? onTimeout, double delay, bool repeating = false,
        bool callDeferred = false)
    {
        if (Timers.ContainsKey(id))
        {
            throw new Exception("timerId 重复了");
        }

        var timer = new XTimer(this, delay, repeating, callDeferred);
        timer.Timeout += onTimeout;
        Timers[id] = timer;
    }

    public void Start(string id)
    {
        if (Timers.TryGetValue(id, out var timer))
        {
            timer.Start();
        }
    }

    public void Stop(string id)
    {
        if (Timers.TryGetValue(id, out var timer))
        {
            timer.Stop();
        }
    }

    public void Remove(string id)
    {
        if (!Timers.TryGetValue(id, out var timer)) return;

        timer.Stop();
        RemoveChild(timer);
        Timers.Remove(id);
    }
}