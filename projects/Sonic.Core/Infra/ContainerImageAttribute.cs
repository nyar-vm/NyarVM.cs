using System;

namespace Core.Infra;

/// <summary>
///     标记一个类为容器镜像定义
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ContainerImageAttribute : Attribute
{
    /// <summary>
    ///     基础镜像
    /// </summary>
    public string? base_image { get; set; }

    /// <summary>
    ///     镜像标签
    /// </summary>
    public string? tag { get; set; }
}