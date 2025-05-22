using SQLite;

namespace GodotServiceFramework.GTaskV2.Entity;

[Table(name: "tb_task_loop")]
public class GTaskLoopEntity
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }
}