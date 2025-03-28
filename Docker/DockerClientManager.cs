using Docker.DotNet;
using Godot;

namespace GodotServiceFramework.Docker;

/// <summary>
/// 这是dockerClient的管理器
/// </summary>
public partial class DockerClientManager : Node
{
    
    // private readonly Dictionary<string, DockerClient> _dockerClients = [];
    //
    // /// <summary>
    // /// 获取一个dockerClient
    // /// </summary>
    // /// <param name="remoteAddr"></param>
    // /// <returns></returns>
    // /// <exception cref="EntryPointNotFoundException"></exception>
    // public DockerClient Get(string remoteAddr)
    // {
    //     if (_dockerClients.TryGetValue(remoteAddr, out var value)) return value;
    //
    //     var uri = new Uri(remoteAddr);
    //
    //     var dockerClient = new DockerClientConfiguration(uri).CreateClient();
    //
    //     _dockerClients.Add(remoteAddr, dockerClient);
    //
    //     return dockerClient;
    // }
    //
    //
    // /// <summary>
    // /// 关闭客户端
    // /// </summary>
    // /// <param name="remoteAddr"></param>
    // /// <returns></returns>
    // public bool Close(string remoteAddr)
    // {
    //     if (!_dockerClients.Remove(remoteAddr, out var value)) return false;
    //
    //     value.Dispose();
    //     return true;
    // }
}