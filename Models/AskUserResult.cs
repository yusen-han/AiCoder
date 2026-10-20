namespace AiCoder.Models;

/// <summary>
/// AskUser 工具的输出模型，会被序列化后交给 AI。
/// </summary>
public sealed class AskUserResult
{
    /// <summary>用户选择的所有选项文本（包括 Other 文本）。</summary>
    public List<string> Selected { get; set; } = new();

    /// <summary>用户是否点了取消 / 点了弹窗外侧关闭。</summary>
    public bool Cancelled { get; set; }

    /// <summary>是否是多选模式。</summary>
    public bool IsMultiple { get; set; }
}