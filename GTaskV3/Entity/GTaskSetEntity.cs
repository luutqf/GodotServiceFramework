using GodotServiceFramework.Binding;
using GodotServiceFramework.Db;
using SQLite;

namespace GodotServiceFramework.GTaskV3.Entity;

[Table(name: "tb_task_set")]
public class GTaskSetEntity : IBinding
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Unique(Name = "name")]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("condition")] public string Condition { get; set; } = string.Empty;

    [Column("content")] public string TaskContentJson { get; set; } = "[]";

    [Column("parameters")] public string ParametersJson { get; set; } = "{}";
}