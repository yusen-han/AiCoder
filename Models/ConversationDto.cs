namespace AiCoder.Models;

public sealed class ConversationDto
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "New Session";
    public string ProviderId { get; set; } = "";
    public ToolMode ToolMode { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = new();
}
