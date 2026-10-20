// OllamaProviderFactory.cs
using AiCoder.Models;
using AiCoder.Resources.Strings;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace AiCoder.Services.Factories;

public sealed class OllamaProviderFactory : IProviderFactory
{
    public string Kind => "ollama";
    public string DisplayName => "Ollama (Local)";

    public IChatClient Create(ProviderConfig c)
    {
        if (string.IsNullOrWhiteSpace(c.Endpoint))
            throw new InvalidOperationException(AppResources.EndpointRequired);
        if (string.IsNullOrWhiteSpace(c.Model))
            throw new InvalidOperationException(AppResources.ModelNameRequired);

        return new OllamaApiClient(new Uri(c.Endpoint), c.Model);
    }
}
