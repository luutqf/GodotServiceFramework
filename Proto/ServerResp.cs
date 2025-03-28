
using ProtoBuf;

namespace GodotServiceFramework.Proto;

[ProtoContract]
public class ServerResp
{
    // 对应 Protobuf 中的 `string respType = 1;`
    [ProtoMember(1)] public string RespType { get; set; } = "";
    
    [ProtoMember(2)] public string SubType { get; set; } = "";

    [ProtoMember(3)] public Dictionary<string, string> Dict { get; set; } = new();
}