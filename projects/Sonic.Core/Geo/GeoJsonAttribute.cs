using System;

namespace Core.Geo;

/// <summary>
///     标记支持 GeoJSON 序列化的类型
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class GeoJsonAttribute : Attribute
{
}