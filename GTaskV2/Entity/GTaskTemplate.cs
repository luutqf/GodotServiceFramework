using GodotServiceFramework.Binding;
using Newtonsoft.Json;
using SQLite;

namespace SigmusV2.GodotServiceFramework.GTaskV2;

/// <summary>
/// 用户基于模板自定义任务, 包含任务参数, 和其他说明
/// </summary>
[Table("tb_g_task_entity")]
public class GTaskTemplate : IBinding
{
    #region 基础结构

    private Dictionary<string, string>? _args;

    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    public required string Name { get; set; }

    public string Desc { get; set; } = string.Empty;

    public string ArgsJson { get; set; } = "{}";

    [Ignore]
    public Dictionary<string, string> Args
    {
        get => (_args ??= JsonConvert.DeserializeObject<Dictionary<string, string>>(ArgsJson)) ?? [];
        set
        {
            _args = value;
            ArgsJson = JsonConvert.SerializeObject(value);
        }
    }

    #endregion
}