using SigmusV2.GodotServiceFramework.Util;

namespace GodotServiceFramework.GTaskV2.Model;

/// <summary>
/// 用于在sqlite中json序列化
/// </summary>
public class GTaskModel
{
    //节点ID, 雪花生成吧
    public long Id { get; set; } = SnowflakeIdGenerator.NextId();

    //Name就是任务的唯一ID, 在factory中唯一生成
    public required string Name { get; set; }

    public required long[] NextIds { get; set; }

    public GTaskModel[] NextModels { get; set; } = [];

    public Dictionary<string, object> Parameters { get; set; } = [];


    public static readonly GTaskModel DefaultModel = new GTaskModel
    {
        Name = "Default",
        NextIds =
        [
        ]
    };
}