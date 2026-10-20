using AiCoder.Models;
using Microsoft.Extensions.AI;

namespace AiCoder.Services;

/// <summary>
/// 自定义模型提供者的接入点。实现这个接口并调用 ProviderRegistry.Register 即可
/// 让新的 API 协议出现在"提供者类型"下拉里。
/// </summary>
public interface IProviderFactory
{
    /// <summary>唯一标识，存进 ProviderConfig.Kind。</summary>
    string Kind { get; }

    /// <summary>UI 下拉中显示的名字。</summary>
    string DisplayName { get; }

    /// <summary>根据配置构造 IChatClient（可以抛异常，UI 会展示）。</summary>
    IChatClient Create(ProviderConfig config);
}