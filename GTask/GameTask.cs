using GodotServiceFramework.Util;
using Newtonsoft.Json.Linq;

namespace GodotServiceFramework.GTask;

public abstract class GameTask(GameTaskWorkflow gameTaskWorkflow, int[] index, Dictionary<string, object> args)
{
    private bool _initialized;

    private bool _completed;
    protected string TaskId => $"{GameTaskWorkflow.Id}-{Index[0]}-{Index[1]}";

    private readonly List<string> _skipKeys = [];

    private readonly List<string> _skipNoKeys = [];

    protected GameTaskContext Context => GameTaskWorkflow.Context;

    public void AppendResult(string line)
    {
        GameTaskWorkflow.AppendResult(Index, Title, $"{line}", 1);
    }

    protected void AppendWarn(string line)
    {
        GameTaskWorkflow.AppendResult(Index, Title, $"{line}", 1);
    }

    protected void AppendError(string line)
    {
        GameTaskWorkflow.AppendResult(Index, Title, $"{line}", 3);
    }

    public void AppendSingle(string line)
    {
        GameTaskWorkflow.AppendResult(Index, Title, $"{line}", 4);
    }


    public bool Destroyed;

    /// <summary>
    /// 检查任务的执行进度, 管理器通过调用这个方法, 更新任务序列的状态, 并决定是否开始执行下一个任务.
    /// </summary>
    public int Progress
    {
        get => _progress;
        protected set
        {
            if (_progress is >= TaskComplete or TaskError or TaskAbort or SkipAllTask) return;

            if (_progress == value) return;

            _progress = value;

            GameTaskWorkflow.UpdateProgress(Index, value);
            //这里,如果是100, 通知任务管理器
            switch (value)
            {
                case SkipAllTask:
                {
                    OnCompleted0();
                    Destroy0();
                    GameTaskWorkflow.SkipAllTask();
                    break;
                }
                case TaskComplete: //只进入一次
                    Logger.Info($"{Name} in {GameTaskWorkflow.Id} task workflow completed.");

                    OnCompleted0();
                    Destroy0();
                    Task.Run(() => { GameTaskWorkflow.CompleteTask(Index, _progress); });
                    break;
                case > TaskComplete:
                    //后成功, 已经完成销毁的成功. 一般搭配-2?
                    OnCompleted0();
                    break;
                case TaskError: //只进入一次
                    Logger.Info($"{Name} in {GameTaskWorkflow} task workflow error.");
                    // OnError();
                    Destroy0();
                    GameTaskWorkflow.StopWorkflow(true, true);

                    GameTaskWorkflow.Context.PutTag(GameTaskWorkflow.Id, "error");
                    break;
                case TaskAbort: //只进入一次, 不会触发任务完成的逻辑
                    //主動中止
                    Destroy0();
                    GameTaskWorkflow.StopWorkflow(true);
                    break;
                case TaskSelfSkip: //后台运行,不自动销毁. 当任务全部结束时, 自动销毁. 视为主动跳过
                    Logger.Info($"{Name} in {GameTaskWorkflow} task workflow backend.");
                    Task.Run(() => { GameTaskWorkflow.CompleteTask(Index, _progress); });
                    break;
                case TaskTagSkip:
                    //跳过不会执行初始化, 销毁, 没有任何操作, 直接进行下一个.
                    Destroy0();
                    Logger.Info($"{Name} in {GameTaskWorkflow} task workflow skipped.");
                    Task.Run(() => { GameTaskWorkflow.CompleteTask(Index, _progress); });

                    break;
                case TaskGhostSkip:
                    //什么都不做
                    break;
                case TaskStart:
                    if (!_initialized)
                    {
                        Init();
                        _initialized = true;
                    }

                    break;
            }
        }
    }

    public readonly Dictionary<string, object> Args = args;

    public readonly int[] Index = index;

    public readonly GameTaskWorkflow GameTaskWorkflow = gameTaskWorkflow;

    private int _progress = TaskDefault;

    /// <summary>
    /// 唯一的名称,代表一个任务类别
    /// </summary>
    public virtual string Name => GetType().Name;

    public string Title { get; set; } = string.Empty;

    public virtual string StartMessage => $"{Title} started";
    public virtual string SuccessMessage => $"✅  {Title} succeeded";
    public virtual string SkipMessage => $"✅  {Title} skipped";
    public virtual string FailMessage => $"❌  {Title} failed";

    public virtual Dictionary<string, string> SkipMarker => [];


    /// <summary>
    /// 这个方法由管理器调用, 用于注册被动触发的行为, 即监控某些数据或状态, 当发生变化时, 触发任务执行.
    /// </summary>
    public virtual void Init()
    {
    }

    /// <summary>
    /// 这个方法在任务完成或发生错误时,取消被动触发行为
    /// </summary>
    public virtual void Destroy()
    {
    }

    public void Destroy0()
    {
        lock (this)
        {
            if (Destroyed) return;
            Destroyed = true;
        }

        Destroy();
        //主动中止也算成功
        var success = Progress is >= TaskComplete or TaskGhostSkip or TaskSelfSkip or TaskTagSkip or TaskAbort;
        switch (Progress)
        {
            case >= TaskComplete or TaskAbort:
                AppendSingle(SuccessMessage);
                break;
            case TaskGhostSkip or TaskSelfSkip or TaskTagSkip:
                AppendSingle(SkipMessage);
                break;
            default:
                AppendError(FailMessage);
                break;
        }
    }

    public async Task Start0()
    {
        lock (this)
        {
            //这里要考虑并发问题,存在同时触发下一个任务的情况
            if (Progress != TaskDefault) return;


            if (Args.TryGetValue("skipKeys", out var skipKeys))
            {
                if (skipKeys is JArray array)
                {
                    foreach (var jToken in array)
                    {
                        _skipKeys.Add(jToken.ToString());
                    }
                }
            }

            if (Args.TryGetValue("skipNoKeys", out var skipNoKeys))
            {
                if (skipNoKeys is JArray array)
                {
                    foreach (var jToken in array)
                    {
                        _skipNoKeys.Add(jToken.ToString());
                    }
                }
            }

            //检查任务是否可以跳过, 如果包含这个tag, 则跳过
            if (_skipKeys.Any(key => GameTaskWorkflow.Context.HasTag(key)))
            {
                AppendSingle($"{Name} 跳过 -> {skipKeys}");
                Progress = TaskSelfSkip;
                return;
            }


            //如果不包含这个tag,则跳过
            if (_skipNoKeys.Any(key => !GameTaskWorkflow.Context.HasTag(key)))
            {
                AppendSingle($"{Name} 跳过 -> {skipKeys}");
                Progress = TaskSelfSkip;
                return;
            }


            Progress = TaskStart;
        }

        try
        {
            AppendResult(StartMessage);
            var progress = await Start();
            Progress = progress;
        }
        catch (Exception e)
        {
            Logger.Error(e);
            Progress = -1;
        }
    }

    /// <summary>
    /// 执行这个任务, 任务执行期间需要自行维护自己的生命周期,  自己想办法, 因为某些动作可能是异步的
    /// </summary>
    public abstract Task<int> Start();


    private void OnCompleted0()
    {
        lock (this)
        {
            if (_completed) return;
            _completed = true;
            OnCompleted();
        }
    }

    protected virtual void OnCompleted()
    {
    }

    protected virtual void OnError()
    {
    }


    public const int TaskAbort = -2;

    public const int TaskError = -1;

    public const int TaskStart = 1;

    public const int TaskDefault = 0;

    public const int TaskComplete = 100;

    public const int TaskSelfSkip = -5;

    public const int TaskTagSkip = -3;

    public const int TaskGhostSkip = -4;

    public const int SkipAllTask = -100;


    // public const int TaskTerminate = 100;
}