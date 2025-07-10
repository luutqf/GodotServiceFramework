using System.Collections.Concurrent;
using Godot;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Util;
using Timer = Godot.Timer;

namespace SigmusV2.GodotServiceFramework.GTaskV2;

public class GTaskActionModel
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public double Timer { get; set; }
    public double Delay { get; set; } = 1;

    public bool AutoStart { get; set; } = true;
    public Action Callback { get; set; } = delegate { };
}

/// <summary>
/// 这个Timer用于为所有正在运行的定时GTask,提供间隔性的调度
/// </summary>
[InjectService]
public partial class GTaskTimer : Node
{
    //key 为task的objectId, action是task自己指定的
    private readonly ConcurrentDictionary<long, GTaskActionModel> _actions = [];

    private readonly ConcurrentQueue<long> _willRemoveIds = [];

    public override void _PhysicsProcess(double delta)
    {
        if (!_willRemoveIds.IsEmpty)
        {
            for (var i = 0; i < _willRemoveIds.Count; i++)
            {
                if (!_willRemoveIds.TryDequeue(out var id)) continue;

                if (_actions.Remove(id, out var model))
                {
                    Log.Info($"Timer {model.Name} has been removed", BbColor.Yellow);
                }
            }
        }

        foreach (var pair in _actions)
        {
            if (pair.Value.Active)
            {
                pair.Value.Timer += delta;
                if (pair.Value.Timer >= pair.Value.Delay)
                {
                    pair.Value.Timer = 0;
                    Task.Run(pair.Value.Callback);
                }
            }
            else
            {
                if (!_willRemoveIds.Contains(pair.Key))
                    _willRemoveIds.Enqueue(pair.Key);
            }
        }
    }

    public void AddTimerAction(GTaskActionModel model)
    {
        _actions[model.Id] = model;

        if (model.AutoStart)
        {
            Task.Run(model.Callback);
        }
    }


    public void StopTimerAction(long id)
    {
        _actions[id].Active = false;
    }

    public void RemoveTimerAction(long id)
    {
        StopTimerAction(id);
        _actions.Remove(id, out _);
    }
}