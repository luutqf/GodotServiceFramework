using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;

namespace GodotServiceFramework.GTaskV3.Config;

public class LinqCustomProvider : DefaultDynamicLinqCustomTypeProvider
{
    
    public LinqCustomProvider(ParsingConfig config, IList<Type> additionalTypes, bool cacheCustomTypes = true) : base(
        config, additionalTypes, cacheCustomTypes)
    {
        foreach (var additionalType in additionalTypes)
        {
            base.GetCustomTypes().Add(additionalType);
        }
    }

    public override HashSet<Type> GetCustomTypes()
    {
        var result = base.GetCustomTypes();
        result.Add(typeof(GTaskContext));
        return result;
    }
}