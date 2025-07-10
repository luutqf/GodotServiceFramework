using Godot;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Db;
using GodotServiceFramework.GTaskV2.Entity;
using SQLite;

namespace GodotServiceFramework.GTaskV2.Service;

/// <summary>
/// 基础的任务服务, 增删改查
/// </summary>
[InjectService]
public partial class GTaskEntityService
{
    public GTaskEntityService()
    {
        SqliteManager.Instance.AddTableTypes([typeof(GTaskFlowEntity)]);
    }

    public GTaskFlowEntity GetFlowEntity(string flowName)
    {
        return SqliteManager.Table<GTaskFlowEntity>().First(entity => entity.Name == flowName);
    }
}