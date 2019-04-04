using System;

namespace Core.Command;

/// <summary>
///     补全类型提示特性，标注参数的补全类型与可选的文件扩展名
///     由 Source Generator 生成补全元数据
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ValueHintAttribute : Attribute
{
    /// <summary>
    ///     标注补全提示类型
    /// </summary>
    /// <param name="hintType">补全类型</param>
    public ValueHintAttribute(ValueHintType hintType)
    {
        hint_type = hintType;
    }

    /// <summary>
    ///     补全提示类型
    /// </summary>
    public ValueHintType hint_type { get; }

    /// <summary>
    ///     适用于文件路径的扩展名过滤（如 [".json", ".yaml"]）
    /// </summary>
    public string[]? extensions { get; set; }
}