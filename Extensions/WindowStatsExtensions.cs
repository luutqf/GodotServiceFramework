using Godot;
using GodotServiceFramework.Context;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Nodes;

namespace GodotServiceFramework.Extensions;

public static class WindowStatsExtensions
{
    public static T OpenPage<T>(this Node parent) where T : Control
    {
        return Services.Get<SceneStatsManager>()!.OpenPage<T>(parent);
    }
}