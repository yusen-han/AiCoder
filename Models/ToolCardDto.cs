namespace AiCoder.Models;

public sealed class ToolCardDto
{
    public string CallId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? CallContent { get; set; }
    public string? ResultContent { get; set; }

    // 兼容旧会话
    public string? Type { get; set; }
    public string? Content { get; set; }
}
