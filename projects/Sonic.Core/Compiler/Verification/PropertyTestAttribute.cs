using System;

namespace Core.Compiler.Verification;

/// <summary>
///     标记方法为基于属性的测试
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PropertyTestAttribute : Attribute
{
    /// <summary>
    ///     测试迭代次数，默认为 100
    /// </summary>
    public int iterations { get; set; } = 100;

    /// <summary>
    ///     最大收缩次数，默认为 100
    /// </summary>
    public int max_shrinks { get; set; } = 100;
}