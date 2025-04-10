using GodotServiceFramework.Binding;
using Newtonsoft.Json;
using SQLite;

namespace SigmusV2.GodotServiceFramework.GTaskV2;

/// <summary>
/// 视为任务组,最小编排单位, 包含若干任务, 触发条件等信息
/// </summary>
public class GTaskPod : IBinding
{
    #region 基础结构

    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Ignore]
    public int[] TaskIds
    {
        get => JsonConvert.DeserializeObject<int[]>(TaskIdsJson) ?? [];
        set => TaskIdsJson = JsonConvert.SerializeObject(value);
    }


    [Ignore]
    public Dictionary<string, string>[] Args
    {
        get => JsonConvert.DeserializeObject<Dictionary<string, string>[]>(ArgsJson) ?? [];
        set => ArgsJson = JsonConvert.SerializeObject(value);
    }

    public string TaskIdsJson { get; set; } = "[]";

    public string ArgsJson { get; set; } = "{}";


    #endregion

    #region 运行时结构

    public BaseGTask[] Tasks;
    /// <summary>
    /// 下一个Pod的Id, 
    /// </summary>
    public int NextPodId { get; set; }
    #endregion
}