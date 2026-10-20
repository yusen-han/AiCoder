using System.ComponentModel;

namespace AiCoder.Models;

public sealed class ToolCard : INotifyPropertyChanged
{
    private string _callId = "";
    public string CallId
    {
        get => _callId;
        set { if (_callId == value) return; _callId = value; Raise(nameof(CallId)); }
    }

    private string _name = "";
    public string Name
    {
        get => _name;
        set { if (_name == value) return; _name = value; Raise(nameof(Name)); }
    }

    private string? _callContent;
    public string? CallContent
    {
        get => _callContent;
        set { if (_callContent == value) return; _callContent = value; Raise(nameof(CallContent)); }
    }

    private string? _resultContent;
    public string? ResultContent
    {
        get => _resultContent;
        set
        {
            if (_resultContent == value) return;
            _resultContent = value;
            Raise(nameof(ResultContent));
            Raise(nameof(HasResult));
        }
    }

    public bool HasResult => !string.IsNullOrEmpty(ResultContent);

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
