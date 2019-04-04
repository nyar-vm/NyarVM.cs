namespace Std.Data.Text.Valkyrie.AST.Neural;

/// <summary>
///     Neural 网络层声明的抽象基类
/// </summary>
/// <para>子类型：<see cref="DenseLayerDecl" />（全连接层）、<see cref="ConvLayerDecl" />（卷积层）</para>
/// <para>示例：</para>
/// <code>
/// layer dense1 = Dense { input = 784, output = 256, activation = "relu" };
/// layer conv1 = Conv2D { input_channels = 3, output_channels = 32, kernel_size = 3 };
/// </code>
public abstract record NeuralLayerDecl : ValkyrieNode
{
    /// <summary>
    ///     层名称
    /// </summary>
    public string name { get; init; } = string.Empty;
}