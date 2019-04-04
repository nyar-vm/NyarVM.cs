namespace Std.Data.Text.Valkyrie.AST.Neural;

/// <summary>
///     全连接（Dense）Neural 层声明
/// </summary>
/// <para>示例：</para>
/// <code>
/// layer dense1 = Dense { input = 784, output = 256, activation = "relu" };
/// layer dense2 = Dense { input = 256, output = 10, activation = "softmax" };
/// </code>
public sealed record DenseLayerDecl : NeuralLayerDecl
{
    /// <summary>
    ///     输入特征维度
    /// </summary>
    public int input_size { get; init; }

    /// <summary>
    ///     输出特征维度
    /// </summary>
    public int output_size { get; init; }

    /// <summary>
    ///     激活函数名称（如 <c>"relu"</c>、<c>"sigmoid"</c>、<c>"softmax"</c>），默认为 <c>"relu"</c>
    /// </summary>
    public string activation { get; init; } = "relu";
}