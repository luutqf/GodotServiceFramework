using Godot;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Db;
using GodotServiceFramework.GTaskV2.Entity;
using SQLite;

namespace GodotServiceFramework.GTaskV2.Service;

/// <summary>
/// 基础的任务服务, 增删改查
/// </summary>
public partial class GTaskEntityService : AutoGodotService
{
    private readonly SQLiteConnection _db;

    public GTaskEntityService()
    {
        var globalizePath = ProjectSettings.GlobalizePath("user://data/db.sqlite");
        _db = SqliteTool.Db(globalizePath, out _,
            initTables:
            [
                typeof(GTaskFlowEntity)
            ]);
    }

    public GTaskFlowEntity GetFlowEntity(string flowName)
    {
        return _db.Table<GTaskFlowEntity>().First(entity => entity.Name == flowName);
    }
}