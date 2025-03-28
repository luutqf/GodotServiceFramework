using Godot;
using GodotServiceFramework.Binding;
using Newtonsoft.Json;
using SQLite;

namespace GodotServiceFramework.GTask.Entity;

[Table("game_task_flow_entity")]
public partial class GameTaskFlowEntity : RefCounted, IBinding
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string TargetApp { get; set; } = string.Empty;

    public string TasksJson { get; set; } = string.Empty;

    //用于覆盖模板
    public string TaskArgsJson { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Ignore]
    public ulong SessionId
    {
        get => this.GetProperty<ulong>("sessionId");
        set => this.SetProperty("sessionId", value);
    }

    [Ignore]
    public int FlowId
    {
        get => this.GetProperty<int>("flowId");
        set => this.SetProperty("flowId", value);
    }

    public List<List<int>>? TaskIds => JsonConvert.DeserializeObject<List<List<int>>>(TasksJson);

    public List<List<string>>? TaskArgs => JsonConvert.DeserializeObject<List<List<string>>>(TaskArgsJson);


    [Ignore] public List<List<GameTaskEntity>> TaskEntities { get; set; } = [];
}