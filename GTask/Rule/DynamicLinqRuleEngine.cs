using System.Linq.Dynamic.Core;
using GodotServiceFramework.GTask.Config;

namespace GodotServiceFramework.GTask.Rule;

// 规则引擎类
public class DynamicLinqRuleEngine<T>
{
    // 评估对象并返回触发的规则
    public IEnumerable<TaskRule> Evaluate(T subject, List<TaskRule> rules)
    {
        var subjectAsQueryable = new[] { subject }.AsQueryable();

        var triggeredRules = new List<TaskRule>();

        foreach (var rule in rules.OrderByDescending(r => r.Priority))
        {
            try
            {
                ParsingConfig.Default.CustomTypeProvider = new LinqCustomProvider(ParsingConfig.Default, []);

                // 使用Dynamic LINQ评估条件
                var isMatch = subjectAsQueryable.Any(rule.Condition);

                if (isMatch)
                {
                    triggeredRules.Add(rule);
                }
            }
            catch (Exception ex)
            {
                // 处理表达式错误
                Console.WriteLine($"规则评估错误 '{rule.Name}': {ex.Message}");
            }
        }

        return triggeredRules;
    }

    // 评估并执行动作
    public void EvaluateAndExecute(T subject, Action<TaskRule> actionExecutor, List<TaskRule> rules)
    {
        var triggeredRules = Evaluate(subject, rules);

        foreach (var rule in triggeredRules)
        {
            actionExecutor(rule);
        }
    }
}