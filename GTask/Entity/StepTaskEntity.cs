using Godot;
using SQLite;

namespace GodotServiceFramework.GTask.Entity;

[Table("step_task")]
public partial class StepTaskEntity : RefCounted
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Desc { get; set; } = string.Empty;

    public string AppName { get; set; } = string.Empty;


    public string TaskFlowNames { get; set; } = string.Empty;
}