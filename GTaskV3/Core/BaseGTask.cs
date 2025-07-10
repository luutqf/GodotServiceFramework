using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GTaskV3;

/// <summary>
/// 基础任务类型
/// </summary>
public class BaseGTask : IGTask
{
    public long SetId { get; set; }

    private GTaskModel? _model;

    private Func<GTaskModel, Task<int>>? _func;

    public bool IsUsable => _model == null && _func == null && Progress == 0;

    public int Progress
    {
        get => _model?.Progress ?? 0;
        set
        {
            if (_model != null) _model.Progress = value;
        }
    }


    public void Init(GTaskModel task)
    {
        _model = task;
        _func = Services.Get<GTaskFuncFactory>()!.GetTaskFunc(task.TaskType);
        _model.OnCompleted += Destroy;

        SetId = _model.Pod.Set.Id;
    }

    public async Task Start()
    {
        while (true)
        {
            try
            {
                if (_model == null)
                {
                    //如果没长眼的调用了, 则直接销毁返回
                    Destroy();
                    return;
                }

                if (CheckUnhealthy()) return;

                if (CheckCancel()) return;

                switch (Progress)
                {
                    case 0:
                        Progress = 1;
                        if (_func == null)
                        {
                            Log.Error("func未找到");
                            Destroy();
                            return;
                        }

                        Progress = await _func!.Invoke(_model!);
                        break;
                    case 102:


                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(_model!.Delay * 1000);
                            Progress = await _func!.Invoke(_model!);
                        });


                        break;
                    case > 0 and < 100:
                        //这里要求func是同步完成的
                        Progress = await _func!.Invoke(_model!);
                        break;
                    case > 100:
                        Log.Warn($"本来就是100啊:{_model!.Name}");
                        break;
                }
            }
            catch (Exception e)
            {
                Progress = -1;
                Console.WriteLine(e);
                Log.Error(e);
                // throw;
            }

            //运行之后, 再次检查Progress
            switch (Progress)
            {
                case 0:
                {
                    Log.Debug("并发提前结束了");
                    break;
                }
                case 102:
                {
                    //后台定时运行
                    await Task.Delay(_model!.Delay * 1000);
                    continue;
                }

                case -1:
                {
                    _model!.Context.Send(TaskEvent.Error, _model);
                    return;
                }

                case > 1 and < 100:
                {
                    _model!.Context.Send(TaskEvent.Progress, _model);
                    //正在运行, 正常更新
                    _ = Task.Run(Start);
                    break;
                }

                case 1: //启动状态
                case 101: //后台静默运行
                case 104: //取消
                case 105: //后台阻塞运行
                case 100: //执行完成,清理
                case 103: //跳过
                {
                    _model!.Context.Send(TaskEvent.Progress, _model);
                    break;
                }
            }

            break;
        }
    }

    /// <summary>
    /// 检查是否已取消
    /// </summary>
    /// <returns></returns>
    private bool CheckCancel()
    {
        if (!_model!.Context.Cts.IsCancellationRequested) return false;

        try
        {
            switch (Progress)
            {
                case 102:
                    Log.Info($"后台任务完成:{_model!.Name}");
                    Progress = 100;
                    return true;
                case 100:
                    return true;

                default:
                    Progress = 104;
                    return true;
            }
        }
        finally
        {
            Destroy();
        }
    }

    /// <summary>
    /// 检查是否非健康状态
    /// </summary>
    /// <returns></returns>
    private bool CheckUnhealthy()
    {
        if (_model!.Status.Health) return false;

        if (++_model.Status.RetryCount >= _model.RetryCount)
        {
            _model.Context.Send(TaskEvent.Error, _model, msg: "重试次数超过上限");
            Progress = -1;
            return true;
        }

        _model.Context.Send(TaskEvent.Warning, _model, msg: $"重试:{_model.Status.RetryCount}");

        return false;
    }


    public void Stop()
    {
    }

    public void Pause()
    {
        throw new NotImplementedException();
    }

    public void Resume()
    {
        throw new NotImplementedException();
    }

    public void Destroy()
    {
        _model = null;
        _func = null;
        SetId = 0;

        Services.Get<GTaskPool>()?.ReleaseTask(this);
    }
}