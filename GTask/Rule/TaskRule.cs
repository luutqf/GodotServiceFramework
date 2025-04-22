namespace GodotServiceFramework.GTask.Rule;

/// <summary>
/// 任务规则, 
/// </summary>
public class TaskRule
{
    public required string Name { get; set; }

    public bool FastError { get; set; } = true;

    public string Cron { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty; // Dynamic LINQ表达式

    public int Priority { get; set; }

    public required string Action { get; set; } // 可以是动作标识符或序列化的动作, 默认为任务流
}