using Std.Data.Text.Valkyrie.AST.Term;

namespace Std.Data.Text.Valkyrie.AST.Neural;

/// <summary>
///     Neural 层参数声明（键值对形式的配置参数）
/// </summary>
/// <para>示例：</para>
/// <code>
/// layer conv1 = Conv2D {
///     input_channels = 3;
///     output_channels = 32;
///     kernel_size = 3;
/// }
/// // 每个键值对对应一个 NeuralLayerParamDecl
/// // NeuralLayerParamDecl { Name = "input_channels", Value = LiteralExpr(3) }
/// </code>
public sealed record NeuralLayerParamDecl : ValkyrieNode
{
    /// <summary>
    ///     参数名称
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     参数值
    /// </summary>
    public TermNode value { get; init; } = null!;
}