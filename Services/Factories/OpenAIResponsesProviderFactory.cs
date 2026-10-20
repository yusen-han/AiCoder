// OpenAIResponsesProviderFactory.cs
using AiCoder.Models;
using Microsoft.Extensions.AI;
using OpenAI;

namespace AiCoder.Services.Factories;

public sealed class OpenAIResponsesProviderFactory : IProviderFactory
{
    public string Kind => "openai-responses";
    public string DisplayName => "OpenAI Responses API";

    public IChatClient Create(ProviderConfig c)
    {
#pragma warning disable OPENAI001
        return OpenAIClientHelper.Create(c).GetResponsesClient().AsIChatClient();
#pragma warning restore OPENAI001
    }
}