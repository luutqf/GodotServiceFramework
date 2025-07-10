using System.Reflection;
using Godot;
using GodotServiceFramework.Binding;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Exceptions;

namespace GodotServiceFramework.Util;

public static class NodeUtils
{
    public static bool IsChildOf(Node child, Node potentialParent)
    {
        Node current = child;

        while (current != null)
        {
            if (current == potentialParent)
                return true;

            current = current.GetParent();
        }

        return false;
    }


    public static void RemoveAllChild(this Node node, Type[]? ignoreTypes = null, string[]? ignoreNames = null)
    {
        foreach (var child in node.GetChildren())
        {
            if (ignoreTypes != null &&
                ignoreTypes.Any(i => child.GetType().IsAssignableFrom(i) || child.GetType() == i))
            {
                continue;
            }

            if (ignoreNames != null && ignoreNames.Any(i => child.Name == i))
            {
                continue;
            }

            node.RemoveChild(child);
        }
    }

    public static void RemoveAllChildDeferred(this Node node, Type[]? ignores = null)
    {
        foreach (var child in node.GetChildren())
        {
            if (ignores != null && ignores.Any(i => child.GetType().IsAssignableFrom(i) || child.GetType() == i))
            {
                continue;
            }

            node.CallDeferred(Node.MethodName.RemoveChild, child);
            child.CallDeferred(Node.MethodName.QueueFree);
        }
    }

    public static void HideAllChild(this Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is Control control)
            {
                control.Hide();
            }
        }
    }


    public static bool IsTrue(this Node node)
    {
        switch (node)
        {
            case Button button:
            {
                return button.IsPressed();
            }
        }

        return false;
    }


    public static void BindData(this Node node)
    {
    }


    public static void ConnectAllChildren(this Node node, Callable callable, string signalName = "gui_input",
        List<Type>? ignoredTypes = null)
    {
        GetAllChildNodes(node, ignoredTypes).ForEach(child =>
        {
            if (child is Control control)
            {
                control.Connect(signalName, callable);
            }
        });
    }

    private static List<Node> GetAllChildNodes(Node parent, List<Type>? ignoredTypes)
    {
        // 创建一个列表来存储所有子节点
        var childNodes = new List<Node>();

        //这里暂且try catch
        try
        {
            // 获取当前节点的所有子节点
            foreach (var child in parent.GetChildren())
            {
                if (ignoredTypes != null && ignoredTypes.Any(type => type == child.GetType()))
                {
                    continue;
                }

                childNodes.Add(child);

                // 递归获取每个子节点的子节点
                childNodes.AddRange(GetAllChildNodes(child, ignoredTypes));
            }
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to get child nodes: {e.Message} -> {parent.Name}");
        }


        return childNodes;
    }


    private static readonly Dictionary<WeakReference<Node>, WeakReference<object>> RootDict = [];


    /// <summary>
    /// 检查T是否是Attribute或其子类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool IsAttributeType<T>()
    {
        return typeof(Attribute).IsAssignableFrom(typeof(T));
    }

    /// <summary>
    /// 获取最近的父节点，根据属性找
    /// </summary>
    /// <param name="node"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Node? GetRootByAttribute<T>(this Node node)
    {
        while (true)
        {
            if (node == null) return null;

            var parent = node.GetParent();
            if (node.GetType().GetCustomAttributes(typeof(T), true).Length > 0)
            {
                RootDict.TryAdd(new WeakReference<Node>(node), new WeakReference<object>(parent));
                return parent;
            }

            node = parent;
        }
    }


    /// <summary>
    /// 通过类型获取最近的父节点
    ///
    /// 这里有一个缓存 TODO 要不要管理起来
    /// </summary>
    /// <param name="obj"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? GetRoot<T>(this object obj)
    {
        if (obj is not Node node) return default;
        if (node is T root) return root;

        if (RootDict.TryGetValue(new WeakReference<Node>(node), out var value))
        {
            if (value.TryGetTarget(out var target))
            {
                if (target is T t)
                {
                    return t;
                }
            }
        }

        while (true)
        {
            if (node == null) return default;
            if (node.GetParent() is T t)
            {
                RootDict.TryAdd(new WeakReference<Node>(node), new WeakReference<object>(t));
                return t;
            }

            node = node.GetParent();
        }
    }


    /// <summary>
    /// 设置一个节点的值? 没啥用
    /// </summary>
    /// <param name="this"></param>
    /// <param name="value"></param>
    public static void SetBindValue(this Node @this, object? value)
    {
        if (value == null) return;

        switch (@this)
        {
            case Label label:
            {
                if (label.GetText() == value.ToString())
                {
                    return;
                }

                label.CallDeferred(Label.MethodName.SetText, value.ToString() ?? string.Empty);
                break;
            }
            case LineEdit lineEdit:
            {
                if (lineEdit.GetText() == value.ToString())
                {
                    return;
                }

                lineEdit.CallDeferred(LineEdit.MethodName.SetText, Variant.From(value));
                break;
            }
        }
    }


    /// <summary>
    /// 获取一个node的值， 好像也没啥用
    /// </summary>
    /// <param name="this"></param>
    /// <returns></returns>
    public static object? GetNodeValue(this Node @this)
    {
        return @this switch
        {
            Label label => label.GetText(),
            LineEdit lineEdit => lineEdit.GetText(),
            OptionButton optionButton => optionButton.Selected,
            BaseButton button => button.IsPressed(),
            _ => null
        };
    }

    [Obsolete("在用吗?")]
    public static object? GetValue(this Node @this, DataType type = DataType.String)
    {
        object? result = @this switch
        {
            Label label => label.GetText(),
            LineEdit lineEdit => lineEdit.GetText(),
            _ => null
        };

        switch (type)
        {
            case DataType.String:
            {
                result = result?.ToString();
                break;
            }
            case DataType.Float:
            {
                if (result is not float)
                    if (float.TryParse((string?)result, out var f))
                    {
                        result = f;
                    }

                break;
            }
            case DataType.Int:
            {
                if (result is not int)
                    if (int.TryParse((string?)result, out int i))
                    {
                        result = i;
                    }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        return result;
    }


    /// <summary>
    /// 根据一个type创建一个controller
    /// 这里的问题是,只针对controller, 我们还没有实现Bean的依赖关系
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="TypeNotFoundException"></exception>
    public static object CreateInstanceForController(this Type type)
    {
        List<ParameterInfo> parameterInfos = [];
        foreach (var constructor in type.GetConstructors())
        {
            parameterInfos.AddRange(constructor.GetParameters());
            break;
        }

        //TODO 依赖注入
        // if (parameterInfos.Any(info => !info.ParameterType.IsSubclassOf(typeof(AutoGodotService))))
        // {
        //     throw new TypeNotFoundException(type.Name);
        // }

        List<object> paramList = [];
        paramList.AddRange(parameterInfos.Select(parameterInfo => Services.Get(parameterInfo.ParameterType))!);

        return Activator.CreateInstance(type, paramList.ToArray())!;
    }
}