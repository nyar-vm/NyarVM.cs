using Core.AI;

namespace Std.AI;

/// <summary>
///     模型推理默认实现，实现 IModelInference&lt;TInput, TOutput&gt; 接口
/// </summary>
/// <typeparam name="TInput">输入类型</typeparam>
/// <typeparam name="TOutput">输出类型</typeparam>
public sealed class ModelInference<TInput, TOutput> : IModelInference<TInput, TOutput>
{
    /// <summary>
    ///     推理函数
    /// </summary>
    private readonly Func<TInput, TOutput> _infer_func;

    /// <summary>
    ///     初始化模型推理
    /// </summary>
    /// <param name="inferFunc">推理函数</param>
    public ModelInference(Func<TInput, TOutput> inferFunc)
    {
        _infer_func = inferFunc;
    }

    /// <summary>
    ///     对给定输入执行推理
    /// </summary>
    /// <param name="input">推理输入</param>
    /// <returns>推理结果</returns>
    public TOutput infer(TInput input)
    {
        return _infer_func(input);
    }
}