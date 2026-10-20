namespace AiCoder.Models;

public sealed class MessageBlock
{
    public string Type { get; set; } = "text"; // "text" or "tool"
    public string? Text { get; set; }
    public ToolCard? ToolCard { get; set; }
}
