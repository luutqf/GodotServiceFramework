using Newtonsoft.Json;
using SQLite;

namespace GodotServiceFramework.GTaskV2.Entity;

[Table(name: "tb_task_template")]
public class GTaskTemplate
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string ParametersJson { get; set; } = "{}";

    [Ignore]
    public Dictionary<string, object> Parameters =>
        JsonConvert.DeserializeObject<Dictionary<string, object>>(ParametersJson)!;
}