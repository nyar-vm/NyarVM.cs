using System;

namespace Core.Infra;

/// <summary>
///     标记一个方法为基础设施函数
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class InfraFunctionAttribute : Attribute
{
    /// <summary>
    ///     运行时标识
    /// </summary>
    public string? runtime { get; set; }
}