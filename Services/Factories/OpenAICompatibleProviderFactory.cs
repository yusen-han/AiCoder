// OpenAICompatibleProviderFactory.cs
using AiCoder.Models;
using AiCoder.Resources.Strings;
using Microsoft.Extensions.AI;
using OpenAI;

namespace AiCoder.Services.Factories;

/// <summary>
/// 任何兼容 OpenAI /v1/chat/completions 的服务：DeepSeek、vLLM、LM Studio、
/// llama.cpp server、OneAPI、Azure 代理……只要填 base url + key + model 即可。
/// </summary>
public sealed class OpenAICompatibleProviderFactory : IProviderFactory
{
    public string Kind => "openai-compatible";
    public string DisplayName => "OpenAI Compatible (Custom)";

    public IChatClient Create(ProviderConfig c)
    {
        if (string.IsNullOrWhiteSpace(c.Endpoint))
            throw new InvalidOperationException(AppResources.EndpointRequired);
        return OpenAIClientHelper.Create(c).GetChatClient(c.Model).AsIChatClient();
    }
}
