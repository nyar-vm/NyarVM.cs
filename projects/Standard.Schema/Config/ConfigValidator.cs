using System.Text.RegularExpressions;

namespace Hermes.Config;

/// <summary>
///     Config 验证器 — 启动时 Fail-fast 验证
/// </summary>
public sealed class ConfigValidator
{
    /// <summary>
    ///     验证单个 ConfigClassTrait 的解析结果
    /// </summary>
    /// <param name="trait">配置类特征</param>
    /// <param name="resolvedValues">从 provider 链解析后的字段值字典（字段名 → 值字符串）</param>
    /// <returns>错误列表，为空表示全部通过</returns>
    public IReadOnlyList<ConfigValidationError> Validate(ConfigClassTrait trait,
        IReadOnlyDictionary<string, string?> resolvedValues)
    {
        var errors = new List<ConfigValidationError>();

        foreach (var field in trait.fields)
        {
            if (field.IsSuperSecret)
            {
                errors.AddRange(ValidateSuperSecretField(trait, field, resolvedValues));
                continue;
            }

            errors.AddRange(ValidateField(trait, field, resolvedValues));
        }

        return errors;
    }

    private static IReadOnlyList<ConfigValidationError> ValidateField(
        ConfigClassTrait trait,
        ConfigFieldTrait field,
        IReadOnlyDictionary<string, string?> resolvedValues)
    {
        var errors = new List<ConfigValidationError>();

        var hasValue = resolvedValues.TryGetValue(field.FieldName, out var rawValue);
        var value = hasValue ? rawValue : null;

        if (field.IsRequired && string.IsNullOrEmpty(value))
        {
            errors.Add(new ConfigValidationError(
                trait.ClassName, field.FieldName,
                "必填字段缺失值"));
            return errors;
        }

        if (field.IsSecret && string.IsNullOrEmpty(value))
        {
            errors.Add(new ConfigValidationError(
                trait.ClassName, field.FieldName,
                "机密字段缺失值"));
            return errors;
        }

        if (!hasValue || value is null) return errors;

        foreach (var rule in field.Validations)
        {
            var ruleError = ApplyRule(trait.ClassName, field, value, rule);
            if (ruleError is not null) errors.Add(ruleError);
        }

        return errors;
    }

    private static IReadOnlyList<ConfigValidationError> ValidateSuperSecretField(
        ConfigClassTrait trait,
        ConfigFieldTrait field,
        IReadOnlyDictionary<string, string?> resolvedValues)
    {
        var errors = new List<ConfigValidationError>();

        if (!resolvedValues.TryGetValue(field.FieldName, out var rawValue) || string.IsNullOrEmpty(rawValue))
            errors.Add(new ConfigValidationError(
                trait.ClassName, field.FieldName,
                "超级机密字段缺失值"));

        return errors;
    }

    private static ConfigValidationError? ApplyRule(
        string className,
        ConfigFieldTrait field,
        string value,
        ValidationRule rule)
    {
        return rule switch
        {
            RangeValidation r => ValidateRange(className, field, value, r),
            OneOfValidation o => ValidateOneOf(className, field, value, o),
            RegexValidation rx => ValidateRegex(className, field, value, rx),
            LengthValidation l => ValidateLength(className, field, value, l),
            _ => null
        };
    }

    private static ConfigValidationError? ValidateRange(string className, ConfigFieldTrait field, string value,
        RangeValidation rule)
    {
        if (!double.TryParse(value, out var num))
            return new ConfigValidationError(className, field.FieldName,
                $"值 \"{value}\" 不是有效数字", value);

        if (num < rule.Min || num > rule.Max)
            return new ConfigValidationError(className, field.FieldName,
                $"值 {num} 不在范围 [{rule.Min}, {rule.Max}] 内", value);

        return null;
    }

    private static ConfigValidationError? ValidateOneOf(string className, ConfigFieldTrait field, string value,
        OneOfValidation rule)
    {
        if (!rule.Values.Contains(value, StringComparer.OrdinalIgnoreCase))
            return new ConfigValidationError(className, field.FieldName,
                $"值 \"{value}\" 不在允许值 [{string.Join(", ", rule.Values)}] 中", value);

        return null;
    }

    private static ConfigValidationError? ValidateRegex(string className, ConfigFieldTrait field, string value,
        RegexValidation rule)
    {
        if (!Regex.IsMatch(value, rule.Pattern))
            return new ConfigValidationError(className, field.FieldName,
                $"值 \"{value}\" 不匹配模式 \"{rule.Pattern}\"", value);

        return null;
    }

    private static ConfigValidationError? ValidateLength(string className, ConfigFieldTrait field, string value,
        LengthValidation rule)
    {
        var length = value.Length;
        if (length < rule.MinLength || length > rule.MaxLength)
            return new ConfigValidationError(className, field.FieldName,
                $"值长度 {length} 不在范围 [{rule.MinLength}, {rule.MaxLength}] 内", value);

        return null;
    }
}