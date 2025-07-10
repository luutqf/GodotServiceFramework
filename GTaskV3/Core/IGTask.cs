namespace GodotServiceFramework.GTaskV3;

/// <summary>
/// 首先, 任务主体是一个顺序序列, 但初始化之后, 是一个链表, 可以任意修改次序
///
/// 关于任务类型, 有定时任务, 有和弦任务, 有路引任务,  有指针任务, 有组合任务
///
/// 每种任务类型, 都可以是后台任务
///
/// 路引任务会
/// </summary>
public interface IGTask
{
    //表示是否可用, 用于从任务池中取出时, 判断是否被占用
    public bool IsUsable { get; }

    public int Progress { get; set; }

    public void Init(GTaskModel task);


    public Task Start();

    //停止
    public void Stop();

    //暂停
    public void Pause();

    //恢复
    public void Resume();

    //销毁,但销毁只清除func,context和taskInfo
    public void Destroy();
}