using GodotServiceFramework.GTaskV2.Entity;
using GodotServiceFramework.GTaskV2.Model;

namespace GodotServiceFramework.GTaskV2;

/// <summary>
/// 任务状态机, 可以向其中添加任务或任务流. 当条件合适时, 触发对应的任务
///
/// 首先要设置必须要执行的初始任务流, 初始化完成后, 启动规则引擎定时器, 检索各个条件.
///
/// 如果条件合适, 则启动对应的
/// </summary>
public class GTaskStateMachine
{
    public string Name { get; set; }

    //状态机停止的条件
    public string StopCondition { get; set; } = "{}";

    //状态机启动的条件
    public string StartCondition { get; set; }

    //状态机初始化的任务流, 可以为空
    public List<GTaskFlow> InitFlows { get; set; } = [];

    //条件任务流,
    public List<(string condition, GTaskFlowEntity flow)> ConditionFlows { get; set; } = [];

    //条件任务
    public List<(string condition, GTaskModel task)> ConditionTasks { get; set; } = [];


    //全部的任务流都使用同一个上下文
    private readonly GTaskContext _currentContext = new();
}