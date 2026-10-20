// OpenAIChatProviderFactory.cs
using AiCoder.Models;
using Microsoft.Extensions.AI;
using OpenAI;

namespace AiCoder.Services.Factories;

public sealed class OpenAIChatProviderFactory : IProviderFactory
{
    public string Kind => "openai-chat";
    public string DisplayName => "OpenAI Chat Completions";

    public IChatClient Create(ProviderConfig c) =>
        OpenAIClientHelper.Create(c).GetChatClient(c.Model).AsIChatClient();
}