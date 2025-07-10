using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Db;
using GodotServiceFramework.GTaskV3.Entity;

namespace SigmusV3.GodotServiceFramework.GTaskV3.Service;

[InjectService]
public class GTaskSetService
{
    public GTaskSetService()
    {
        SqliteManager.Instance.AddTableTypes([typeof(GTaskSetEntity)]);
    }
}