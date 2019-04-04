namespace Core.Math.Tensor;

/// <summary>
///     优化器接口，定义参数更新的单步操作。
/// </summary>
public interface IOptimizer
{
    /// <summary>
    ///     执行一次参数更新步骤。
    /// </summary>
    void step();
}