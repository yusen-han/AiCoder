using AiCoder.Models;
using AiCoder.Resources.Strings;
using AiCoder.Services;

namespace AiCoder.Views;

public partial class ProviderEditorPage : ContentPage
{
    private readonly ProviderConfig? _editing;
    private readonly List<IProviderFactory> _factories;
    private readonly TaskCompletionSource<ProviderConfig?> _tcs = new();

    public ProviderEditorPage(ProviderConfig? editing)
    {
        InitializeComponent();
        _editing = editing;
        _factories = ProviderRegistry.All.ToList();

        KindPicker.ItemsSource = _factories;
        KindPicker.ItemDisplayBinding = new Binding(nameof(IProviderFactory.DisplayName));

        if (editing is null)
        {
            KindPicker.SelectedIndex = 0;
        }
        else
        {
            NameBox.Text = editing.Name;
            EndpointBox.Text = editing.Endpoint;
            ApiKeyBox.Text = editing.ApiKey;
            ModelBox.Text = editing.Model;

            var idx = _factories.FindIndex(f =>
                string.Equals(f.Kind, editing.Kind, StringComparison.OrdinalIgnoreCase));
            KindPicker.SelectedIndex = idx >= 0 ? idx : 0;
        }

        UpdatePlaceholders();
    }

    public Task<ProviderConfig?> Result => _tcs.Task;

    private string SelectedKind =>
        KindPicker.SelectedItem is IProviderFactory f ? f.Kind : "ollama";

    private void KindPicker_SelectedIndexChanged(object? sender, EventArgs e)
        => UpdatePlaceholders();

    private void UpdatePlaceholders()
    {
        switch (SelectedKind)
        {
            case "ollama":
                EndpointBox.Placeholder = "http://localhost:11434";
                ApiKeyBox.IsEnabled = false;
                ApiKeyBox.Placeholder = AppResources.OllamaNoApiKey;
                break;
            case "openai-chat":
            case "openai-responses":
                EndpointBox.Placeholder = AppResources.OpenAiEndpointPlaceholder;
                ApiKeyBox.IsEnabled = true;
                ApiKeyBox.Placeholder = AppResources.OpenAiApiKeyPlaceholder;
                break;
            default:
                EndpointBox.Placeholder = AppResources.CompatibleEndpointPlaceholder;
                ApiKeyBox.IsEnabled = true;
                ApiKeyBox.Placeholder = AppResources.CompatibleApiKeyPlaceholder;
                break;
        }
    }

    private async void Save_Click(object? sender, EventArgs e)
    {
        var model = ModelBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(model))
        {
            await DisplayAlertAsync(AppResources.Remind, AppResources.ProviderNameMustNotBeEmpty, AppResources.Ok);
            return;
        }

        var name = NameBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name))
            name = _factories.FirstOrDefault(f => f.Kind == SelectedKind)?.DisplayName ?? SelectedKind;

        var cfg = new ProviderConfig
        {
            Id = _editing?.Id ?? Guid.NewGuid().ToString("N"),
            Name = name,
            Kind = SelectedKind,
            Endpoint = EndpointBox.Text?.Trim() ?? "",
            ApiKey = ApiKeyBox.Text?.Trim() ?? "",
            Model = model
        };

        _tcs.TrySetResult(cfg);
        await Navigation.PopModalAsync();
    }

    private async void Cancel_Click(object? sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        await Navigation.PopModalAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        _tcs.TrySetResult(null);
        return base.OnBackButtonPressed();
    }
}
