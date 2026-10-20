using System.Collections.ObjectModel;
using System.ComponentModel;
using AiCoder.Resources.Strings;

namespace AiCoder.Models;

public sealed class ChatMessage : INotifyPropertyChanged
{
    private string _text = "";
    public string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;
            _text = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayText)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasText)));
        }
    }

    public string DisplayText => StripPrefix(Text);

    public ChatMessageRole Role { get; set; } = ChatMessageRole.User;

    public string RoleLabel => Role switch
    {
        ChatMessageRole.User => AppResources.MessageYou,
        ChatMessageRole.Assistant => AppResources.MessageAgent,
        ChatMessageRole.Tool => AppResources.MessageToolResult,
        _ => ""
    };

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    public byte[]? ImageBytes { get; set; }

    public string? MimeType { get; set; }

    public bool HasText => !string.IsNullOrWhiteSpace(DisplayText) || Blocks.Any(b => b.Type == "text" && !string.IsNullOrWhiteSpace(b.Text));

    public bool HasImage => ImageBytes is { Length: > 0 };

    public ImageSource? ImageSource
    {
        get
        {
            if (ImageBytes is null || ImageBytes.Length == 0) return null;
            return ImageSource.FromStream(() => new MemoryStream(ImageBytes));
        }
    }

    public ObservableCollection<MessageBlock> Blocks { get; } = new();

    // 兼容旧会话：ToolCards 用于从旧数据恢复
    public ObservableCollection<ToolCard> ToolCards { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    private static string StripPrefix(string text)
    {
        var prefixes = new[]
        {
            AppResources.MessageYou + " ",
            AppResources.MessageAgent + " ",
            AppResources.MessageToolCall + " ",
            AppResources.MessageToolResult + " "
        };
        foreach (var p in prefixes)
        {
            if (text.StartsWith(p, StringComparison.Ordinal))
                return text[p.Length..];
        }
        return text;
    }
}
