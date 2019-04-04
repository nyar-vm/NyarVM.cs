namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     Using 别名条目，为完整类型路径指定一个短别名
/// </summary>
/// <para>示例：</para>
/// <code>
/// using std.collections.HashMap as Map;
/// //                          ^^^^  UsingAliasEntry { AliasName = "Map", TargetPath = "std.collections.HashMap" }
/// </code>
public sealed record UsingAliasEntry
{
    /// <summary>
    ///     别名名称
    /// </summary>
    public string alias_name { get; init; } = string.Empty;

    /// <summary>
    ///     目标类型的完整路径
    /// </summary>
    public string target_path { get; init; } = string.Empty;
}