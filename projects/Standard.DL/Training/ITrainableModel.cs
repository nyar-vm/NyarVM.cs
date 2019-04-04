using Std.DL.Flux;

namespace Std.DL.Training;

/// <summary>
///     可训练模型接口 —— 扩展 ILayer，要求实现 Forward 方法
/// </summary>
public interface ITrainableModel : ILayer
{
    /// <summary>
    ///     前向传播（无自动微分）
    /// </summary>
    /// <param name="input">输入张量</param>
    /// <returns>输出张量</returns>
    ArrayND forward(ArrayND input);

    /// <summary>
    ///     前向传播（带自动微分记录）
    /// </summary>
    /// <param name="input">输入张量</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>输出张量</returns>
    ArrayND forward(ArrayND input, AutogradContext ctx);
}