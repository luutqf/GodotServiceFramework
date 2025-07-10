using System.Diagnostics;
using Godot;
using GodotServiceFramework.Context.Service;

namespace GodotServiceFramework.Context.Session;

/// <summary>
/// 用于管理session节点
/// </summary>
[InjectService]
public partial class SessionManager : IDisposable
{
    public static readonly Dictionary<ulong, ulong> SessionIdMap = [];

    public static readonly List<ulong> MainSessionIds = [];

    public static readonly AsyncLocal<ulong> CurrentActivity = new()
    {
        Value = 0
    };


    public void Dispose()
    {
        SessionIdMap.Clear();
    }
}