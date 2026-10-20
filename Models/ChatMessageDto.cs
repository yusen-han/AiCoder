namespace AiCoder.Models;

public sealed class ChatMessageDto
{
    public ChatMessageRole Role { get; set; }
    public string Text { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
    public string? ImageBase64 { get; set; }
    public string? MimeType { get; set; }
    public List<MessageBlockDto> Blocks { get; set; } = new();
    public List<ToolCardDto> ToolCards { get; set; } = new();
}
