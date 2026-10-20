namespace AiCoder.Models;

public sealed class MessageBlockDto
{
    public string Type { get; set; } = "text";
    public string? Text { get; set; }
    public ToolCardDto? ToolCard { get; set; }
}
