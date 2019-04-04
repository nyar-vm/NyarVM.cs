namespace Nyar.PartialEvaluate;

/// <summary>
///     标记一个方法支持部分求值——当所有参数为静态已知值时直接求值，否则走动态路径
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class StaticAwareAttribute : Attribute
{
}