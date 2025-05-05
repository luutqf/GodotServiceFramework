using Godot;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.Context.Service;

public static class Services
{
    private static readonly List<string> RegisterNames = [];

    public static bool Has(string name)
    {
        return Engine.HasSingleton(name);
    }

    public static bool Has(Type type)
    {
        return Engine.HasSingleton(type.Name);
    }


    public static void Add(GodotObject obj)
    {
        Add(obj.GetType().Name, obj);
    }

    public static void Add(string name, GodotObject obj)
    {
        if (Engine.HasSingleton(name))
        {
            GD.PushWarning($"Duplicate singleton: {name}");
            return;
        }

        RegisterNames.Add(name);

        Engine.RegisterSingleton(name, obj);

        Task.Run(async () =>
        {
            if (obj is Node node && node.GetParent() == null)
            {
                for (var i = 0; i < 10; i++)
                {
                    if (GetSceneTree() == null)
                    {
                        await Task.Delay(1000);
                        continue;
                    }

                    // await NextProcessFrame(); 

                    GetSceneTree()?.Root.CallDeferred(Node.MethodName.AddChild, node);
                    break;
                }
            }
        });
    }

    public static bool TryGet<T>(out T? obj) where T : GodotObject
    {
        try
        {
            var foo = Get<T>();
            obj = foo;
            return obj != null;
        }
        catch (Exception e)
        {
            Log.Error(e);
            obj = null;
            return false;
        }
    }

    public static T? Get<T>() where T : GodotObject
    {
        return Get<T>(typeof(T).Name) ?? null;
    }

    public static T? Get<T>(string name) where T : GodotObject
    {
        if (!Engine.HasSingleton(name)) return null;

        return Engine.GetSingleton(name) as T;
    }

    public static GodotObject? Get(Type type)
    {
        return Get<GodotObject>(type.Name);
    }

    public static void Remove(string name)
    {
        if (!Has(name)) return;

        var obj = Get<GodotObject>(name);

        if (obj is IService service)
        {
            service.Destroy();
        }

        Engine.UnregisterSingleton(name);

        if (obj is Node node && node.GetParent() != null)
        {
            node.GetParent().CallDeferred(Node.MethodName.RemoveChild, node);
        }

        RegisterNames.Remove(name);
    }

    public static void Remove(Type type)
    {
        Remove(type.Name);
    }

    public static void Remove(object? instance)
    {
        if (instance != null) Remove(instance.GetType().Name);
    }

    public static void Destroy()
    {
        foreach (var name in RegisterNames.ToArray().Where(Has))
        {
            Remove(name);
        }
    }


    public static SceneTree? GetSceneTree()
    {
        return Engine.GetMainLoop() as SceneTree;
    }

    public static T GetRoot<T>() where T : Node
    {
        var rootNode = GetSceneTree()?.Root;
        return rootNode!.GetNode<T>($"/root/{typeof(T).Name}");
    }

    public static async Task NextProcessFrame()
    {
        var sceneTree = GetSceneTree();
        if (sceneTree == null) return;
        await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
    }
}