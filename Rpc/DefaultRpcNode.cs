using Godot;
using GodotServiceFramework.Util;

namespace SigmusV3.GodotServiceFramework.Rpc;

public class DefaultRpcNode 
{
    // 所有客户端都能调用，在所有客户端执行
    // [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    // public void TestMessage(string message)
    // {
    //     Log.Info($"client message: {message}");
    // }
}