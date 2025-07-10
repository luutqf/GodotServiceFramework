using Godot;

namespace GodotServiceFramework.Util;

public static class Log
{
    public static void Info(object message, BbColor color = BbColor.Gray)
    {
        if (MyLogger.Instance != null)
        {
            MyLogger.Info(message, color);
        }
        else
        {
            Console.WriteLine(message);
        }
    }

    public static void Debug(object message, BbColor color = BbColor.Gray)
    {
        if (MyLogger.Instance != null)
        {
            MyLogger.Debug(message, color);
        }
    }

    public static void Warn(object message, BbColor color = BbColor.Orange)
    {
        if (MyLogger.Instance != null)
        {
            MyLogger.Warn(message, color);
        }
        else
        {
            GD.PrintErr(message);
        }
    }

    public static void Error(object message, BbColor color = BbColor.Red)
    {
        if (MyLogger.Instance != null)
        {
            MyLogger.Error(message, color);
        }
        else
        {
            Console.Error.WriteLine(message);
        }
    }

    public static void Scene(string name, object args)
    {
    }
}