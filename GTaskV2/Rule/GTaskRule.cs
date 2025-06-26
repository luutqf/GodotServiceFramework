namespace SigmusV2.GodotServiceFramework.GTaskV2.Rule;

public class GTaskRule
{
    public required string Name { get; set; }

    public TaskRuleType Type { get; set; }
    
    public bool FastError { get; set; } = true;

    public string Cron { get; set; } = string.Empty;
    
    public string Condition { get; set; } = string.Empty; // Dynamic LINQ表达式

    public int Priority { get; set; }

    public required string Action { get; set; } // 可以是动作标识符或序列化的动作, 默认为任务流
}

public enum TaskRuleType
{
    TaskFlow
}