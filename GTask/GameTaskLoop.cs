using Godot;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.GTask.Rule;
using GodotServiceFramework.GTask.Service;
using Timer = Godot.Timer;


namespace GodotServiceFramework.GTask;

/// <summary>
/// 这里有一个定时器, 循环检查规则引擎
///
/// 这里还需要添加cron任务, 向一个Quartz中添加多个触发规则, 然后执行单独的规则评估
/// </summary>
public class GameTaskLoop
{
    private Timer? _myTimer;

    private static int Delay => 10;


    //全部的任务流都使用同一个上下文
    private readonly GameTaskContext _currentContext = new();

    public readonly Dictionary<TaskRule, GameTaskWorkflow> WorkflowsData = new();


    public void AddTask(TaskRule taskRule)
    {
        WorkflowsData.Add(taskRule, Services.Get<TaskWorkflowService>()!.GetTaskWorkflow(taskRule.Action));
    }

    //定时任务和任务流的名字绑定, 定时触发后, 仍需要验证rule. 以及当前任务的状态
    private List<(string flowName, string cron)> _crons = [];

    //用这个序列化上面的组合
    public string WorkflowsJson { get; set; } = "[]";

    //这里可以独立设置session
    public void SetSessionId(ulong sessionId)
    {
        _currentContext.SessionId = sessionId;
    }

    public Dictionary<string, object> CommonArgs
    {
        get => _currentContext.CommonArgs;
        set => _currentContext.CommonArgs = value;
    }

    //构造函数
    public GameTaskLoop()
    {
        _currentContext.OnTag += OnTag;
    }

    public void Start()
    {
        // 创建 Timer 节点
        _myTimer = new Timer();

        // 设置 Timer 属性
        _myTimer.WaitTime = Delay;
        _myTimer.OneShot = false; // 设置为循环（非一次性）
        _myTimer.Autostart = true; // 自动开始
        _myTimer.Timeout += OnTimeout;
        Services.Get<MyTimer>()!.CallDeferred(Node.MethodName.AddChild, _myTimer);

        OnTimeout();
    }

    private void OnTimeout()
    {
        ExecuteAll();
    }

    /// <summary>
    /// 执行, 这里评估规则并执行对应的任务流. 外部执行这个方法的契机是管理器的定时器回调
    ///
    /// 如果一个任务流已经启动, 则直接返回
    /// </summary>
    public void ExecuteAll()
    {
        var ruleEngine = Services.Get<DynamicRuleEngineManager>()!.Instance;

        ruleEngine.EvaluateAndExecute(_currentContext, rule =>
        {
            lock (rule)
            {
                if (rule.FastError && _currentContext.ErrorCounts.TryGetValue(rule.Action, out var errorCount) &&
                    errorCount > 2)
                    return;


                var flow = WorkflowsData[rule];
                if (flow.IsStarted)
                {
                    return;
                }

                flow.Reset();
                flow.Context = _currentContext;

                flow.Start();
                flow.Context.PutTag($"{flow.Name} started");
            }
        }, WorkflowsData.Keys.ToList());
    }

    /// <summary>
    /// 评估单个rule, 适用于定时执行, 规则是否匹配是必要条件
    /// </summary>
    /// <param name="rule"></param>
    public void Execute(TaskRule rule)
    {
        var ruleEngine = Services.Get<DynamicRuleEngineManager>()!.Instance;

        ruleEngine.EvaluateAndExecute(_currentContext, _ =>
        {
            var flow = WorkflowsData[rule];

            if (flow.IsStarted)
            {
                return;
            }

            flow.Reset();

            flow.Start();
        }, [rule]);
    }

    /// <summary>
    /// 当任务流触发tag时, 也要执行评估流程
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="id"></param>
    public void OnTag(string tag, int id)
    {
        ExecuteAll();
    }
}