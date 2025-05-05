namespace GodotServiceFramework.GTask.Extensions;

public static class GameTaskExtensions
{
    /// <summary>
    /// 附加任务消息, 
    /// </summary>
    /// <param name="gameTask"></param>
    /// <param name="message">消息内容</param>
    /// <param name="level">消息等级</param>
    /// <param name="type">消息类型, 默认为session</param>
    /// <param name="args"></param>
    public static void AppendMessage(this GameTask gameTask, string message, int level = 1, string type = "session",
        Dictionary<string, object>? args = null)
    {
    }

    /// <summary>
    /// 附加任务序列消息
    /// </summary>
    /// <param name="workflow"></param>
    /// <param name="message"></param>
    /// <param name="level"></param>
    /// <param name="type"></param>
    /// <param name="args"></param>
    public static void AppendMessage(this GameTaskWorkflow workflow, string message, int level = 1,
        string type = "session", Dictionary<string, object>? args = null)
    {
    }

    /// <summary>
    /// 获取Arg
    /// </summary>
    /// <param name="task"></param>
    /// <param name="keys"></param>
    /// <returns></returns>
    public static object? GetArg(this GameTask task, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (task.Args.TryGetValue(key, out var arg)) return arg;
        }

        return null;
    }
}