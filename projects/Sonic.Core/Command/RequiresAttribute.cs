using System;

namespace Core.Command;

/// <summary>
///     参数依赖约束，标注当前选项必须在指定选项同时给出时才有效
///     由 Source Generator 在编译时展开校验代码
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class RequiresAttribute : Attribute
{
    /// <summary>
    ///     标注选项依赖关系
    /// </summary>
    /// <param name="optionName">依赖的选项长名称</param>
    public RequiresAttribute(string optionName)
    {
        option_name = optionName;
    }

    /// <summary>
    ///     依赖的选项长名称（如 "config" 对应 --config）
    /// </summary>
    public string option_name { get; }
}