using System;

namespace Core.State;

/// <summary>
///     Derived 属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DerivedAttribute : Attribute
{
}