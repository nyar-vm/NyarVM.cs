namespace Core.AI;

/// <summary>
///     模型推理接口，将输入转换为输出
/// </summary>
/// <typeparam name="TInput">输入类型</typeparam>
/// <typeparam name="TOutput">输出类型</typeparam>
public interface IModelInference<TInput, TOutput>
{
    /// <summary>
    ///     对给定输入执行推理
    /// </summary>
    /// <param name="input">推理输入</param>
    /// <returns>推理结果</returns>
    TOutput infer(TInput input);
}