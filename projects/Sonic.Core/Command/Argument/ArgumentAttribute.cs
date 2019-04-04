using System;

namespace Core.Command.Argument;

/// <summary>
///     参数特性，标注位置参数
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ArgumentAttribute : Attribute
{
    /// <summary>
    ///     标注位置参数
    /// </summary>
    /// <param name="position">参数位置（从 0 开始）</param>
    /// <param name="description">参数描述</param>
    public ArgumentAttribute(int position, string description)
    {
        this.position = position;
        this.description = description;
    }

    /// <summary>
    ///     参数位置（从 0 开始）
    /// </summary>
    public int position { get; }

    /// <summary>
    ///     参数描述
    /// </summary>
    public string description { get; }

    /// <summary>
    ///     是否必填
    /// </summary>
    public bool required { get; set; }
}