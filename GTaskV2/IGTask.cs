using GodotServiceFramework.GTaskV2.Model;

namespace GodotServiceFramework.GTaskV2;

public interface IGTask
{
    public long Id { get; }
    public virtual string Name => GetType().Name;

    public int Progress { get; set; }

    Dictionary<string, object> Parameters { get; set; }

    /// <summary>
    /// 启动任务
    /// </summary>
    /// <returns></returns>
    public Task<bool> Start();

    /// <summary>
    /// 停止任务
    /// </summary>
    /// <returns></returns>
    public bool Stop();

    /// <summary>
    /// 初始化任务
    /// </summary>
    public void Initialize(Dictionary<string, object>? parameters = null);

    /// <summary>
    /// 暂停任务
    /// </summary>
    public void Pause();

    /// <summary>
    /// 恢复任务
    /// </summary>
    public void Resume();


    /// <summary>
    /// 重置当前任务
    /// </summary>
    public void Reset();

    /// <summary>
    /// 回滚当前任务状态之最初的状态
    /// </summary>
    public void Rollback();
}