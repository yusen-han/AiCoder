using System.Text.Json;
using AiCoder.Models;

namespace AiCoder.Services;

public static class ProviderStore
{
    private static readonly string FilePath =
        Path.Combine(FileSystem.AppDataDirectory, "providers.json");

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static async Task<List<ProviderConfig>> LoadAsync()
    {
        try
        {
            if (!File.Exists(FilePath)) return new();
            var json = await File.ReadAllTextAsync(FilePath);
            return JsonSerializer.Deserialize<List<ProviderConfig>>(json, Options) ?? new();
        }
        catch { return new(); }
    }

    public static async Task SaveAsync(IEnumerable<ProviderConfig> providers)
    {
        try
        {
            var json = JsonSerializer.Serialize(providers.ToList(), Options);
            await File.WriteAllTextAsync(FilePath, json);
        }
        catch { /* 忽略持久化失败 */ }
    }
}