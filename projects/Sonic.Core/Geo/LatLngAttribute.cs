using System;

namespace Core.Geo;

/// <summary>
///     标记表示经纬度坐标的属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class LatLngAttribute : Attribute
{
}