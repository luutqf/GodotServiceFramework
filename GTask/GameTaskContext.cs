namespace GodotServiceFramework.GTask;

public class GameTaskContext
{
    public string Name { get; set; } = string.Empty;

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

    public void PutTags(int flowId, params string[] tags)
    {
        foreach (var tag in tags)
        {
            FlowTags.Add(tag);
            OnTag.Invoke(tag, flowId);
        }
    }

    #endregion


    public void Clear()
    {
        Data.Clear();
        FlowTags.Clear();
        OnTag = delegate { };
    }
}