using System.Text;
using System.Text.Json;
using AiCoder.Models;

namespace AiCoder.Services;

public static class SessionStore
{
    private static readonly string FilePath =
        Path.Combine(FileSystem.AppDataDirectory, "sessions.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<List<ConversationDto>> LoadAsync()
    {
        try
        {
            if (!File.Exists(FilePath)) return new();
            var json = await File.ReadAllTextAsync(FilePath);
            return JsonSerializer.Deserialize<List<ConversationDto>>(json, Options) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static async Task SaveAsync(IEnumerable<Conversation> conversations)
    {
        try
        {
            var dtos = conversations.Select(c => new ConversationDto
            {
                Id = c.Id,
                Title = c.Title,
                ProviderId = c.Provider.Id,
                ToolMode = c.ToolMode,
                Messages = c.Messages.Select(m => new ChatMessageDto
                {
                    Role = m.Role,
                    Text = m.Text,
                    Timestamp = m.Timestamp,
                    ImageBase64 = m.ImageBytes is { Length: > 0 }
                        ? Convert.ToBase64String(m.ImageBytes)
                        : null,
                    MimeType = m.MimeType,
                    Blocks = m.Blocks.Select(b => new MessageBlockDto
                    {
                        Type = b.Type,
                        Text = b.Text,
                        ToolCard = b.ToolCard is null ? null : new ToolCardDto
                        {
                            CallId = b.ToolCard.CallId,
                            Name = b.ToolCard.Name,
                            CallContent = b.ToolCard.CallContent,
                            ResultContent = b.ToolCard.ResultContent
                        }
                    }).ToList(),
                    ToolCards = m.ToolCards.Select(t => new ToolCardDto
                    {
                        CallId = t.CallId,
                        Name = t.Name,
                        CallContent = t.CallContent,
                        ResultContent = t.ResultContent
                    }).ToList()
                }).ToList()
            }).ToList();

            var json = JsonSerializer.Serialize(dtos, Options);
            await File.WriteAllTextAsync(FilePath, json);
        }
        catch
        {
            // 忽略持久化失败
        }
    }
}
