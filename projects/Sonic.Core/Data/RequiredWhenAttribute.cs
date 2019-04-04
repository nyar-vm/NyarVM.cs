using System;

namespace Core.Data;

/// <summary>
///     当依赖字段等于指定值时，标记当前字段为必填。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RequiredWhenAttribute : Attribute
{
    /// <summary>
    ///     初始化条件必填验证特性。
    /// </summary>
    /// <param name="dependentField">依赖字段的名称。</param>
    /// <param name="expectedValue">依赖字段的期望值。</param>
    public RequiredWhenAttribute(string dependentField, string expectedValue)
    {
        dependent_field = dependentField;
        expected_value = expectedValue;
    }

    /// <summary>
    ///     依赖字段的名称。
    /// </summary>
    public string dependent_field { get; }

    /// <summary>
    ///     依赖字段的期望值。
    /// </summary>
    public string expected_value { get; }
}