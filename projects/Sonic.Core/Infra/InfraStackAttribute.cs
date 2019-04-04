using System;

namespace Core.Infra;

/// <summary>
///     标记一个类为基础设施栈定义
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class InfraStackAttribute : Attribute
{
    /// <summary>
    ///     基础设施栈名称
    /// </summary>
    public string? name { get; set; }
}