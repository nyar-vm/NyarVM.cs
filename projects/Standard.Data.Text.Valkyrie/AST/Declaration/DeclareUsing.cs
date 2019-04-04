namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     Using 导入声明，引入外部模块或命名空间
/// </summary>
/// <para>支持多种导入形式：</para>
/// <code>
/// using std.math;                  # 导入整个模块
/// using std.math.{sin, cos};       # 选择性导入
/// using! std.math.{sin, cos};      # 选择性导入并重新导出
/// using std.collections.HashMap as Map;   # 带别名导入
/// using {std::{symbol1}, core::{symbol2}}
/// </code>
public sealed record DeclareUsing : ValkyrieNode
{
    /// <summary>
    ///     是否为重新导出导入。
    ///     <c>using!</c> 为 <see langword="true" />，普通 <c>using</c> 为 <see langword="false" />。
    /// </summary>
    public bool is_reexport { get; init; }

    /// <summary>
    ///     导入的模块路径
    /// </summary>
    public string module_path { get; init; } = string.Empty;

    /// <summary>
    ///     目标命名空间（当从模块中导入特定命名空间时）
    /// </summary>
    public QualifiedPathNode @namespace { get; init; }

    /// <summary>
    ///     模块别名，为 <c>null</c> 时使用原名
    /// </summary>
    public IdentifierNode? alias { get; init; }

    /// <summary>
    ///     选择性导入的名称列表（<c>{ a, b } from module</c> 形式）
    /// </summary>
    public IReadOnlyList<IdentifierNode> selections { get; init; } = [];

    /// <summary>
    ///     类型别名列表（<c>as Alias</c> 形式）
    /// </summary>
    public IReadOnlyList<UsingAliasEntry> type_aliases { get; init; } = [];
}
