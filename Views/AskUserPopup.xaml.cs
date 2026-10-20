using AiCoder.Models;
using AiCoder.Resources.Strings;
using CommunityToolkit.Maui.Views;

namespace AiCoder.Views;

/// <summary>
/// 询问用户的 Popup。支持单选、多选、Other 自由输入。
/// 关闭时通过 CloseAsync 返回 AskUserResult。
/// </summary>
public partial class AskUserPopup : Popup<AskUserResult>
{
    private readonly AskUserRequest _request;
    private readonly bool _isMultiple;
    private readonly List<OptionRow> _rows = new();

    public AskUserPopup(AskUserRequest request)
    {
        InitializeComponent();

        _request = request;
        _isMultiple = string.Equals(
            request.SelectionMode, "multiple", StringComparison.OrdinalIgnoreCase);

        QuestionLabel.Text = request.Question;
        OtherEntry.IsVisible = request.AllowOther;

        BuildOptions();
    }

    private void BuildOptions()
    {
        OptionsLayout.Children.Clear();
        _rows.Clear();

        // 普通选项
        foreach (var option in _request.Options)
        {
            var row = CreateOptionRow(option);
            _rows.Add(row);
            OptionsLayout.Children.Add(row.Root);
        }

        // Other 行（如果有）
        if (_request.AllowOther)
        {
            var otherRow = CreateOptionRow(AppResources.OtherPlaceholder, isOther: true);
            _rows.Add(otherRow);
            OptionsLayout.Children.Add(otherRow.Root);
        }
    }

    private OptionRow CreateOptionRow(string text, bool isOther = false)
    {
        var label = new Label
        {
            Text = text,
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.WordWrap
        };

        View selector;
        Action<bool> setSelected;
        Func<bool> getSelected;

        if (_isMultiple)
        {
            var cb = new CheckBox { VerticalOptions = LayoutOptions.Center };
            selector = cb;
            setSelected = v => cb.IsChecked = v;
            getSelected = () => cb.IsChecked;

            // 点击整行也能切换
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => cb.IsChecked = !cb.IsChecked;
            label.GestureRecognizers.Add(tap);
        }
        else
        {
            var rb = new RadioButton { GroupName = "askuser_group", VerticalOptions = LayoutOptions.Center };
            selector = rb;
            setSelected = v => rb.IsChecked = v;
            getSelected = () => rb.IsChecked;

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => rb.IsChecked = true;
            label.GestureRecognizers.Add(tap);

            // 单选：选中 Other 时自动把焦点给输入框
            if (isOther)
                rb.CheckedChanged += (_, _) => { if (rb.IsChecked) OtherEntry.Focus(); };
        }

        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 8
        };
        grid.Add(selector, 0, 0);
        grid.Add(label, 1, 0);

        // Other 行的条目文字是 "Other"，实际值用输入框内容
        return new OptionRow
        {
            Root = grid,
            Text = text,
            IsOther = isOther,
            SetSelected = setSelected,
            GetSelected = getSelected
        };
    }

    private async void Confirm_Click(object? sender, EventArgs e)
    {
        var result = new AskUserResult { IsMultiple = _isMultiple };

        foreach (var row in _rows)
        {
            if (!row.GetSelected()) continue;

            if (row.IsOther)
            {
                var text = OtherEntry.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(text))
                {
                    // Other 被选中但没输入内容 → 提示
                    var hostPage = Application.Current?.Windows.FirstOrDefault()?.Page;
                    if (hostPage is not null)
                        await hostPage.DisplayAlertAsync(AppResources.Prompt, AppResources.OtherEmptyWarning, AppResources.Ok);
                    return;
                }
                result.Selected.Add(text);
            }
            else
            {
                result.Selected.Add(row.Text);
            }
        }

        if (result.Selected.Count == 0)
        {
            var hostPage = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (hostPage is not null)
                await hostPage.DisplayAlertAsync(AppResources.Prompt, AppResources.PleaseSelectAtLeastOne, AppResources.Ok);
            return;
        }

        await CloseAsync(result);
    }

    private async void Cancel_Click(object? sender, EventArgs e)
    {
        await CloseAsync(new AskUserResult { Cancelled = true });
    }

    private sealed class OptionRow
    {
        public View Root { get; set; } = null!;
        public string Text { get; set; } = "";
        public bool IsOther { get; set; }
        public Action<bool> SetSelected { get; set; } = _ => { };
        public Func<bool> GetSelected { get; set; } = () => false;
    }
}
