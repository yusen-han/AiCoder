using System.Text.Json.Serialization;

namespace AiCoder.Models;

/// <summary>
/// 一个模型提供者的完整配置。序列化到 providers.json。
/// Kind 对应 IProviderFactory.Kind，是"接入方式"的标识。
/// </summary>
public sealed class ProviderConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New Provider";
    public string Kind { get; set; } = "ollama";
    public string Endpoint { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "";

    [JsonIgnore]
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Kind : Name;
}