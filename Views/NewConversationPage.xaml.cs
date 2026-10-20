using System.Collections.ObjectModel;
using AiCoder.Models;
using AiCoder.Resources.Strings;
using AiCoder.Services;

namespace AiCoder.Views;

public partial class NewConversationPage : ContentPage
{
    private readonly ObservableCollection<ProviderConfig> _providers;
    private readonly TaskCompletionSource<NewConversationResult?> _tcs = new();

    public NewConversationPage(ObservableCollection<ProviderConfig> providers)
    {
        InitializeComponent();
        _providers = providers;

        ProviderPicker.ItemsSource = _providers;
        ProviderPicker.ItemDisplayBinding = new Binding(nameof(ProviderConfig.Name));
        if (_providers.Count > 0) ProviderPicker.SelectedIndex = 0;

        ToolModePicker.ItemsSource = new[]
        {
            AppResources.AutoRunTools,
            AppResources.AskForToolCalling,
            AppResources.NeverRunTools
        };
        ToolModePicker.SelectedIndex = 0;
    }

    /// <summary>页面关闭后由 MainPage await 的结果；null 表示取消。</summary>
    public Task<NewConversationResult?> Result => _tcs.Task;

    private async void Manage_Click(object? sender, EventArgs e)
        => await Navigation.PushModalAsync(new ProviderListPage(_providers));

    private async void Create_Click(object? sender, EventArgs e)
    {
        if (ProviderPicker.SelectedItem is not ProviderConfig provider)
        {
            await DisplayAlertAsync(AppResources.Remind, AppResources.PleaseAddOrChooseProvider, AppResources.Ok);
            return;
        }

        var mode = ToolModePicker.SelectedIndex switch
        {
            1 => ToolMode.Approve,
            2 => ToolMode.Never,
            _ => ToolMode.Auto
        };

        _tcs.TrySetResult(new NewConversationResult(provider, mode));
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
