using System;

namespace Core.Compiler.Verification;

/// <summary>
///     标记方法为基准测试
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class BenchmarkAttribute : Attribute
{
    /// <summary>
    ///     基准测试迭代次数，默认为 100
    /// </summary>
    public int iterations { get; set; } = 100;

    /// <summary>
    ///     预热迭代次数，默认为 10
    /// </summary>
    public int warmup_iterations { get; set; } = 10;
}