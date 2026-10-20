using System.Collections.ObjectModel;
using System.ComponentModel;
using AiCoder.Models;
using Microsoft.Agents.AI;

namespace AiCoder.Models;

/// <summary>
/// 一次独立的对话。持有自己的 Agent / Session / 消息集合 / 取消令牌，
/// 因此多个会话可以真正并行地跑流式请求。
/// Provider 在构造时确定，之后不可更改。
/// </summary>
public sealed class Conversation : INotifyPropertyChanged
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public ProviderConfig Provider { get; }
    public ToolMode ToolMode { get; }
    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public CancellationTokenSource Cts { get; } = new();

    public AIAgent? Agent { get; set; }
    public AgentSession? Session { get; set; }

    /// <summary>
    /// 从磁盘恢复后，首次发送时需要把历史消息喂给新 Session。
    /// 此标记记录是否已经喂过。
    /// </summary>
    public bool IsHistorySeeded { get; set; }

    private string _title = "New Session";
    public string Title
    {
        get => _title;
        set { if (_title == value) return; _title = value; Raise(nameof(Title)); }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { if (_isBusy == value) return; _isBusy = value; Raise(nameof(IsBusy)); }
    }

    public Conversation(ProviderConfig provider, ToolMode toolMode)
    {
        Provider = provider;
        ToolMode = toolMode;
    }

    public Conversation(string id, ProviderConfig provider, ToolMode toolMode)
    {
        Id = id;
        Provider = provider;
        ToolMode = toolMode;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

public sealed record NewConversationResult(ProviderConfig Provider, ToolMode ToolMode);
