using System;
using System.Collections.Generic;

namespace GodotServiceFramework.Udp;

public class UdpMessageCache
{
    private event Action<ActionType>? OnCacheUpdate;

    private readonly Dictionary<ActionType, Dictionary<string, string>> _cacheMap = new();

    private readonly Dictionary<ActionType, Action<Dictionary<string, string>>> _respHandlers = new();


    public UdpMessageCache()
    {
        OnCacheUpdate += type =>
        {
            if (!_respHandlers.TryGetValue(type, out var action)) return;
            action(_cacheMap[type]);
        };
    }


    public void PutServerResp(ActionType type, Dictionary<string, string> dict)
    {
        _cacheMap[type] = dict;
        OnCacheUpdate?.Invoke(type);
    }

    public Dictionary<string, string> GetServerResp(ActionType type)
    {
        return _cacheMap[type];
    }


    public void RegisterHandler(ActionType type, Action<Dictionary<string, string>> action)
    {
        _respHandlers.TryAdd(type, action);
    }

    public void RemoveHandler(ActionType type)
    {
        _respHandlers.Remove(type);
    }


}

public record ActionType(string Type, string SubType = "");