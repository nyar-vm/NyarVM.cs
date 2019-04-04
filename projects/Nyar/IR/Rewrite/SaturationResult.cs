namespace Nyar.IR.Rewrite;

/// <summary>
///     饱和优化结果
/// </summary>
/// <param name="iterations">实际迭代次数。</param>
/// <param name="total_unions">合并操作总数。</param>
/// <param name="is_saturated">是否达到饱和。</param>
public readonly record struct SaturationResult(int iterations, int total_unions, bool is_saturated);