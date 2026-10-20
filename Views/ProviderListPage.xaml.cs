using System.Collections.ObjectModel;
using AiCoder.Models;
using AiCoder.Resources.Strings;
using AiCoder.Services;

namespace AiCoder.Views;

public partial class ProviderListPage : ContentPage
{
    private readonly ObservableCollection<ProviderConfig> _providers;

    public ProviderListPage(ObservableCollection<ProviderConfig> providers)
    {
        InitializeComponent();
        _providers = providers;
        ProviderList.ItemsSource = _providers;
    }

    private async void Add_Click(object? sender, EventArgs e)
    {
        var editor = new ProviderEditorPage(null);
        await Navigation.PushModalAsync(editor);
        var cfg = await editor.Result;
        if (cfg is null) return;

        _providers.Add(cfg);
        await ProviderStore.SaveAsync(_providers);
    }

    private async void Edit_Click(object? sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not ProviderConfig cfg) return;

        var editor = new ProviderEditorPage(cfg);
        await Navigation.PushModalAsync(editor);
        var updated = await editor.Result;
        if (updated is null) return;

        var idx = _providers.IndexOf(cfg);
        if (idx >= 0) _providers[idx] = updated;
        await ProviderStore.SaveAsync(_providers);
    }

    private async void Delete_Click(object? sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not ProviderConfig cfg) return;

        var ok = await DisplayAlertAsync(AppResources.DeleteProvider, string.Format(AppResources.ConfirmDeleteProvider, cfg.Name), AppResources.Confirm, AppResources.Cancel);
        if (!ok) return;

        _providers.Remove(cfg);
        await ProviderStore.SaveAsync(_providers);
    }

    private async void Close_Click(object? sender, EventArgs e)
        => await Navigation.PopModalAsync();
}
