namespace Std.Data.Text.Valkyrie.AST;

/// <summary>
///     限定路径表达式，表示带模块路径的完整名称引用
/// </summary>
/// <para>示例：</para>
/// <code>
/// Game.Sonic.Core.Vector3         // Segments = ["Game", "Sonic.Core", "Vector3"]
/// ::Game.Sonic.Core.normalize     // IsGlobal = true, Segments = ["Game", "Sonic.Core", "normalize"]
/// </code>
public sealed record QualifiedPathNode : ValkyrieNode
{
    /// <summary>
    ///     是否为全局限定路径（以 <c>::</c> 开头）
    /// </summary>
    public bool is_global { get; init; }

    /// <summary>
    ///     路径段列表
    /// </summary>
    public IReadOnlyList<IdentifierNode> segments { get; init; } = [];

    public string name => segments.Count > 0 ? segments[^1].name : string.Empty;

    /// <summary>
    ///     完整路径名称（段之间用 <c>::</c> 连接）
    /// </summary>
    public string full_name => string.Join("::", segments.Select(segment => segment.name));
}