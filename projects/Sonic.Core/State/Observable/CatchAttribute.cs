using System;

namespace Core.State.Observable;

/// <summary>
///     Catch 属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CatchAttribute : Attribute
{
    /// <summary>
    ///     错误处理方法名
    /// </summary>
    public string? handler { get; set; }
}