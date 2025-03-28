using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.GConsole;

[GlobalClass]
public partial class MyRichTextLabel : RichTextLabel
{
    [Export] private float _typingSpeed = 0.015f; // 每个字符之间的时间间隔（秒）

    private string _fullText = "";
    private string _visibleText = "";
    private int _currentCharIndex;
    private bool _isTyping;
    private readonly List<BbCodeTag> _bbCodeTags = [];

    private bool _autoClear;
    private float _clearDelay;


    [Signal]
    public delegate void AppendTextSEventHandler(string text);

    private struct BbCodeTag
    {
        public int StartIndex;
        public int EndIndex;
        public string Tag;
    }

    public override void _Ready()
    {
        // 确保RichTextLabel已经配置为使用BBCode
        BbcodeEnabled = true;
        Text = string.Empty;
        // FitContent = true;

        Connect(SignalName.AppendTextS, Callable.From<string>(s => SetText(s)));
    }

    // 设置要显示的文本（包含BBCode）
    public void SetText(string text, float typingSpeed = 0.015f, bool autoClear = false, float clearDelay = 0.7f)
    {
        if (typingSpeed == 0)
        {
            base.SetText(text);
            return;
        }

        _typingSpeed = typingSpeed;

        _autoClear = autoClear;
        _clearDelay = clearDelay;

        _fullText = text;
        _visibleText = "";
        _currentCharIndex = 0;
        _bbCodeTags.Clear();

        // 解析BBCode标签
        ParseBbCode(_fullText);

        // 清空当前显示的文本
        Text = "";

        // 开始打字机效果
        StartTyping();
    }

    // 解析文本中的所有BBCode标签
    private void ParseBbCode(string text)
    {
        // 使用正则表达式匹配所有BBCode标签
        const string pattern = @"\[(/?[a-z0-9_=\-\s""']*)\]";

        var matches = Regex.Matches(text, pattern);

        foreach (Match match in matches)
        {
            var tag = new BbCodeTag
            {
                StartIndex = match.Index,
                EndIndex = match.Index + match.Length - 1,
                Tag = match.Value
            };

            _bbCodeTags.Add(tag);
        }
    }

    // 开始打字机效果
    public void StartTyping()
    {
        if (_fullText.Length <= 0 || _isTyping) return;

        _isTyping = true;
        TypeNextCharacter();
    }

    // 立即完成打字效果，显示全部文本
    public void CompleteTyping()
    {
        _isTyping = false;
        Text = _fullText;
        _currentCharIndex = _fullText.Length;
    }

    // 输出下一个字符
    private void TypeNextCharacter()
    {
        if (_currentCharIndex >= _fullText.Length)
        {
            _isTyping = false;
            return;
        }

        // 检查当前位置是否在BBCode标签内
        var isInTag = false;
        var currentTag = "";

        foreach (var tag in _bbCodeTags.Where(tag =>
                     _currentCharIndex >= tag.StartIndex && _currentCharIndex <= tag.EndIndex))
        {
            isInTag = true;
            currentTag = tag.Tag;
            break;
        }

        if (isInTag)
        {
            // 如果在标签内，一次性添加整个标签
            AppendText(currentTag);
            _currentCharIndex = _currentCharIndex + currentTag.Length;
        }
        else
        {
            // 否则添加单个字符
            AppendText(_fullText[_currentCharIndex].ToString());
            _currentCharIndex++;
        }

        // 如果还有字符，设置定时器继续输出
        if (_currentCharIndex < _fullText.Length)
        {
            // 创建一个计时器来延迟显示下一个字符
            GetTree().CreateTimer(_typingSpeed).Timeout += TypeNextCharacter;
        }
        else
        {
            if (_autoClear)
            {
                GetTree().CreateTimer(_clearDelay).Timeout += () => { this.GetRoot<TextLineControl>()?.Close(); };
            }

            _isTyping = false;
        }
    }

    // 跳过当前正在进行的打字效果
    public void SkipTyping()
    {
        if (_isTyping)
        {
            CompleteTyping();
        }
    }
}