using AiCoder.Models;
using AiCoder.Resources.Strings;
using OpenAI;
using System.ClientModel;

namespace AiCoder.Services;

internal static class OpenAIClientHelper
{
    public static OpenAIClient Create(ProviderConfig c)
    {
        if (string.IsNullOrWhiteSpace(c.Model))
            throw new InvalidOperationException(AppResources.ModelNameRequired);

        var key = string.IsNullOrWhiteSpace(c.ApiKey) ? "not-needed" : c.ApiKey;
        var credential = new ApiKeyCredential(key);

        if (string.IsNullOrWhiteSpace(c.Endpoint))
            return new OpenAIClient(credential);   // 走官方默认 endpoint

        var options = new OpenAIClientOptions { Endpoint = new Uri(c.Endpoint) };
        return new OpenAIClient(credential, options);
    }
}
