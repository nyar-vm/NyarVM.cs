using System;

namespace Core.State;

/// <summary>
///     Observable 属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ObservableAttribute : Attribute
{
}