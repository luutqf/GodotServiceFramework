using Godot;
using Godot.Collections;
using GodotServiceFramework.Binding;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Context.Session;
using GodotServiceFramework.Data;
using GodotServiceFramework.Extensions;
using GodotServiceFramework.Util;
using AutoGodotService = GodotServiceFramework.Context.Service.AutoGodotService;

namespace GodotServiceFramework.Nodes;

// [AutoGlobalService]
public partial class SceneStatsManager : AutoGodotService
{
    //当前焦点所在的节点
    private readonly System.Collections.Generic.Dictionary<Type, Node?> _currentFocusDict = [];

    private readonly System.Collections.Generic.Dictionary<Type, long> _lastFocusTimes = [];

    //当前在最前面的页面, 也是焦点所在的页面
    private Control? _currentPage;

    //激活的页面, 页面句柄是唯一的.
    private readonly System.Collections.Generic.Dictionary<string, Control> _activePages = [];

    //最小化的页面
    private readonly System.Collections.Generic.Dictionary<string, Control> _minimizePages = [];

    //打包的场景或页面
    private readonly System.Collections.Generic.Dictionary<string, PackedScene> _packedScenes = [];


    public static event Action<string> OnPageChanged = _ => { };
    public static event Action<string> OnSceneChanged = _ => { };

    public override void _EnterTree()
    {
        if (Engine.GetMainLoop() is not SceneTree sceneTree) return;


        sceneTree.NodeAdded += source =>
        {
            //TODO 这里需要管理是否需要sessionId
            var node = source;
            while (true)
            {
                if (node == null) return;

                if (SessionManager.SessionIdMap.ContainsKey(node.GetInstanceId()))
                {
                    SessionManager.SessionIdMap[source.GetInstanceId()] = node.GetInstanceId();
                    return;
                }

                node = node.GetParent();
            }
        };
    }

    public override void _Ready()
    {
        Log.Info("场景管理器已加载");
    }

    public Node? DefaultParent
    {
        get => _defaultParent;
        set
        {
            if (_defaultParent != null)
            {
                CloseAllPage();
            }

            _defaultParent = value;
        }
    }

    //页面链表, 当新打开一个
    private LinkedList<Node> _pageLink = [];
    private Node? _defaultParent;


    public PackedScene LoadScene(string path)
    {
        var packedScene = GD.Load<PackedScene>(path);
        var name = packedScene._Bundled["names"].As<Array<string>>()[0];

        name = name.ToLower();
        _packedScenes.TryAdd(name, packedScene);
        return packedScene;
    }


    public bool HasScene(string name)
    {
        name = name.ToLower();
        return _packedScenes.ContainsKey(name);
    }

    public T InstantiatePack<T>() where T : Node
    {
        if (!_packedScenes.TryGetValue(typeof(T).Name.ToLower(), out var scene)) throw new KeyNotFoundException();

        return scene.Instantiate<T>();
    }

    public void UnloadScene(string name)
    {
        _packedScenes.Remove(name.ToLower());
    }

    public bool ChangeScene(string name)
    {
        name = name.ToLower();
        if (!_packedScenes.TryGetValue(name, out var value)) return false;
        var sceneTree = Services.GetSceneTree();
        if (sceneTree == null) return false;

        if (sceneTree.CurrentScene.Name == name) return true;

        var changeSceneToPacked = sceneTree.ChangeSceneToPacked(value);

        if (changeSceneToPacked == Error.Ok)
        {
            OnSceneChanged.Invoke(name);
            return true;
        }

        Log.Error(changeSceneToPacked);
        return false;
    }


    /// <summary>
    /// 加载一个场景, 但不显示, 对象返回
    /// </summary>
    /// <param name="name"></param>
    /// <param name="args"></param>
    /// <param name="readyCallBack"></param>
    public T InstantiateScene<T>(string name, object? args = null, Action<T>? readyCallBack = null) where T : Node
    {
        return InstantiateScene1(name, args, readyCallBack);
    }

    public Node InstantiateScene(string name, object? args = null, Action<Node>? readyCallBack = null)
    {
        return InstantiateScene0(name, args, readyCallBack);
    }

    public T InstantiateScene<T>(object? args = null, Action<T>? readyCallBack = null) where T : Node
    {
        return InstantiateScene1(typeof(T).Name, args, readyCallBack);
    }

    public Node InstantiateScene0(string name, object? args, Action<Node>? readyCallBack)
    {
        var instantiate = _packedScenes.TryGetValue(name.ToLower(), out var packedScene)
            ? packedScene.Instantiate()
            : throw new Exception("???");

        SwitchBindData(args, instantiate, readyCallBack);

        return instantiate;
    }

    public T InstantiateScene1<T>(string name, object? args, Action<T>? readyCallBack) where T : Node
    {
        var instantiate = _packedScenes.TryGetValue(name.ToLower(), out var packedScene)
            ? packedScene.Instantiate<T>()
            : throw new Exception("???");

        SwitchBindData(args, instantiate, readyCallBack);

        return instantiate;
    }

    private static void SwitchBindData<T>(object? args, T instantiate, Action<T>? readyCallBack) where T : Node
    {
        switch (args)
        {
            case Variant variant:
                instantiate.Connect(Node.SignalName.Ready, Callable.From(() =>
                {
                    instantiate.InitBindData(variant);
                    readyCallBack?.Invoke(instantiate);
                }));
                break;
            // ReSharper disable once SuspiciousTypeConversion.Global
            case IBinding data when instantiate is IDataNode node:
                instantiate.Connect(Node.SignalName.Ready, Callable.From(() =>
                {
                    node.InitBindData(data);
                    readyCallBack?.Invoke(instantiate);
                }));
                break;
            default:
                instantiate.Connect(Node.SignalName.Ready,
                    Callable.From(() => { readyCallBack?.Invoke(instantiate); }));
                break;
        }
    }

    public Node InstantiateScene(Type type)
    {
        return _packedScenes.TryGetValue(type.Name.ToLower(), out var packedScene)
            ? packedScene.Instantiate<Node>()
            : throw new Exception("???");
    }


    private Control InstantiatePage(string name)
    {
        return _packedScenes.TryGetValue(name.ToLower(), out var packedScene)
            ? packedScene.Instantiate<Control>()
            : throw new Exception("???");
    }


    /// <summary>
    /// 页面名称必须是唯一的, 打开这个页面后, 设置为当前页面
    /// </summary>
    /// <param name="parent"></param>
    /// <exception cref="ArgumentException"></exception>
    public T OpenPage<T>(Node? parent = null) where T : Control
    {
        T page;
        var name = typeof(T).Name.ToLower();
        if (!_activePages.TryGetValue(name, out var activePage))
        {
            //自动设置默认父节点
            if (parent != null && DefaultParent == null)
            {
                DefaultParent = parent;
            }

            parent ??= DefaultParent;
            if (parent == null) throw new ArgumentException("parent is null");

            page = InstantiateScene<T>(name);

            parent.AddChild(page);
        }
        else
        {
            page = (T)activePage;
        }

        if (page is Control control)
        {
            control.Visible = true;
            control.Position = new Vector2(200, 200);
        }

        _activePages.Add(page.Name.ToString().ToLower(), page);
        ChangePage(page);
        return page;
    }


    public void OpenPage(string name)
    {
        Control page;
        name = name.ToLower();
        if (!_activePages.TryGetValue(name, out var activePage))
        {
            if (DefaultParent == null) throw new ArgumentException("parent is null");

            page = InstantiatePage(name);

            DefaultParent.AddChild(page);
        }
        else
        {
            page = activePage;
        }

        page.Visible = true;
        page.Position = new Vector2(200, 200);


        _activePages.Add(page.Name.ToString().ToLower(), page);
        // return page;
        ChangePage(page);
    }

    public void ChangePageByDrag(Node page)
    {
        if (!_activePages.TryGetValue(page.Name.ToString().ToLower(), out var control)) return;

        if (_currentPage != null && _currentPage == control)
        {
            return;
        }

        _currentPage = control;
    }

    /// <summary>
    /// 设置当前页面, 主要是让其排序调整到最顶. 后续可以考虑新增一个信号
    /// </summary>
    /// <param name="page"></param>
    /// <param name="autoMini"></param>
    // ReSharper disable once MemberCanBePrivate.Global
    public void ChangePage(Node page, bool autoMini = false)
    {
        ChangePage0(page.Name.ToString().ToLower(), autoMini);
    }

    //
    public void ChangePage(string pageName, bool autoMini = false)
    {
        pageName = pageName.ToLower();
        ChangePage0(pageName, autoMini);
        OnPageChanged.Invoke(pageName);
    }

    private void ChangePage0(string name, bool autoMini = false)
    {
        name = name.ToLower();
        try
        {
            if (!_activePages.TryGetValue(name, out var page))
            {
                if (!_minimizePages.Remove(name, out page))
                {
                    OpenPage(name);
                    return;
                }

                _activePages.Add(name, page);
            }

            if (Equals(_currentPage, page))
            {
                if (autoMini)
                    MinimizePage();
                return;
            }

            //如果不是当前页面, 将page的排序调整到最前
            var parent = page.GetParent();
            parent?.MoveChild(page, parent.GetChildCount() - 1);
            _currentPage = page;
            _currentPage.Visible = true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    /// 获取当前页面
    /// </summary>
    /// <returns></returns>
    public Node? GetCurrentPage()
    {
        return _currentPage;
    }

    /// <summary>
    /// 最小化页面, 这里暂时直接隐藏
    /// </summary>
    /// <param name="page"></param>
    public void MinimizePage(Control? page = null)
    {
        if (page == null)
        {
            page = _currentPage;
            _currentPage = null;
        }
        else
        {
            if (Equals(page, _currentPage))
            {
                _currentPage = null;
            }
        }

        if (page == null) return;

        page.Visible = false;


        _activePages.Remove(page.Name.ToString().ToLower());
        _minimizePages.Add(page.Name.ToString().ToLower(), page);

        //TODO 在屏幕下面或其他地方保留一个恢复按钮
    }


    /// <summary>
    /// 关闭页面, 将页面节点从树中删除
    /// </summary>
    /// <param name="page"></param>
    public void ClosePage(Node page)
    {
        if (page is Control control)
        {
            control.Visible = false;
        }

        _activePages.Remove(page.Name.ToString().ToLower());
        _minimizePages.Remove(page.Name.ToString().ToLower());
        page.GetParent().RemoveChild(page);
    }

    public void CloseAllPage()
    {
        foreach (var pair in _minimizePages.Concat(_activePages))
        {
            pair.Value.GetParent().RemoveChild(pair.Value);
        }

        _minimizePages.Clear();
        _activePages.Clear();
    }


    public void SetCurrentFocus<T>(Node? node)
    {
        Node? currentFocus = null;

        var type = typeof(T);
        if (_currentFocusDict.TryGetValue(type, out var value))
        {
            currentFocus = value;
        }


        if (Equals(currentFocus, node))
        {
            _lastFocusTimes[type] = DateTime.Now.Ticks;
            return;
        }

        //先取消前一个节点的选中状态
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (currentFocus is ISelectable selectable1)
        {
            selectable1.Select(false);
        }

        _currentFocusDict[type] = node;
        _lastFocusTimes[type] = DateTime.Now.Ticks;

        //启用后一个选择状态
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (node is ISelectable selectable2)
        {
            selectable2.Select(true);
        }
    }

    public T? GetCurrentFocus<T>(out long lastTime) where T : Node
    {
        var type = typeof(T);
        if (_currentFocusDict.TryGetValue(type, out var value))
        {
            lastTime = _lastFocusTimes[type];
            return (T?)value;
        }

        lastTime = 0;
        return null;
    }

    public override void Destroy()
    {
    }
}