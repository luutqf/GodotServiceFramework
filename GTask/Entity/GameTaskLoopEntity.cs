using Godot;
using GodotServiceFramework.Binding;
using GodotServiceFramework.GTask.Rule;
using Newtonsoft.Json;
using SQLite;

namespace GodotServiceFramework.GTask.Entity;

[Table("game_task_loop_entity")]
public partial class GameTaskLoopEntity : RefCounted, IBinding
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Unique(Name = "name")] public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string CommonArgsJson { get; set; } = "{}";

    public string TaskRulesJson { get; set; } = "[]";

    //0代表不会终结,  其他正数代表未执行任何任务的周期数
    public int AutoStop { get; set; } = 0;

    public GameTaskLoop ToTaskLoop()
    {
        var loop = new GameTaskLoop(Name)
        {
            CommonArgs = JsonConvert.DeserializeObject<Dictionary<string, object>>(CommonArgsJson)!,
            AutoStop = AutoStop,
        };

        var taskRules = JsonConvert.DeserializeObject<TaskRule[]>(TaskRulesJson);
        if (taskRules != null)
        {
            foreach (var taskRule in taskRules)
            {
                loop.AddTask(taskRule);
            }
        }


        return loop;
    }
}