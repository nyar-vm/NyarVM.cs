using System.Reflection;
using System.Text.RegularExpressions;

namespace Std.Net.Routing;

/// <summary>
///     模型验证器，检查对象上的验证属性。
/// </summary>
public static class ModelValidator
{
    /// <summary>
    ///     验证对象上的所有验证属性，返回错误列表。
    /// </summary>
    /// <param name="model">待验证对象</param>
    /// <returns>错误信息列表</returns>
    public static IReadOnlyList<ValidationError> validate(object model)
    {
        var errors = new List<ValidationError>();
        var properties = model.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var value = property.GetValue(model);

            foreach (var attr in property.GetCustomAttributes())
            {
                var error = validate_attribute(attr, property.Name, value);
                if (error != null) errors.Add(error);
            }
        }

        return errors;
    }

    private static ValidationError? validate_attribute(Attribute attr, string propertyName, object? value)
    {
        switch (attr)
        {
            case RequiredAttribute:
                if (value == null || (value is string s && string.IsNullOrWhiteSpace(s)))
                    return new ValidationError(propertyName, $"字段 '{propertyName}' 为必填项");
                break;

            case MaxLengthAttribute maxLen when value is string str:
                if (str.Length > maxLen.length)
                    return new ValidationError(propertyName,
                        $"字段 '{propertyName}' 长度不能超过 {maxLen.length}（当前 {str.Length}）");
                break;

            case MinLengthAttribute minLen when value is string str:
                if (str.Length < minLen.length)
                    return new ValidationError(propertyName,
                        $"字段 '{propertyName}' 长度不能少于 {minLen.length}（当前 {str.Length}）");
                break;

            case RangeAttribute range when value != null:
                if (value is IComparable comparable)
                    if (comparable.CompareTo(range.minimum) < 0
                        || comparable.CompareTo(range.maximum) > 0)
                        return new ValidationError(propertyName,
                            $"字段 '{propertyName}' 值必须在 {range.minimum} 到 {range.maximum} 之间");

                break;

            case StringLengthAttribute sl when value is string str:
                if (str.Length < sl.minimum_length || str.Length > sl.maximum_length)
                    return new ValidationError(propertyName,
                        $"字段 '{propertyName}' 长度必须在 {sl.minimum_length} 到 {sl.maximum_length} 之间（当前 {str.Length}）");
                break;

            case EmailAttribute when value is string email:
                var emailPattern = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailPattern.IsMatch(email))
                    return new ValidationError(propertyName, $"字段 '{propertyName}' 不是有效的邮箱地址");
                break;
        }

        return null;
    }
}

/// <summary>
///     验证错误信息。
/// </summary>
public sealed class ValidationError
{
    /// <summary>
    ///     初始化验证错误。
    /// </summary>
    /// <param name="propertyName">字段名称</param>
    /// <param name="message">错误描述</param>
    public ValidationError(string propertyName, string message)
    {
        property_name = propertyName;
        this.message = message;
    }

    /// <summary>
    ///     字段名称。
    /// </summary>
    public string property_name { get; init; }

    /// <summary>
    ///     错误描述。
    /// </summary>
    public string message { get; init; }
}