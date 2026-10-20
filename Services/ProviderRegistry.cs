using AiCoder.Models;
using AiCoder.Services.Factories;

namespace AiCoder.Services;

public static class ProviderRegistry
{
    private static readonly List<IProviderFactory> _ordered = new();
    private static readonly Dictionary<string, IProviderFactory> _byKind =
        new(StringComparer.OrdinalIgnoreCase);

    static ProviderRegistry()
    {
        // 内置提供者。想加 Anthropic / Gemini / 自研协议，
        // 写一个 IProviderFactory 实现后在这里 Register 即可。
        Register(new OllamaProviderFactory());
        Register(new OpenAIChatProviderFactory());
        Register(new OpenAIResponsesProviderFactory());
        Register(new OpenAICompatibleProviderFactory());
    }

    public static void Register(IProviderFactory factory)
    {
        if (_byKind.ContainsKey(factory.Kind))
            _ordered.Remove(_byKind[factory.Kind]);
        _byKind[factory.Kind] = factory;
        _ordered.Add(factory);
    }

    public static IProviderFactory Get(string kind) =>
        _byKind.TryGetValue(kind, out var f)
            ? f
            : throw new NotSupportedException($"Unknown Provider Type: {kind}");

    public static IReadOnlyList<IProviderFactory> All => _ordered;
}