using System;

namespace Core.AI;

/// <summary>
///     标记 AI 模型类的元数据属性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AiModelAttribute : Attribute
{
    /// <summary>
    ///     模型路径
    /// </summary>
    public string? model_path { get; set; }

    /// <summary>
    ///     模型版本
    /// </summary>
    public string? version { get; set; }
}