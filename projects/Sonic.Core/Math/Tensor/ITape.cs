namespace Core.Math.Tensor;

/// <summary>
///     磁带接口，支持自动微分的前向传播和反向传播。
/// </summary>
public interface ITape
{
    /// <summary>
    ///     执行前向传播。
    /// </summary>
    void forward();

    /// <summary>
    ///     执行反向传播。
    /// </summary>
    void backward();
}