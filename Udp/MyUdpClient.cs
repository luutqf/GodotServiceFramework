using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Godot;
using GodotServiceFramework.Context;
using GodotServiceFramework.Context.Service;
using ProtoBuf;
using GodotServiceFramework.Proto;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.Udp;

// [AutoGlobalService]
public partial class MyUdpClient : Node, ICloseable
{
    public static MyUdpClient? Instance = Services.Get<MyUdpClient>()!;

    private readonly UdpClient _udpClient = new();

    private readonly Dictionary<Endpoint, IPEndPoint> _remoteEndPoints = new();

    private readonly Dictionary<string, Action<ServerResp>> _serverRespHandlers = [];

    private Endpoint? MainEndpoint { get; set; }


    private readonly UdpMessageCache _cache;

    public bool Receiving { get; set; }

    public bool CanReceive { get; set; } = true;

    public void SetMain(string remoteAddress, int remotePort)
    {
        MainEndpoint = new Endpoint(remoteAddress, remotePort);
    }

    public MyUdpClient(string remoteAddress, int remotePort)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            const uint iocIn = 0x80000000;
            const uint iocVendor = 0x18000000;
            const uint sioUdpConnReset = iocIn | iocVendor | 12;
            _udpClient.Client.IOControl(unchecked((int)sioUdpConnReset), [Convert.ToByte(false)], null);
        }

        AddEndpoint(remoteAddress, remotePort);

        _cache = new UdpMessageCache();


        SendProtoMessage(new PlayerAction()
        {
            ActionType = "Heartbeat"
        });
    }

    public MyUdpClient() : this("127.0.0.1", 7879)
    {
    }

    public Endpoint[] GetEndpoints()
    {
        return _remoteEndPoints.Keys.ToArray();
    }


    public void AddEndpoint(string remoteAddress, int remotePort)
    {
        var ipEndPoint = new IPEndPoint(IPAddress.Parse(remoteAddress), remotePort);
        var endpoint = new Endpoint(remoteAddress, remotePort);
        _remoteEndPoints.Add(endpoint, ipEndPoint);
        MainEndpoint ??= endpoint;
    }

    public void RemoveEndpoint(string remoteAddress, int remotePort)
    {
        var endpoint = new Endpoint(remoteAddress, remotePort);
        if (MainEndpoint == endpoint)
        {
            MainEndpoint = null;
        }

        _remoteEndPoints.Remove(endpoint);
    }

    public void SendProtoMessage(string type, string subType, Dictionary<string, string> dict)
    {
        SendProtoMessage(new PlayerAction
        {
            ActionType = type,
            SubType = subType,
            Dict = dict
        });
    }

    public void SendProtoMessage(object message)
    {
        if (MainEndpoint == null) return;

        if (!_remoteEndPoints.TryGetValue(MainEndpoint, out var value)) return;

        using var memoryStream = new MemoryStream();
        // 使用 protobuf-net 将 `PlayerAction` 对象序列化为二进制数据
        Serializer.Serialize(memoryStream, message);
        var data = memoryStream.ToArray();
        _udpClient.Send(data, data.Length, value);
    }

    public void RegisterMessageHandler(ActionType type, Action<Dictionary<string, string>> action)
    {
        _cache.RegisterHandler(type, action);
    }

    public void RegisterMessageHandler(string id, Action<ServerResp> action)
    {
        _serverRespHandlers[id] = action;
    }

    public void UnregisterMessageHandler(string id)
    {
        _serverRespHandlers.Remove(id);
    }

    public void RemoveHandler(ActionType type)
    {
        _cache.RemoveHandler(type);
    }


    public void ReceiveProtoMessageAsync(Action<ServerResp> handler)
    {
        if (Receiving) return;
        Receiving = true;

        Task.Run(async () =>
        {
            // if (Receiving) return;

            // Receiving = true;
            while (CanReceive)
            {
                // 等待接收 UDP 数据报文
                var receivedResult = await _udpClient.ReceiveAsync();
                var receivedData = receivedResult.Buffer;

                using var memoryStream = new MemoryStream(receivedData);

                // 使用 protobuf-net 反序列化数据为 `ServerResp` 对象
                var message = Serializer.Deserialize<ServerResp>(memoryStream);
                _ = Task.Run(() =>
                {
                    foreach (var action in _serverRespHandlers.Values)
                    {
                        action.Invoke(message);
                    }

                    handler.Invoke(message);

                    _cache.PutServerResp(new ActionType(message.RespType, message.SubType), message.Dict);
                });
            }

            Receiving = false;
            Console.WriteLine("stop receive message");
        });
    }

    public void ReceiveProtoMessage(Action<ServerResp> handler)
    {
        if (MainEndpoint != null) ReceiveProtoMessage(handler, MainEndpoint);
    }

    public void ReceiveProtoMessage(Action<ServerResp> handler, Endpoint endPoint)
    {
        if (Receiving) return;
        Receiving = true;

        Task.Run(() =>
        {
            // if (Receiving) return;

            // Receiving = true;
            while (CanReceive)
            {
                // 等待接收 UDP 数据报文
                if (!_remoteEndPoints.TryGetValue(endPoint, out var value))
                {
                    return;
                }

                var receivedResult = _udpClient.Receive(ref value);
                // var receivedData = receivedResult.Buffer;

                // using var memoryStream = new MemoryStream(receivedData);

                // 使用 protobuf-net 反序列化数据为 `ServerResp` 对象
                var message = Serializer.Deserialize<ServerResp>(new MemoryStream(receivedResult));
                Console.WriteLine($"message:{message}");
                _ = Task.Run(() =>
                {
                    _cache.PutServerResp(new ActionType(message.RespType, message.SubType), message.Dict);
                    message.Dict.Add("subType", message.SubType);
                    handler.Invoke(message);
                });
            }

            Receiving = false;
            Console.WriteLine("stop receive message");
        });
    }

    public void Close()
    {
        _udpClient.Close();
    }

    public static void RegisterCustomer(Action<Dictionary<string, string>> action, string type, string subType = "")
    {
        Services.Get<MyUdpClient>()?.RegisterMessageHandler(new ActionType(type, subType), action);
    }


    public static MyUdpClient Create(string remoteAddress, int remotePort, Action<ServerResp> handler
    )
    {
        var udpClient = new MyUdpClient(remoteAddress, remotePort);
        udpClient.ReceiveProtoMessageAsync(handler);
        udpClient.SetMain(remoteAddress, remotePort);
        return udpClient;
    }
}

public record Endpoint(string IpAddress, int Port);