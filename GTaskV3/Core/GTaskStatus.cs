namespace GodotServiceFramework.GTaskV3;

public class GTaskStatus
{
    //任务进度
    public int Progress { get; set; }

    //任务消息, 这个是半持久化的状态,会累加, status重置也不会影响
    public readonly List<string> Messages = [];

    //任务标签, 运行过程中添加
    public readonly HashSet<string> Tags = [];

    //用于标记任务是否处于健康状态, 如果处于非健康状态, 则开启重试计数
    public bool Health { get; set; } = true;

    public int RetryCount { get; set; } = 0;

    public void Clean()
    {
        Progress = 0;
        Tags.Clear();
        Health = true;
        RetryCount = 0;
    }
}