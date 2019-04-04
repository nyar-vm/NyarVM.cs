using Nyar.Language.Css;

namespace Std.Data.Text.Awsl;

/// <summary>
///     Awsl 组件解析结果
/// </summary>
public sealed class AwslParseResult
{
    /// <summary>
    ///     组件名称
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     属性声明列表
    /// </summary>
    public IReadOnlyList<AwslProperty> properties { get; init; } = [];

    /// <summary>
    ///     函数声明列表
    /// </summary>
    public IReadOnlyList<AwslMethod> methods { get; init; } = [];

    /// <summary>
    ///     模板节点列表
    /// </summary>
    public IReadOnlyList<AwslTemplateNode> template_nodes { get; init; } = [];

    /// <summary>
    ///     样式定义
    /// </summary>
    public CssStylesheet styles { get; init; } = new();
}