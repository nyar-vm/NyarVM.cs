namespace Std.Data.Text.Valkyrie.AST.Neural;

/// <summary>
///     Transformer Block 声明
/// </summary>
public sealed record TransformerBlockDecl : ValkyrieNode
{
    /// <summary>
    ///     注意力头数
    /// </summary>
    public int heads { get; init; }

    /// <summary>
    ///     嵌入维度
    /// </summary>
    public int dim { get; init; }

    /// <summary>
    ///     前馈网络维度
    /// </summary>
    public int ffn_dim { get; init; }
}