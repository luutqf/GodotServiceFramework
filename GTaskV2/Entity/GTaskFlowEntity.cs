using Godot;
using GodotServiceFramework.Binding;
using GodotServiceFramework.GTaskV2.Model;
using Newtonsoft.Json;
using SQLite;

namespace GodotServiceFramework.GTaskV2.Entity;

[Table(name: "tb_task_flow")]
public partial class GTaskFlowEntity : RefCounted, IBinding
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Unique(Name = "name")] public string Name { get; set; } = string.Empty;

    public long FirstNodeId { get; set; }
    
    /// 标记最后一个节点,不一定是真的最后一个, 但为了链接其他任务,设置它
    public long LastNodeId { get; set; }

    [Ignore]
    public Dictionary<string, object> Parameters =>
        JsonConvert.DeserializeObject<Dictionary<string, object>>(ParamsJson)!;


    public string ParamsJson { get; set; } = "{}";
    public string Content { get; set; } = "[]";

    public GTaskModel[] Models
    {
        get
        {
            var models = JsonConvert.DeserializeObject<GTaskModel[]>(Content)!;
            foreach (var model in models)
            {
                var nextModels = new GTaskModel[model.NextIds.Length];
                for (var i = 0; i < model.NextIds.Length; i++)
                {
                    nextModels[i] = models.First(m => m.Id == model.NextIds[i]);
                }

                model.NextModels = nextModels;
            }

            return models;
        }
    }
}