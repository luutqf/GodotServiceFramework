using Godot;
using GodotServiceFramework.Binding;
using GodotServiceFramework.Extensions;
using Newtonsoft.Json;
using SQLite;

namespace GodotServiceFramework.GTask.Entity;

/// <summary>
/// 这个是用于序列化的实体类, 不再基于workflow内置,免得麻烦
/// </summary>
[Table("game_task_entity")]
public partial class GameTaskEntity : RefCounted, IBinding
{
    private string _title = string.Empty;

    [Unique(Name = "title")]
    public string Title
    {
        get => string.IsNullOrEmpty(_title) ? Name : _title;
        set => _title = value;
    }

    public string Name { get; set; } = string.Empty;

    [Unique(Name = "title")] public string Group { get; set; } = string.Empty;

    public string ArgsJson { get; set; } = string.Empty;

    public IDictionary<string, object>? Args =>
        JsonConvert.DeserializeObject<Dictionary<string, object>>(ArgsJson)?.AddRange(OverrideArgs);


    [Ignore] public Dictionary<string, object> OverrideArgs { get; set; } = [];

    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }
}