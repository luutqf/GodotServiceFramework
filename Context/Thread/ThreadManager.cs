using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.Context.Thread;

public partial class ThreadManager : AutoGodotService
{
    public static ThreadManager? Instance { get; private set; }
    public CustomThreadPool? CustomThreadPool { get; private set; }

    public ThreadManager()
    {
        CustomThreadPool = new CustomThreadPool(maxThreads: Environment.ProcessorCount);
        Instance = this;
    }

    public override void Destroy()
    {
    }


    public Task<TResult> QueueWorkItem<TResult>(Func<TResult> func) => CustomThreadPool!.QueueWorkItem(func);

    public void QueueWorkItem(Action action) => CustomThreadPool!.QueueWorkItem(action);
}