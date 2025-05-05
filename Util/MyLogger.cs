using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Godot;
using GodotServiceFramework.Context.Service;
using AutoGodotService = GodotServiceFramework.Context.Service.AutoGodotService;

namespace GodotServiceFramework.Util;

/*
 * This is meant to replace all GD.Print(...) with Logger.Log(...) to make
 * logging multi-thread friendly. Remember to put Logger.Update() in
 * _PhysicsProcess(double delta) otherwise you will be wondering why Logger.Log(...)
 * is printing nothing to the console.
 */
// [AutoGlobalService]
[Order(-2000)]
public partial class MyLogger : AutoGodotService
{
    public event Action<string?, BbColor> MessageLogged = delegate { };

    private readonly ConcurrentQueue<LogInfo> _messages = new();

    public static LogLevel CurrentLevel { get; set; } = LogLevel.Info;

    public static MyLogger? Instance { get; private set; }

    /// <summary>
    /// Log a message
    /// </summary>
    public void BaseLog(object message, BbColor color = BbColor.Gray) =>
        _messages.Enqueue(new LogInfo(LoggerOpcode.Message, new LogMessage($"{message}"), color));


    public static void Info(object? message, BbColor color = BbColor.Gray)
    {
        Log(message, color);
    }

    public static void Debug(object? message, BbColor color = BbColor.Gray)
    {
        Log(message, color, level: LogLevel.Debug);
    }

    public static void Warn(object? message, BbColor color = BbColor.Gray)
    {
        Log(message, color, level: LogLevel.Warn);
    }

    public static void Error(object? message, BbColor color = BbColor.Gray)
    {
        Log(message, color, level: LogLevel.Error);
    }

    public static void Log(object? message, BbColor color = BbColor.Gray, LogLevel level = LogLevel.Info)
    {
        if (message == null || CurrentLevel > level)
        {
            return;
        }

        if (Instance == null)
        {
            Console.WriteLine(message);
            return;
        }

        switch (level)
        {
            case LogLevel.Info:
            {
                Instance.BaseLog(message, color);
                break;
            }
            case LogLevel.Warn:
            {
                Instance.LogWarning(message, color);

                break;
            }
            case LogLevel.Todo:
            {
                Instance.LogTodo(message, color);

                break;
            }
            case LogLevel.Debug:
            {
                Instance.LogDebug(message, color);

                break;
            }
            case LogLevel.Error:
            {
                Instance.PrintErr(message, color);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }

    /// <summary>
    /// Log a warning
    /// </summary>
    public void LogWarning(object message, BbColor color = BbColor.Orange) =>
        BaseLog($"[Warning] {message}", color);

    /// <summary>
    /// Log a todo
    /// </summary>
    public void LogTodo(object message, BbColor color = BbColor.White) =>
        BaseLog($"[Todo] {message}", color);

    /// <summary>
    /// Logs an exception with trace information. Optionally allows logging a human readable hint
    /// </summary>
    public void LogErr
    (
        Exception e,
        string? hint = null,
        BbColor color = BbColor.Red,
        [CallerFilePath] string filePath = null!,
        [CallerLineNumber] int lineNumber = 0
    ) => LogDetailed(LoggerOpcode.Exception,
        $"[Error] {(string.IsNullOrWhiteSpace(hint) ? "" : $"'{hint}' ")}{e.Message}{e.StackTrace}", color, true,
        filePath, lineNumber);

    /// <summary>
    /// Logs a debug message that optionally contains trace information
    /// </summary>
    public void LogDebug
    (
        object message,
        BbColor color = BbColor.Magenta,
        bool trace = true,
        [CallerFilePath] string filePath = null!,
        [CallerLineNumber] int lineNumber = 0
    ) => LogDetailed(LoggerOpcode.Debug, $"[Debug] {message}", color, trace, filePath, lineNumber);

    /// <summary>
    /// Log the time it takes to do a section of code
    /// </summary>
    public void LogMs(Action code)
    {
        var watch = new Stopwatch();
        watch.Start();
        code();
        watch.Stop();
        Log($"Took {watch.ElapsedMilliseconds} ms");
    }

    public override void _PhysicsProcess(double delta)
    {
        Update();
    }

    /// <summary>
    /// Checks to see if there are any messages left in the queue
    /// </summary>
    public bool StillWorking() => !_messages.IsEmpty;

    /// <summary>
    /// Dequeues a Requested Message and Logs it
    /// </summary>
    public void Update()
    {
        if (!_messages.TryDequeue(out var result))
            return;

        switch (result.Opcode)
        {
            case LoggerOpcode.Message:
                Print(result.Data.Message, result.Color);
                System.Console.ResetColor();
                break;

            case LoggerOpcode.Exception:
                PrintErr(result.Data.Message, result.Color);

                if (result.Data is LogMessageTrace exceptionData && exceptionData.ShowTrace)
                    PrintErr(exceptionData.TracePath, BbColor.DarkGray);

                System.Console.ResetColor();
                break;

            case LoggerOpcode.Debug:
                Print(result.Data.Message, result.Color);

                if (result.Data is LogMessageTrace debugData && debugData.ShowTrace)
                    Print(debugData.TracePath, BbColor.DarkGray);

                System.Console.ResetColor();
                break;
        }

        MessageLogged?.Invoke(result.Data.Message, result.Color);
    }

    /// <summary>
    /// Logs a message that may contain trace information
    /// </summary>
    void LogDetailed(LoggerOpcode opcode, string? message, BbColor color, bool trace, string filePath, int lineNumber)
    {
        string? tracePath;

        if (filePath.Contains("Scripts"))
        {
            // Ex: Scripts/Main.cs:23
            tracePath = $"  at {filePath[filePath.IndexOf("Scripts", StringComparison.Ordinal)..]}:{lineNumber}";
            tracePath = tracePath.Replace('\\', '/');
        }
        else
        {
            // Main.cs:23
            var elements = filePath.Split('\\');
            tracePath = $"  at {elements[^1]}:{lineNumber}";
        }

        _messages.Enqueue(
            new LogInfo(opcode,
                new LogMessageTrace(
                    message,
                    trace,
                    tracePath
                ),
                color
            ));
    }

    void Print(object? v, BbColor color)
    {
        //Console.ForegroundColor = color;

        if (GOS.IsExportedRelease())
            GD.Print(v);
        else
            // Full list of BBCode color tags: https://absitomen.com/index.php?topic=331.0
            GD.PrintRich($"[color={color}]{v}");
    }

    void PrintErr(object? v, BbColor color)
    {
        //Console.ForegroundColor = color;
        GD.PrintErr(v);
        GD.PushError(v);
    }

    public override void Init()
    {
        Instance = this;
    }

    public override void Destroy()
    {
    }
}

public class LogInfo
{
    public LoggerOpcode Opcode { get; set; }
    public LogMessage Data { get; set; }
    public BbColor Color { get; set; }

    public LogInfo(LoggerOpcode opcode, LogMessage data, BbColor color = BbColor.Gray)
    {
        Opcode = opcode;
        Data = data;
        Color = color;
    }
}

public class LogMessage(string? message)
{
    public string? Message { get; set; } = message;
}

public class LogMessageTrace(string? message, bool trace = true, string? tracePath = null)
    : LogMessage(message)
{
    // Show the Trace Information for the Message
    public bool ShowTrace { get; set; } = trace;
    public string? TracePath { get; set; } = tracePath;
}

public enum LoggerOpcode
{
    Message,
    Exception,
    Debug
}

// Full list of BBCode color tags: https://absitomen.com/index.php?topic=331.0
public enum BbColor
{
    Gray,
    DarkGray,
    Green,
    DarkGreen,
    LightGreen,
    Aqua,
    DarkAqua,
    Deepskyblue,
    Magenta,
    Red,
    White,
    Yellow,
    Orange
}

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error,
    Todo,
}