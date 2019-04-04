using System;

namespace Core.Compiler.BuildTask;

/// <summary>
///     标记类为构建任务
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class BuildTaskAttribute : Attribute
{
    /// <summary>
    ///     构建任务名称
    /// </summary>
    public string? name { get; }
}