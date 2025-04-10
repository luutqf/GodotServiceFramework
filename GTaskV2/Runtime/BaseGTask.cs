using GodotServiceFramework.Util;

namespace SigmusV2.GodotServiceFramework.GTaskV2;

public abstract class BaseGTask
{
    public event Action<BaseGTask, string> OnBeforeStart = delegate { };
    public event Action<BaseGTask, string> OnAfterStart = delegate { };
    public event Action<BaseGTask, string> OnStop = delegate { };
    public event Action<BaseGTask, string> OnError = delegate { };
    public event Action<BaseGTask, string> OnDestroy = delegate { };

    public event Action<BaseGTask, string> OnSkip = delegate { };

    public event Action<BaseGTask, string> OnAbort = delegate { };
    public event Action<BaseGTask, string> OnComplete = delegate { };
    public event Action<BaseGTask, string> OnTag = delegate { };


    private int _progress = TaskDefault;

    public int Progress
    {
        get => _progress;
        protected set { }
    }

    public virtual void Start()
    {
    }

    public void _Start()
    {

    }


    public virtual void Stop()
    {
    }

    public void _Stop()
    {

    }

    public virtual void Destroy()
    {
    }

    public void _Destroy()
    {
    }

    /// <summary>
    /// 用于接收某些消息
    /// </summary>
    /// <param name="message"></param>
    public virtual void Receive((string type, string key, string value) message)
    {
    }


    public const int TaskAbort = -2;

    public const int TaskError = -1;

    public const int TaskStart = 1;

    public const int TaskDefault = 0;

    public const int TaskComplete = 100;

    public const int TaskSelfSkip = -5;

    public const int TaskTagSkip = -3;

    public const int TaskGhostSkip = -4;

    public const int SkipAllTask = -100;
}