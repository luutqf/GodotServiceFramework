using Godot;

namespace GodotServiceFramework.GConsole;
public partial class ConsoleMessage : RefCounted
{
    private string _text = string.Empty;
    private string _messageId = string.Empty;

    public string Type
    {
        get
        {
            if (!string.IsNullOrEmpty(Scene))
            {
                return "scene";
            }

            if (!string.IsNullOrEmpty(User))
            {
                return "chat";
            }

            return "text";
        }
    }

    public string Scene { get; set; } = string.Empty;

    public Variant Args { get; set; }

    public string Text
    {
        get => _text;
        set => _text = value.Trim();
    }


    public bool AutoClear { get; set; } = false;
    public string User { get; set; } = string.Empty;

    public string MessageId { get; set; } = string.Empty;

    public float Speed { get; set; } = 0.015f;
    public float ClearDelay { get; set; } = 0.7f;

    public int Level { get; set; } = 1;

    /// <summary>
    /// 标记输出的目标会话类型
    /// </summary>
    public string SessionType { get; set; } = string.Empty;
}