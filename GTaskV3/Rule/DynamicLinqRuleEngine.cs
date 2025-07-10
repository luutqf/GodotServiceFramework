using System.Linq.Dynamic.Core;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.GTaskV3.Config;

namespace GodotServiceFramework.GTaskV3.Rule;

// 任务规则引擎类
[InjectService]
public class DynamicLinqRuleEngine
{
    public DynamicLinqRuleEngine()
    {
        ParsingConfig.Default.CustomTypeProvider = new LinqCustomProvider(ParsingConfig.Default, []);
    }

}