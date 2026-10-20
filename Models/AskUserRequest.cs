using System.ComponentModel;

namespace AiCoder.Models;

/// <summary>
/// AskUser 工具的输入模型。AI 通过填充这个对象来描述要问用户什么。
/// </summary>
public sealed class AskUserRequest
{
    [Description("The question to present to the user.")]
    public string Question { get; set; } = "";

    [Description("Optional list of answer options the user can choose from.")]
    public List<string> Options { get; set; } = new();

    [Description("Selection mode: 'single' for single choice, 'multiple' for multiple choice. Defaults to 'single'.")]
    public string SelectionMode { get; set; } = "single";

    [Description("Whether to show an 'Other' option that allows the user to type a free-form answer. Defaults to true.")]
    public bool AllowOther { get; set; } = true;
}