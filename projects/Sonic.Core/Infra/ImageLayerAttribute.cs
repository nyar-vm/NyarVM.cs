using System;

namespace Core.Infra;

/// <summary>
///     标记一个方法为容器镜像构建层
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ImageLayerAttribute : Attribute
{
}