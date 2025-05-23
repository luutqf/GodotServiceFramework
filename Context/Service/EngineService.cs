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

    /// <summary>
    /// 添加service, 标好名字
    /// </summary>
    /// <param name="name"></param>
    /// <param name="obj"></param>
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
                    
                    GetSceneTree()?.Root.CallDeferred(Node.MethodName.AddChild, node);
                    break;
                }
            }
        });
    }

    
    /// <summary>
    /// 尝试通过泛型获取service
    /// </summary>
    /// <param name="obj"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
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

    /// <summary>
    /// 通过名称获取服务
    /// </summary>
    /// <param name="name"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? Get<T>(string name) where T : GodotObject
    {
        if (!Engine.HasSingleton(name)) return null;

        return Engine.GetSingleton(name) as T;
    }

    /// <summary>
    /// 通过类型获取服务
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static GodotObject? Get(Type type)
    {
        return Get<GodotObject>(type.Name);
    }

    /// <summary>
    /// 根据名称删掉服务
    /// </summary>
    /// <param name="name"></param>
    public static void Remove(string name)
    {
        if (!Has(name)) return;

        var obj = Get<GodotObject>(name);

        Engine.UnregisterSingleton(name);

        if (obj is Node node && node.GetParent() != null)
        {
            node.GetParent().CallDeferred(Node.MethodName.RemoveChild, node);
        }

        RegisterNames.Remove(name);
    }

    /// <summary>
    /// 根据类型删掉服务
    /// </summary>
    /// <param name="type"></param>
    public static void Remove(Type type)
    {
        Remove(type.Name);
    }

    /// <summary>
    /// 根据实例删掉服务
    /// </summary>
    /// <param name="instance"></param>
    public static void Remove(object? instance)
    {
        if (instance != null) Remove(instance.GetType().Name);
    }

    /// <summary>
    /// 把缓存里的,用过的都删除掉,根据名字
    /// </summary>
    public static void Destroy()
    {
        foreach (var name in RegisterNames.ToArray().Where(Has))
        {
            Remove(name);
        }
    }


    /// <summary>
    /// 获取场景树
    /// </summary>
    /// <returns></returns>
    public static SceneTree? GetSceneTree()
    {
        return Engine.GetMainLoop() as SceneTree;
    }

    /// <summary>
    /// 获取root节点
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetRoot<T>() where T : Node
    {
        var rootNode = GetSceneTree()?.Root;
        return rootNode!.GetNode<T>($"/root/{typeof(T).Name}");
    }

    /// <summary>
    /// 等待下一帧
    /// </summary>
    public static async Task NextProcessFrame()
    {
        var sceneTree = GetSceneTree();
        if (sceneTree == null) return;
        await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
    }
}