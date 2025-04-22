using Godot;
using GodotServiceFramework.Binding;
using SQLite;

namespace GodotServiceFramework.GTask.Entity;

public partial class GameTaskLoopEntity : RefCounted, IBinding
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    public string CommonArgsJson { get; set; } = "[]";

    public string TaskRulesJson { get; set; } = "[]";
    
    
}