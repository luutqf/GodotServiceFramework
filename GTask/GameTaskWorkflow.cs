using System.Collections;
using Godot;
using GodotServiceFramework.Binding;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.GTask.Entity;
using GodotServiceFramework.GTask.Extensions;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GTask;

/// <summary>
/// 任务工作流, 在这里只作为容器, 不具备主动执行的能力.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public partial class GameTaskWorkflow : RefCounted, IEnumerable<GameTask[]>, IBinding
{
    public GameTaskWorkflow()
    {
        Id = this.RandomFlowId();
    }

    #region 一些基础字段

    public int Id { get; set; }

    /// <summary>
    /// SessionId主要用于将运行状态,定位到一个合适的会话
    /// </summary>
    public ulong SessionId { get; set; }

    /// <summary>
    /// 这是任务的名称, 一般情况下, name是唯一的
    /// </summary>
    public string Name { get; set; } = string.Empty;

    //公共参数, 每个任务都会复制一份快照
    public Dictionary<string, object> CommonArgs { get; set; } = [];

    /// <summary>
    /// 这里保存着Task的数据实体, 任务启动时, 我们会初始化它们
    /// </summary>
    private List<List<GameTaskEntity>> _tasksEntities = [];


    public Dictionary<string, object> FlowData { get; set; } = [];

    /// <summary>
    /// 这个用来标记, 任务流是否是唯一的,条件是Name
    /// </summary>
    public bool Unique { get; set; } = true;

    //这里代表着一个任务流的顺序列表, 每个元素可能是一个并行集合
    private GameTask[][] Tasks { get; set; } = [];


    // //TODO 这个东西应该删掉, 放在主对象中也没什么问题的
    // public GameTaskStatus Status { get; set; } = new();


    public int[][] TaskProgress { get; private set; } = [];

    public bool IsStarted { get; private set; }

    public bool IsDestroyed { get; private set; }

    public int CurrentIndex { get; private set; } = 0;


    public TaskWorkflowChainEntity? Chain { get; set; } = null;

    #endregion


    //这是一个静态的,存储着正在运行的任务流的容器🤔,应该干掉
    public static readonly Dictionary<(string, int), GameTaskWorkflow> RunningTaskWorkFlow = [];


    #region 生命周期相关

    /// <summary>
    /// 这个Token用于终止所有任务
    /// </summary>
    public CancellationTokenSource Cts = new();

    // 这是几个任务流的生命周期
    public event Action<string, int> OnDestroy = delegate { };
    public event Action<string, int> OnError = delegate { };
    public event Action<string, int> OnAfterStart = delegate { };
    public event Action<string, int> OnBeforeStart = delegate { };
    public event Action<string, int> OnComplete = delegate { };


    //当进度更新时
    public event Action<int[], int> OnUpdateProgress = (_, _) => { };


    //当Task发出一个结果行时
    public event Action<int[], string, string, int> OnResultLine = delegate { };

    #endregion


    #region 初始化相关的任务列表字段, 分为元组初始化和实体初始化

    //使用元组的场景,多为内部调用的固定任务流; 实体的话,就是数据库中持久化可修改的任务流.
    public List<List<(string name, Dictionary<string, object>? dict)>> TaskTuples
    {
        set
        {
            if (Tasks.Length != 0 || value.Count <= 0) return;

            var gameTaskFactory = Services.Get<GameTaskFactory>();
            if (gameTaskFactory == null) return;

            //调用任务工厂,初始化这些任务.
            Tasks = value.Select((gameTasks, i) =>
                    gameTasks.Select((tuple, j) =>
                        gameTaskFactory.CreateGameTask(tuple.name, this, [i, j], tuple.dict, tuple.name)).ToArray())
                .ToArray();
        }
    }

    public List<List<GameTaskEntity>> TaskEntities
    {
        get => _tasksEntities;
        set
        {
            if (Tasks.Length != 0 || value.Count <= 0) return;

            var gameTaskFactory = Services.Get<GameTaskFactory>();
            if (gameTaskFactory == null) return;

            //调用任务工厂,初始化这些任务.
            Tasks = value.Select((gameTasks, i) =>
                    gameTasks.Select((entity, j) =>
                        gameTaskFactory.CreateGameTask(entity.Name, this, [i, j], entity.Args, entity.Title)).ToArray())
                .ToArray();

            _tasksEntities = value;
        }
    }

    #endregion


    #region 任务执行相关的逻辑

    /// <summary>
    /// Task通过调用这个,向外传递日志
    /// </summary>
    /// <param name="index"></param>
    /// <param name="title"></param>
    /// <param name="line"></param>
    /// <param name="level"></param>
    public void AppendResult(int[] index, string title, string line, int level = 1)
    {
        if (OnResultLine.GetInvocationList().Length > 1)
        {
            OnResultLine.Invoke(index, title, line, level);
        }

        Logger.Info(line);
    }

    /// <summary>
    /// 更新任务进度
    /// </summary>
    /// <param name="index"></param>
    /// <param name="progress"></param>
    public void UpdateProgress(int[] index, int progress)
    {
        OnUpdateProgress.Invoke(index, progress);

        if (index.Length != 2) return;


        CurrentIndex = index[0];

        TaskProgress[index[0]][index[1]] = progress;
    }

    /// <summary>
    /// 销毁任务流
    /// </summary>
    public void Destroy()
    {
        RunningTaskWorkFlow.Remove((Name, Id));
        OnDestroy(Name, Id);
        OnUpdateProgress = delegate { };
        OnResultLine = delegate { };
        OnDestroy = delegate { };
        OnAfterStart = delegate { };
        OnError = delegate { };
        OnBeforeStart = delegate { };
        // Context.Clear();
    }


    /// <summary>
    /// 停止任务执行
    /// </summary>
    /// <param name="force"></param>
    /// <param name="error"></param>
    /// <returns></returns>
    public bool StopWorkflow(bool force = false, bool error = false)
    {
        AppendResult([-1, -1], "flow", "任务准备终止", 4);
        var taskWorkflow = this;
        if (!taskWorkflow.IsStarted) return false;


        try
        {
            foreach (var gameTasks in taskWorkflow)
            {
                if (!force)
                {
                    foreach (var gameTask in gameTasks.Where(gameTask => gameTask.Progress is -4))
                    {
                        AppendResult(gameTask.Index, gameTask.Title, $"有任务尚未结束: {gameTask.Title}", 4);
                        return false;
                    }
                }

                foreach (var gameTask in gameTasks)
                {
                    switch (gameTask.Progress)
                    {
                        case GameTask.TaskTagSkip:
                        case GameTask.TaskGhostSkip:
                        case GameTask.TaskSelfSkip:

                            try
                            {
                                gameTask.Destroy0();
                            }
                            catch (Exception e)
                            {
                                AppendResult(gameTask.Index, gameTask.Title, $"任务销毁失败 {gameTask.Name} {e}", 4);

                                throw;
                            }

                            break;
                        case >= GameTask.TaskComplete:
                            break;
                        case > GameTask.TaskDefault:
                            gameTask.Destroy0();
                            break;
                        case GameTask.SkipAllTask:
                            break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            AppendResult([-1, -1], "flow", $"任务销毁失败,请检查任务实现 \n{e}", 4);
        }


        try
        {
            if (error)
            {
                OnError.Invoke(Name, Id);
            }
            else
            {
                OnComplete.Invoke(Name, Id);
            }
        }
        catch (Exception e)
        {
            AppendResult([-1, -1], "flow", $"执行回调失败 \n{e}", 4);
        }

        AppendResult([-1, -1], "flow", $"任务已终止 {taskWorkflow.Id} {taskWorkflow.Name}", 4);
        RunningTaskWorkFlow.Remove((Name, Id));
        taskWorkflow.IsStarted = false;
        taskWorkflow.IsDestroyed = true;
        taskWorkflow.Cts.Cancel();

        return true;
    }

    private bool BeforeStart()
    {
        if (Id == 0) Id = this.RandomFlowId();

        foreach (var (name, id) in RunningTaskWorkFlow.Keys)
        {
            if (name == Name && Unique)
            {
                Logger.Warn("任务名称重复");
                return false;
            }
        }

        RunningTaskWorkFlow[(Name, Id)] = this;
        OnBeforeStart.Invoke(Name, Id);


        if (Cts.IsCancellationRequested)
        {
            Cts = new CancellationTokenSource();
        }


        var taskWorkflow = this;

        //这里是先初始化进度条信息
        var list = new int[taskWorkflow.Count][];
        for (var i = 0; i < taskWorkflow.Count; i++)
        {
            var tasks = taskWorkflow[i];
            list[i] = new int[tasks.Length];

            for (var j = 0; j < tasks.Length; j++)
            {
                list[i][j] = 0;
            }
        }

        taskWorkflow.IsStarted = true;
        taskWorkflow.IsDestroyed = false;
        taskWorkflow.TaskProgress = list;
        taskWorkflow.CurrentIndex = 0;

        return true;
    }


    /// <summary>
    /// 开始执行任务
    /// </summary>
    public void Start()
    {
        if (IsStarted) return;

        if (!BeforeStart())
        {
            AppendResult([-1, -1], "flow", $"任务无法完成初始化", 4);
            return;
        }

        IsStarted = true;

        //这里只执行任务流中的第一组任务,并行运行
        this.RunNextTasks(this[0], 0);

        OnAfterStart(Name, Id);
    }


    public void SkipAllTask()
    {
        StopWorkflow();
    }

    /// <summary>
    /// 这里是task进行到一定程度时,调用的方法, 是一个静态方法, 它会自己寻找任务管理器实例, 然后
    /// </summary>
    /// <param name="index"></param>
    /// <param name="progress"></param>
    public void CompleteTask(int[] index, int progress)
    {
        var workFlow = this;
        workFlow.UpdateProgress(index, progress);

        switch (progress)
        {
            //100 代表完成,执行下一个task, 如果后面没有task, 则触发工作流完成逻辑, 存入历史记录, 并清除map中的缓存
            case GameTask.TaskTagSkip: //skip
            // case GameTask.TaskGhostSkip: //这个是什么都不做, 等待任务自己变更状态
            case GameTask.TaskSelfSkip: //backend
            case GameTask.TaskComplete:
            {
                var statusCurrentIndex = workFlow.CurrentIndex;

                if (workFlow.TaskProgress[statusCurrentIndex].Any(l => l == GameTask.TaskError))
                {
                    Logger.Debug("有任务失败了");
                    return;
                }

                //即进度小于100,或是什么都不做的, 都视为未完成
                if (!workFlow.TaskProgress[statusCurrentIndex].All(l =>
                        l is >= GameTask.TaskComplete or GameTask.TaskTagSkip or GameTask.TaskSelfSkip))
                {
                    Logger.Debug("还有任务需要完成");
                    return;
                }

                if (!this.IndexExists([index[0], index[1]], out _))
                {
                    return;
                }

                var nextTasks = this.GetNextTasks(index, out var finished, out var nextIndex);
                if (nextTasks.Length > 0)
                {
                    Logger.Debug($"执行下一组任务: {nextTasks.Length}");
                    this.RunNextTasks(nextTasks, nextIndex);
                }
                else
                {
                    if (finished)
                    {
                        Logger.Debug("任务流执行完毕");
                        StopWorkflow();
                    }
                }

                break;
            }
            //-1 代表失败
            case GameTask.TaskError:
                //TODO 失败时也触发工作流完成逻辑, 存入历史记录
                StopWorkflow();
                break;
        }
    }

    #endregion


    #region Enumerator相关的扩展,用于把任务流当作数组来使用

    public GameTask this[int i, int j] => Tasks[i][j];

    public GameTask this[int[] i] => Tasks[i[0]][i[1]];
    public GameTask this[Vector2I i] => Tasks[i.X][i.Y];

    public GameTask[] this[int i] => Tasks[i];

    public int Count => Tasks.Length;

    public IEnumerator<GameTask[]> GetEnumerator()
    {
        return ((IEnumerable<GameTask[]>)Tasks).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion

    #region 上下文

    public GameTaskContext Context { get; set; } = new();

    #endregion

    public void PutTag(string tag) => Context.PutTag(Id, tag);
}