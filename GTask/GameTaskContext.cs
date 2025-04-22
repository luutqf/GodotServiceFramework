using GodotServiceFramework.GTask;

public class GameTaskContext
{
    public string Name { get; set; } = string.Empty;


    public Dictionary<string, int> ErrorCounts = [];

    public Dictionary<string, object> CommonArgs { get; set; } = [];

    public readonly HashSet<GameTaskWorkflow> Workflows = [];

    public GameTaskWorkflowState GetWorkflowState(string name)
    {
        return Workflows.First(workflow => workflow.Name == name).State;
    }

    public GameTaskContext()
    {
    }

    #region session相关,待定

    /// <summary>
    /// SessionId主要用于将运行状态,定位到一个合适的会话
    /// </summary>
    public ulong SessionId { get; set; }

    #endregion

    #region Cache, 缓存一些可能会用到的数据

    public readonly Dictionary<string, object> Data = [];

    #endregion

    #region Tags, 标签相关的操作, 涉及到事件传递

    public event Action<string, int> OnTag = delegate { };

    public readonly HashSet<string> FlowTags = [];

    public bool HasTag(string tag)
    {
        return FlowTags.Contains(tag);
    }

    public void PutTag(int flowId, string tag)
    {
        FlowTags.Add(tag);
        OnTag.Invoke(tag, flowId);
    }

    public void PutTag(string tag)
    {
        FlowTags.Add(tag);
        OnTag.Invoke(tag, -1);
    }

    public void PutTags(int flowId, params string[] tags)
    {
        foreach (var tag in tags)
        {
            FlowTags.Add(tag);
            OnTag.Invoke(tag, flowId);
        }
    }

    #endregion


    public void AddError(string name)
    {
        if (!ErrorCounts.TryGetValue(name, out var value))
        {
            value = 1;
            ErrorCounts[name] = value;
            return;
        }

        ErrorCounts[name] = ++value;
    }

    public void Clear()
    {
        Data.Clear();
        FlowTags.Clear();
        OnTag = delegate { };
        Workflows.Clear();
    }
}