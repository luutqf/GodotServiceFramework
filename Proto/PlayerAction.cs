using ProtoBuf;

namespace GodotServiceFramework.Proto;

[ProtoContract]
public class PlayerAction
{
    [ProtoMember(1)] public string ActionType { get; set; } = "";
    
    [ProtoMember(2)] public string SubType { get; set; } = "";

    [ProtoMember(3)] public Dictionary<string, string> Dict { get; set; } = new();

    [ProtoMember(4)] public Dictionary<string, int> Nums { get; set; } = new();

    [ProtoMember(5)] public Dictionary<string, bool> Switches { get; set; } = new();

    [ProtoMember(6)] public Dictionary<string, string> Args { get; set; } = new();
}