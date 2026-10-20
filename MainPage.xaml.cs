using AiCoder.Models;
using AiCoder.Resources.Strings;
using AiCoder.Services;
using AiCoder.Views;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Media;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace AiCoder;

public partial class MainPage : ContentPage
{
    private readonly ObservableCollection<Conversation> _conversations = new();
    private readonly ObservableCollection<ProviderConfig> _providers = new();
    private readonly ObservableCollection<PendingAttachment> _pendingAttachments = new();

    private Conversation? _current;
    private bool _providersLoaded;
    private bool _isListening;
    private CancellationTokenSource? _speechCts;

    public MainPage()
    {
        InitializeComponent();
        ConversationList.ItemsSource = _conversations;
        AttachmentList.ItemsSource = _pendingAttachments;
        _pendingAttachments.CollectionChanged += (_, _) => UpdateAttachmentListVisibility();
        SetupLanguagePicker();
        UpdateInputState();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_providersLoaded) return;
        _providersLoaded = true;

        await LoadProvidersAsync();
        await LoadSessionsAsync();
    }

    public async Task SaveSessionsAsync()
    {
        try
        {
            await SessionStore.SaveAsync(_conversations);
        }
        catch { /* 忽略持久化失败 */ }
    }

    private async Task LoadProvidersAsync()
    {
        foreach (var p in await ProviderStore.LoadAsync())
            _providers.Add(p);

        if (_providers.Count == 0)
        {
            _providers.Add(new ProviderConfig
            {
                Name = "Ollama (Local)",
                Kind = "ollama",
                Endpoint = "http://localhost:11434",
                Model = "gpt-oss:20b"
            });
            await ProviderStore.SaveAsync(_providers);
        }
    }

    private async Task LoadSessionsAsync()
    {
        var dtos = await SessionStore.LoadAsync();
        if (dtos.Count == 0) return;

        foreach (var dto in dtos)
        {
            var provider = _providers.FirstOrDefault(p => p.Id == dto.ProviderId);
            if (provider is null) continue;

            try
            {
                var conv = await CreateConversationAsync(provider, dto.ToolMode, dto.Id, dto.Title);
                foreach (var m in dto.Messages)
                {
                    var message = new Models.ChatMessage
                    {
                        Role = m.Role,
                        Text = m.Text,
                        Timestamp = m.Timestamp,
                        ImageBytes = string.IsNullOrEmpty(m.ImageBase64)
                            ? null
                            : Convert.FromBase64String(m.ImageBase64),
                        MimeType = m.MimeType
                    };

                    if (m.Blocks.Count > 0)
                    {
                        foreach (var b in m.Blocks)
                        {
                            var block = new MessageBlock { Type = b.Type, Text = b.Text };
                            if (b.ToolCard is not null)
                            {
                                block.ToolCard = new ToolCard
                                {
                                    CallId = b.ToolCard.CallId,
                                    Name = b.ToolCard.Name,
                                    CallContent = b.ToolCard.CallContent,
                                    ResultContent = b.ToolCard.ResultContent
                                };
                            }
                            message.Blocks.Add(block);
                        }
                    }
                    else
                    {
                        // 兼容旧数据：文本块 + 工具卡片
                        if (!string.IsNullOrWhiteSpace(message.DisplayText))
                            message.Blocks.Add(new MessageBlock { Type = "text", Text = message.DisplayText });

                        foreach (var t in m.ToolCards)
                            message.Blocks.Add(new MessageBlock
                            {
                                Type = "tool",
                                ToolCard = new ToolCard
                                {
                                    CallId = t.Type == "result" ? t.Name : Guid.NewGuid().ToString("N"),
                                    Name = t.Name,
                                    CallContent = t.Type == "call" ? t.Content : null,
                                    ResultContent = t.Type == "result" ? t.Content : null
                                }
                            });
                    }

                    conv.Messages.Add(message);
                }
                conv.IsHistorySeeded = false;
                _conversations.Add(conv);
            }
            catch { /* 恢复失败的会话跳过 */ }
        }
    }

    private void SetupLanguagePicker()
    {
        LanguagePicker.ItemsSource = LocalizationService.SupportedCultures
            .Select(c => new { c.Name, DisplayName = c.NativeName })
            .ToList();
        LanguagePicker.ItemDisplayBinding = new Binding("DisplayName");

        var current = LocalizationService.CurrentCulture.Name;
        var selected = LocalizationService.SupportedCultures
            .Select((c, i) => (c, i))
            .FirstOrDefault(x => x.c.Name.Equals(current, StringComparison.OrdinalIgnoreCase));
        LanguagePicker.SelectedIndex = selected.c is not null ? selected.i : 0;
    }

    private void LanguagePicker_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (LanguagePicker.SelectedItem is not { } item) return;
        var name = item.GetType().GetProperty("Name")?.GetValue(item)?.ToString();
        if (string.IsNullOrEmpty(name)) return;
        if (name.Equals(LocalizationService.CurrentCulture.Name, StringComparison.OrdinalIgnoreCase)) return;

        LocalizationService.SetCulture(name);
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window is null) return;
        window.Page = new AppShell();
    }

    // ======================= 会话操作 =======================

    private async void NewConversation_Click(object? sender, EventArgs e)
    {
        if (_providers.Count == 0)
        {
            await DisplayAlertAsync(AppResources.Remind, AppResources.PleaseAddProviderFirst, AppResources.Ok);
            return;
        }

        var page = new NewConversationPage(_providers);
        await Navigation.PushModalAsync(page);
        var result = await page.Result;
        if (result is null) return;

        try
        {
            var conv = await CreateConversationAsync(result.Provider, result.ToolMode);
            _conversations.Insert(0, conv);
            SelectConversation(conv);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(AppResources.InitializationFailed, ex.Message, AppResources.Ok);
        }
    }

    private async void ManageProviders_Click(object? sender, EventArgs e)
        => await Navigation.PushModalAsync(new ProviderListPage(_providers));

    private void ConversationList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Conversation conv)
            SelectConversation(conv);
    }

    private async void DeleteConversation_Click(object? sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not Conversation conv) return;

        var ok = await DisplayAlertAsync(AppResources.DeleteSession, string.Format(AppResources.ConfirmDeleteSession, conv.Title), AppResources.Delete, AppResources.Cancel);
        if (!ok) return;

        conv.Cts.Cancel();
        _conversations.Remove(conv);

        if (ReferenceEquals(_current, conv))
        {
            _current = null;
            ChatList.ItemsSource = null;
            HeaderLabel.Text = AppResources.YouHaventChosenSession;
            ConversationList.SelectedItem = null;
            UpdateInputState();
        }

        await SaveSessionsAsync();
    }

    private void SelectConversation(Conversation conv)
    {
        _current = conv;
        ChatList.ItemsSource = conv.Messages;
        HeaderLabel.Text = $"{conv.Title}　·　{conv.Provider.Name}　·　{conv.Provider.Model}";
        ConversationList.SelectedItem = conv;
        UpdateInputState();
    }

    private void UpdateInputState()
    {
        var canSend = _current is { IsBusy: false };
        InputBox.IsEnabled = canSend;
        SendButton.IsEnabled = canSend;
        MicButton.IsEnabled = canSend && !_isListening;
    }

    // ======================= 附件输入 =======================

    private async void AttachImage_Click(object? sender, EventArgs e)
    {
        try
        {
            var photos = await MediaPicker.PickPhotosAsync(new MediaPickerOptions { Title = AppResources.AttachImage });
            if (photos is null) return;
            foreach (var photo in photos.Take(5))
                await AddAttachmentAsync(photo, isImage: true);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(AppResources.Remind, ex.Message, AppResources.Ok);
        }
    }

    private async void AttachFile_Click(object? sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = AppResources.AttachFile,
                FileTypes = null
            });
            if (file is null) return;
            await AddAttachmentAsync(file, isImage: false);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(AppResources.Remind, ex.Message, AppResources.Ok);
        }
    }

    private async void CaptureImage_Click(object? sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.CapturePhotoAsync();
            if (photo is null) return;
            await AddAttachmentAsync(photo, isImage: true);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(AppResources.Remind, ex.Message, AppResources.Ok);
        }
    }

    private async Task AddAttachmentAsync(FileResult file, bool isImage)
    {
        await using var stream = await file.OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var bytes = ms.ToArray();

        _pendingAttachments.Add(new PendingAttachment
        {
            FileName = Path.GetFileName(file.FileName) ?? (isImage ? "image.png" : "file"),
            Bytes = bytes,
            MimeType = string.IsNullOrWhiteSpace(file.ContentType)
                ? (isImage ? "image/png" : "application/octet-stream")
                : file.ContentType,
            IsImage = isImage
        });

        UpdateAttachmentPlaceholder();
    }

    private void RemoveAttachment_Click(object? sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not PendingAttachment attachment) return;
        _pendingAttachments.Remove(attachment);
        UpdateAttachmentPlaceholder();
    }

    private void UpdateAttachmentPlaceholder()
    {
        InputBox.Placeholder = _pendingAttachments.Count == 0
            ? AppResources.EnterYourMessages
            : $"{AppResources.EnterYourMessages} ({_pendingAttachments.Count} {AppResources.FileAttached})";
    }

    private void UpdateAttachmentListVisibility()
    {
        AttachmentList.IsVisible = _pendingAttachments.Count > 0;
    }

    // ======================= 语音输入 =======================

    private async void MicButton_Click(object? sender, EventArgs e)
    {
        if (_isListening)
        {
            await StopListeningAsync();
            return;
        }

        var status = await Permissions.RequestAsync<Permissions.Microphone>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlertAsync(AppResources.Remind, AppResources.MicrophonePermissionDenied, AppResources.Ok);
            return;
        }

        try
        {
            _speechCts = new CancellationTokenSource();
            _isListening = true;
            MicButton.Text = AppResources.Listening;
            UpdateInputState();

            var speech = SpeechToText.Default;
            speech.RecognitionResultUpdated += OnSpeechResultUpdated;
            speech.RecognitionResultCompleted += OnSpeechResultCompleted;

            await speech.StartListenAsync(new CommunityToolkit.Maui.Media.SpeechToTextOptions
            {
                Culture = LocalizationService.CurrentCulture,
                ShouldReportPartialResults = true,
                AutoStopSilenceTimeout = TimeSpan.FromSeconds(3)
            }, _speechCts.Token);
        }
        catch (Exception ex)
        {
            await StopListeningAsync();
            var message = ex.Message ?? "";
            var isPrivacyError = message.Contains("privacy", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Online Speech Recognition", StringComparison.OrdinalIgnoreCase);

            if (isPrivacyError)
            {
                var open = await DisplayAlertAsync(
                    AppResources.Remind,
                    AppResources.SpeechPrivacyRequired,
                    AppResources.OpenSettings,
                    AppResources.Ok);

                if (open)
                {
                    try
                    {
                        await Launcher.OpenAsync("ms-settings:privacy-speech");
                    }
                    catch { /* 忽略启动失败 */ }
                }
            }
            else
            {
                await DisplayAlertAsync(AppResources.Remind, string.Format(AppResources.SpeechToTextFailed, message), AppResources.Ok);
            }
        }
    }

    private void OnSpeechResultUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs e)
    {
        OnUi(() =>
        {
            if (!string.IsNullOrEmpty(e.RecognitionResult))
                InputBox.Text = e.RecognitionResult;
        });
    }

    private void OnSpeechResultCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs e)
    {
        OnUi(async () =>
        {
            if (e.RecognitionResult is { IsSuccessful: true, Text: not null })
                InputBox.Text = e.RecognitionResult.Text;
            await StopListeningAsync();
        });
    }

    private async Task StopListeningAsync()
    {
        _speechCts?.Cancel();
        try
        {
            await SpeechToText.Default.StopListenAsync(CancellationToken.None);
        }
        catch { }

        SpeechToText.Default.RecognitionResultUpdated -= OnSpeechResultUpdated;
        SpeechToText.Default.RecognitionResultCompleted -= OnSpeechResultCompleted;

        _isListening = false;
        MicButton.Text = AppResources.SpeechToText;
        UpdateInputState();
    }

    // ======================= 聊天 =======================

    private async void Send_Click(object? sender, EventArgs e)
    {
        var conv = _current;
        if (conv?.Agent is null || conv.Session is null || conv.IsBusy) return;

        var input = InputBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(input) && _pendingAttachments.Count == 0) return;

        var attachments = _pendingAttachments.ToList();

        var displayText = input;
        if (attachments.Count > 0)
        {
            var names = string.Join(", ", attachments.Select(a => a.FileName));
            displayText += string.IsNullOrEmpty(input)
                ? $"[{AppResources.AttachFile}: {names}]"
                : $"\n[{AppResources.AttachFile}: {names}]";
        }

        var userMessage = new Models.ChatMessage
        {
            Role = ChatMessageRole.User,
            Text = displayText
        };
        userMessage.Blocks.Add(new MessageBlock { Type = "text", Text = displayText });

        // 第一张图片直接作为消息图片显示
        var firstImage = attachments.FirstOrDefault(a => a.IsImage);
        if (firstImage is not null)
        {
            userMessage.ImageBytes = firstImage.Bytes;
            userMessage.MimeType = firstImage.MimeType;
        }

        conv.Messages.Add(userMessage);

        var isFirst = conv.Messages.Count == 1;
        if (isFirst)
        {
            if (string.IsNullOrEmpty(input) && firstImage is not null)
                conv.Title = AppResources.AttachImage;
            else
                conv.Title = input.Length > 24 ? input[..24] + "…" : input;
        }

        InputBox.Text = "";
        _pendingAttachments.Clear();
        UpdateAttachmentPlaceholder();

        var agentMessage = new Models.ChatMessage { Role = ChatMessageRole.Assistant, Text = "" };
        conv.Messages.Add(agentMessage);

        conv.IsBusy = true;
        UpdateInputState();

        try
        {
            if (!conv.IsHistorySeeded && conv.Messages.Count > 2)
            {
                var history = BuildHistoryMessages(conv, userMessage);
                await foreach (var update in conv.Agent
                                   .RunStreamingAsync(history, conv.Session)
                                   .WithCancellation(conv.Cts.Token))
                {
                    ProcessUpdate(update, agentMessage);
                }
                conv.IsHistorySeeded = true;
            }
            else
            {
                var message = BuildUserMessage(input, attachments);
                await foreach (var update in conv.Agent
                                   .RunStreamingAsync(message, conv.Session)
                                   .WithCancellation(conv.Cts.Token))
                {
                    ProcessUpdate(update, agentMessage);
                }
            }

            await SaveSessionsAsync();
        }
        catch (OperationCanceledException) { /* 用户删除会话 / 主动取消 */ }
        catch (Exception ex)
        {
            OnUi(() => conv.Messages.Add(new Models.ChatMessage { Role = ChatMessageRole.Assistant, Text = $"[Error] {ex.Message}" }));
        }
        finally
        {
            conv.IsBusy = false;
            if (ReferenceEquals(conv, _current)) UpdateInputState();
            await SaveSessionsAsync();
        }
    }

    private Microsoft.Extensions.AI.ChatMessage BuildUserMessage(string input, List<PendingAttachment> attachments)
    {
        var contents = new List<AIContent>();

        // 文字 + 文件内容
        var textBuilder = new StringBuilder(input);
        foreach (var attachment in attachments.Where(a => !a.IsImage))
        {
            textBuilder.AppendLine();
            textBuilder.AppendLine($"[File: {attachment.FileName}]");
            if (IsTextMimeType(attachment.MimeType) && attachment.Bytes is not null)
            {
                try { textBuilder.AppendLine(Encoding.UTF8.GetString(attachment.Bytes)); }
                catch { textBuilder.AppendLine("(binary file)"); }
            }
            else
            {
                textBuilder.AppendLine("(binary file attached)");
            }
        }

        var text = textBuilder.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(text))
            contents.Add(new TextContent(text));

        // 图片
        foreach (var attachment in attachments.Where(a => a.IsImage && a.Bytes is not null))
            contents.Add(new DataContent(attachment.Bytes!, attachment.MimeType ?? "image/png"));

        if (contents.Count == 0)
            contents.Add(new TextContent(""));

        return new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, contents);
    }

    private List<Microsoft.Extensions.AI.ChatMessage> BuildHistoryMessages(Conversation conv, Models.ChatMessage latest)
    {
        var messages = new List<Microsoft.Extensions.AI.ChatMessage>();
        foreach (var m in conv.Messages)
        {
            if (m.Role == ChatMessageRole.Tool) continue;
            if (ReferenceEquals(m, latest))
            {
                var latestContents = new List<AIContent>();
                if (!string.IsNullOrWhiteSpace(m.DisplayText))
                    latestContents.Add(new TextContent(m.DisplayText));
                if (m.ImageBytes is { Length: > 0 })
                    latestContents.Add(new DataContent(m.ImageBytes, m.MimeType ?? "image/png"));
                if (latestContents.Count == 0)
                    latestContents.Add(new TextContent(""));
                messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, latestContents));
                continue;
            }

            var role = m.Role == ChatMessageRole.Assistant ? ChatRole.Assistant : ChatRole.User;
            var contents = new List<AIContent>();
            if (!string.IsNullOrWhiteSpace(m.DisplayText))
                contents.Add(new TextContent(m.DisplayText));
            if (m.ImageBytes is { Length: > 0 })
                contents.Add(new DataContent(m.ImageBytes, m.MimeType ?? "image/png"));
            if (contents.Count == 0)
                contents.Add(new TextContent(""));
            messages.Add(new Microsoft.Extensions.AI.ChatMessage(role, contents));
        }
        return messages;
    }

    private static bool IsTextMimeType(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType)) return false;
        var textTypes = new[] { "text/", "application/json", "application/xml", "application/javascript" };
        return textTypes.Any(t => mimeType!.StartsWith(t, StringComparison.OrdinalIgnoreCase));
    }

    private void ProcessUpdate(AgentResponseUpdate update, Models.ChatMessage agentMessage)
    {
        foreach (var content in update.Contents)
        {
            switch (content)
            {
                case TextContent tc when !string.IsNullOrEmpty(tc.Text):
                    var chunk = tc.Text;
                    OnUi(() => AppendAgentText(agentMessage, chunk));
                    break;

                case FunctionCallContent fc:
                    var callText = FormatToolCall(fc);
                    OnUi(() => agentMessage.Blocks.Add(new MessageBlock
                    {
                        Type = "tool",
                        ToolCard = new ToolCard
                        {
                            CallId = fc.CallId,
                            Name = fc.Name,
                            CallContent = callText
                        }
                    }));
                    break;

                case FunctionResultContent fr:
                    var resultText = FormatToolResult(fr);
                    OnUi(() =>
                    {
                        var block = agentMessage.Blocks
                            .Where(b => b.Type == "tool" && b.ToolCard?.CallId == fr.CallId)
                            .Select(b => b.ToolCard)
                            .FirstOrDefault();

                        if (block is not null)
                        {
                            block.ResultContent = resultText;
                        }
                        else
                        {
                            agentMessage.Blocks.Add(new MessageBlock
                            {
                                Type = "tool",
                                ToolCard = new ToolCard
                                {
                                    CallId = fr.CallId,
                                    Name = fr.CallId ?? "result",
                                    ResultContent = resultText
                                }
                            });
                        }
                    });
                    break;
            }
        }
    }

    private static void AppendAgentText(Models.ChatMessage agentMessage, string text)
    {
        agentMessage.Text += text;

        var lastTextBlock = agentMessage.Blocks.LastOrDefault(b => b.Type == "text");
        if (lastTextBlock is not null)
        {
            lastTextBlock.Text += text;
        }
        else
        {
            agentMessage.Blocks.Add(new MessageBlock { Type = "text", Text = text });
        }
    }

    private static void OnUi(Action action)
    {
        if (MainThread.IsMainThread) action();
        else MainThread.BeginInvokeOnMainThread(action);
    }

    private async Task<Conversation> CreateConversationAsync(ProviderConfig provider, ToolMode mode,
        string? id = null, string? title = null)
    {
        var factory = ProviderRegistry.Get(provider.Kind);
        var client = factory.Create(provider);

        var conv = id is null
            ? new Conversation(provider, mode)
            : new Conversation(id, provider, mode);

        if (title is not null) conv.Title = title;

        conv.Agent = new ChatClientAgent(
            client,
            instructions: "You are a helpful assistant that can read, write, edit, and search files, " +
                          "run PowerShell commands, take screenshots, and understand images and attached files.",
            name: "DesktopAgent",
            tools: CreateTools(conv));

        conv.Session = await conv.Agent.CreateSessionAsync();
        return conv;
    }

    // ======================= 工具审批 =======================

    private async Task<bool> ApproveToolAsync(Conversation conv, string toolName, string preview)
    {
        switch (conv.ToolMode)
        {
            case ToolMode.Never: return false;
            case ToolMode.Auto: return true;
        }

        var tcs = new TaskCompletionSource<bool>();
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var ok = await DisplayAlertAsync(
                    string.Format(AppResources.AllowToolCalling, toolName), preview, AppResources.Approve, AppResources.Decline);
                tcs.SetResult(ok);
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return await tcs.Task;
    }

    // ======================= 工具实现 =======================

    private AITool[] CreateTools(Conversation conv)
    {
        return
        [
            AIFunctionFactory.Create((Func<string, Task<string>>)ReadFile),
            AIFunctionFactory.Create((Func<string, string, Task<string>>)WriteFile),
            AIFunctionFactory.Create((Func<string, string, string, Task<string>>)EditFile),
            AIFunctionFactory.Create((Func<string, string, string, bool, Task<string>>)SearchInDirectory),
            AIFunctionFactory.Create((Func<string, Task<string>>)RunPowerShell),
            AIFunctionFactory.Create((Func<Task<string>>)TakeScreenshot),
            AIFunctionFactory.Create((Func<string, List<string>, string, bool, Task<string>>)AskUser)
        ];

        [Description("Ask the user a question and wait for their answer. " +
             "This does NOT stop the conversation — the user answers while the agent keeps its session alive. " +
             "Use this when you need a decision, clarification, or preference from the user before continuing. " +
             "Supports single-choice, multiple-choice, and an optional free-form 'Other' answer.")]
        async Task<string> AskUser(
            [Description("The question to ask the user.")] string question,
            [Description("Optional list of answer options.")] List<string> options,
            [Description("'single' or 'multiple'. Defaults to 'single'.")] string selectionMode = "single",
            [Description("Whether to allow a free-form 'Other' answer. Defaults to true.")] bool allowOther = true)
        {
            var request = new AskUserRequest
            {
                Question = question,
                Options = options ?? new List<string>(),
                SelectionMode = selectionMode,
                AllowOther = allowOther
            };

            AskUserResult result;

            try
            {
                var popupResult = await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var page = this
                               ?? Application.Current?.Windows.FirstOrDefault()?.Page;

                    if (page is null)
                        return null;

                    var host = page.Navigation?.ModalStack?.LastOrDefault() as Page ?? page;
                    var popup = new AskUserPopup(request);
                    return await host.ShowPopupAsync<AskUserResult>(popup);
                });

                if (popupResult?.Result is not AskUserResult r || r.Cancelled)
                    result = new AskUserResult { Cancelled = true };
                else
                    result = r;
            }
            catch (Exception ex)
            {
                return $"Failed to ask user: {ex.Message}";
            }

            if (result.Cancelled || result.Selected.Count == 0)
                return "User cancelled the question. Proceed with your best judgment or ask again later.";

            return result.Selected.Count == 1
                ? $"User selected: {result.Selected[0]}"
                : $"User selected multiple:\n- {string.Join("\n- ", result.Selected)}";
        }

        [Description("Read the contents of a file at the specified path.")]
        async Task<string> ReadFile(
            [Description("The full path of the file.")] string path)
        {
            if (!await ApproveToolAsync(conv, "ReadFile", $"{AppResources.ToolReadFile}\n{path}"))
                return AppResources.DeniedMessage;
            try { return await File.ReadAllTextAsync(path); }
            catch (Exception ex) { return $"Failed to read file: {ex.Message}"; }
        }

        [Description("Write content to a file at the specified path. Overwrites existing content.")]
        async Task<string> WriteFile(
            [Description("The full path of the file.")] string path,
            [Description("The text content to write.")] string content)
        {
            if (!await ApproveToolAsync(conv, "WriteFile",
                    $"{AppResources.ToolWriteFile}\n{path}\n\n{AppResources.Preview}\n{Preview(content)}"))
                return AppResources.DeniedMessage;

            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(path, content);
                return $"Successfully wrote to file: {path}";
            }
            catch (Exception ex) { return $"Failed to write file: {ex.Message}"; }
        }

        [Description("Edit a file by replacing a unique anchor string with new content. " +
                     "The rest of the file is preserved. The anchor must appear exactly once.")]
        async Task<string> EditFile(
            [Description("The full path of the file.")] string path,
            [Description("The exact existing text to be replaced. Must occur exactly once.")] string anchor,
            [Description("The replacement text.")] string replacement)
        {
            if (!await ApproveToolAsync(conv, "EditFile",
                    $"{AppResources.ToolEditFile}\n{path}\n\n{AppResources.Replace}\n{Preview(anchor)}\n\n{AppResources.To}\n{Preview(replacement)}"))
                return AppResources.DeniedMessage;

            try
            {
                if (!File.Exists(path)) return $"File not found: {path}";

                var original = await File.ReadAllTextAsync(path);
                int first = original.IndexOf(anchor, StringComparison.Ordinal);
                if (first < 0) return "Anchor text was not found in the file. No changes made.";

                int second = original.IndexOf(anchor, first + anchor.Length, StringComparison.Ordinal);
                if (second >= 0)
                    return "Anchor text appears more than once. Please provide a more specific anchor.";

                var updated = original[..first] + replacement + original[(first + anchor.Length)..];
                await File.WriteAllTextAsync(path, updated);
                return $"Successfully edited file: {path}";
            }
            catch (Exception ex) { return $"Failed to edit file: {ex.Message}"; }
        }

        [Description("Search for files in a directory whose content contains the given text. " +
                     "Returns the matching file paths.")]
        async Task<string> SearchInDirectory(
            [Description("The directory to search in.")] string directory,
            [Description("The text to look for inside each file.")] string searchContent,
            [Description("Optional file glob pattern, e.g. *.cs. Defaults to *.")] string pattern = "*",
            [Description("Optional case sensitivity. Defaults to false.")] bool caseSensitive = false)
        {
            if (!await ApproveToolAsync(conv, "SearchInDirectory",
                    $"{AppResources.ToolSearch}\n{directory}\n\n{AppResources.Pattern}{pattern}\n{AppResources.Including}{Preview(searchContent)}"))
                return AppResources.DeniedMessage;

            try
            {
                if (!Directory.Exists(directory)) return $"Directory not found: {directory}";

                var comparison = caseSensitive
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;

                var matches = new List<string>();
                foreach (var file in Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories))
                {
                    try
                    {
                        var text = await File.ReadAllTextAsync(file);
                        if (text.Contains(searchContent, comparison)) matches.Add(file);
                    }
                    catch { /* 跳过非文本文件 */ }
                }

                if (matches.Count == 0) return "No files matched.";

                const int limit = 50;
                var sb = new StringBuilder();
                sb.AppendLine($"Found {matches.Count} file(s). Showing up to {limit}:");
                foreach (var m in matches.Take(limit)) sb.AppendLine(m);
                return sb.ToString();
            }
            catch (Exception ex) { return $"Search failed: {ex.Message}"; }
        }

        [Description("Execute a PowerShell command and return its output.")]
        async Task<string> RunPowerShell(
            [Description("The PowerShell command to execute.")] string command)
        {
            if (!await ApproveToolAsync(conv, "RunPowerShell", $"{AppResources.ToolRunPowerShell}\n{command}"))
                return AppResources.DeniedMessage;

            try
            {
                var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(command));

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encoded}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var outTask = process.StandardOutput.ReadToEndAsync();
                var errTask = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                var output = await outTask;
                var error = await errTask;

                return process.ExitCode != 0
                    ? $"Command failed: {error}"
                    : (string.IsNullOrWhiteSpace(output)
                        ? "Command executed successfully (no output)."
                        : output);
            }
            catch (Exception ex) { return $"Error while running PowerShell: {ex.Message}"; }
        }

        [Description("Capture the primary screen and save it as a PNG file.")]
        async Task<string> TakeScreenshot()
        {
            if (!await ApproveToolAsync(conv, "TakeScreenshot", AppResources.ToolTakeScreenshot))
                return AppResources.DeniedMessage;

            try
            {
#if WINDOWS
                var path = await AiCoder.Platforms.Windows.ScreenshotHelper.CapturePrimaryScreenAsync();
                if (path is null) return AppResources.ScreenshotNotSupported;
                return string.Format(AppResources.ScreenshotSaved, path);
#else
                await Task.CompletedTask;
                return AppResources.ScreenshotNotSupported;
#endif
            }
            catch (Exception ex)
            {
                return string.Format(AppResources.ScreenshotFailed, ex.Message);
            }
        }
    }

    // ======================= 格式化 =======================

    private static string FormatToolCall(FunctionCallContent call)
    {
        var args = call.Arguments is null
            ? "(no arguments)"
            : string.Join(", ", call.Arguments.Select(kv =>
                $"{kv.Key}={Preview(kv.Value?.ToString() ?? "null")}"));
        return $"{call.Name}({args})";
    }

    private static string FormatToolResult(FunctionResultContent result)
    {
        return result.Result?.ToString() ?? "(no result)";
    }

    private static string Preview(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "(empty)";
        const int max = 500;
        return text.Length <= max ? text : text[..max] + "\n... (truncated)";
    }
}
