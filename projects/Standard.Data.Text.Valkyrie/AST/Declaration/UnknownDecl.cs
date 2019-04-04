namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     未知声明占位节点，用于表示解析失败或尚不支持语法对应的 AST 片段
/// </summary>
/// <para>当解析器遇到无法识别的语法结构时，回退为该节点以保留错误恢复能力</para>
/// <para>示例：</para>
/// <code>
/// // 假设解析器不认识某语法，回退为 UnknownDecl
/// UnknownDecl { Content = "unsupported_syntax { ... }" }
/// </code>
public sealed record UnknownDecl : ValkyrieNode
{
    /// <summary>
    ///     原始文本内容
    /// </summary>
    public string content { get; init; } = string.Empty;
}