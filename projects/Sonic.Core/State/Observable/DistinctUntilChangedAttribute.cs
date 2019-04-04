using System;

namespace Core.State.Observable;

/// <summary>
///     DistinctUntilChanged 属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DistinctUntilChangedAttribute : Attribute
{
}