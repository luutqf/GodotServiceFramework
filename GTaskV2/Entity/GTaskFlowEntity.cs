using Godot;
using GodotServiceFramework.Binding;
using GodotServiceFramework.GTaskV2.Model;
using GodotServiceFramework.Util;
using Newtonsoft.Json;
using SQLite;

namespace GodotServiceFramework.GTaskV2.Entity;

[Table(name: "tb_task_flow")]
public partial class GTaskFlowEntity : RefCounted, IBinding
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    public string Description { get; set; }

    [Unique(Name = "name")] public string Name { get; set; } = string.Empty;

    public long FirstNodeId { get; set; }

    // /// 标记最后一个节点,不一定是真的最后一个, 但为了链接其他任务,设置它    但其实不需要,因为我不需要向所谓的最后一个节点添加, 而是向当前节点之后添加即可. 或者直接运行
    // public long LastNodeId { get; set; }

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
                    try
                    {
                        nextModels[i] = models.First(m => m.Id == model.NextIds[i]);
                    }
                    catch (Exception e)
                    {
                        Log.Warn($"GTaskFlowEntity.Models: Next model not found for ID: {model.NextIds[i]}",
                            BbColor.Red);
                    }
                }

                model.NextModels = nextModels;
            }

            return models;
        }
    }
}