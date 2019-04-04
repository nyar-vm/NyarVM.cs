using System;

namespace Core.Command;

/// <summary>
///     命令特性，标注命令的名称与描述
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CommandAttribute : Attribute
{
    /// <summary>
    ///     标注命令名称与描述
    /// </summary>
    /// <param name="name">命令名称</param>
    /// <param name="description">命令描述</param>
    public CommandAttribute(string name, string description)
    {
        this.name = name;
        this.description = description;
    }

    /// <summary>
    ///     命令名称
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     命令描述
    /// </summary>
    public string description { get; }

    /// <summary>
    ///     命令别名
    /// </summary>
    public string[] alias { get; set; } = [];

    /// <summary>
    ///     是否在帮助中隐藏此命令
    /// </summary>
    public bool hidden { get; set; }
}