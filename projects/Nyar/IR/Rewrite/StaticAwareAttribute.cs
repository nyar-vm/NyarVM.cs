namespace Nyar.IR.Rewrite;

/// <summary>
///     标记方法支持部分求值（Partial Evaluation）。
///     SourceGenerator 为标记的方法生成静态/动态双路径包装器：
///     - 当所有参数在编译期已知时走静态求值路径
///     - 否则走动态运行时路径
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class StaticAwareAttribute : Attribute
{
    /// <summary>
    ///     是否在编译期强制内联静态求值结果
    /// </summary>
    public bool force_inline { get; set; }

    /// <summary>
    ///     最大递归深度，防止无限展开
    /// </summary>
    public int max_depth { get; set; } = 32;
}