using System;

namespace Core.Command;

/// <summary>
///     参数互斥约束，标注当前选项与指定选项不能同时出现
///     由 Source Generator 在编译时展开校验代码
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class ConflictsWithAttribute : Attribute
{
    /// <summary>
    ///     标注互斥关系
    /// </summary>
    /// <param name="optionName">互斥的选项长名称</param>
    public ConflictsWithAttribute(string optionName)
    {
        option_name = optionName;
    }

    /// <summary>
    ///     互斥的选项长名称
    /// </summary>
    public string option_name { get; }
}