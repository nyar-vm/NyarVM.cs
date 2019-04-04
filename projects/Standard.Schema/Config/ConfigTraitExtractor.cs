namespace Hermes.Config;

/// <summary>
///     从 SchemaIR 中提取 ConfigClassTrait 的适配器
/// </summary>
public sealed class ConfigTraitExtractor
{
    /// <summary>
    ///     Config source 属性名
    /// </summary>
    private const string ConfigAttrName = "config";

    /// <summary>
    ///     从 SchemaIR 中提取所有 [config] class 的特征
    /// </summary>
    /// <param name="schema">编译后的 SchemaIR</param>
    /// <returns>ConfigClassTrait 列表</returns>
    public IReadOnlyList<ConfigClassTrait> Extract(SchemaIR schema)
    {
        var traits = new List<ConfigClassTrait>();

        foreach (var classDef in schema.Classes)
        {
            var configAttr = classDef.Attributes.FirstOrDefault(a =>
                string.Equals(a.Name, ConfigAttrName, StringComparison.OrdinalIgnoreCase));

            if (configAttr is null) continue;

            var trait = ExtractFromClass(classDef, configAttr, schema.Namespace);
            traits.Add(trait);
        }

        return traits;
    }

    private static ConfigClassTrait ExtractFromClass(
        ClassDefinition classDef,
        AttributeDefinition configAttr,
        string @namespace)
    {
        var sources = ExtractSources(configAttr);
        var hasFallback = false;
        string? fallbackSource = null;
        var fieldTraits = new List<ConfigFieldTrait>();

        foreach (var arg in configAttr.Arguments)
            if (string.Equals(arg.Key, "fallback", StringComparison.OrdinalIgnoreCase))
            {
                hasFallback = true;
                fallbackSource = arg.Value;
            }

        foreach (var field in classDef.fields)
        {
            var fieldTrait = ExtractFieldTrait(field);
            fieldTraits.Add(fieldTrait);
        }

        return new ConfigClassTrait(
            classDef.Name,
            @namespace,
            sources,
            fieldTraits,
            hasFallback,
            fallbackSource);
    }

    private static IReadOnlyList<string> ExtractSources(AttributeDefinition configAttr)
    {
        var sources = new List<string>();

        foreach (var arg in configAttr.Arguments)
        {
            if (string.Equals(arg.Key, "fallback", StringComparison.OrdinalIgnoreCase)) continue;

            if (string.IsNullOrEmpty(arg.Key)) sources.Add(arg.Value);
        }

        if (sources.Count == 0) sources.Add("local:defaults");

        return sources;
    }

    private static ConfigFieldTrait ExtractFieldTrait(FieldDefinition field)
    {
        var (isSecret, isSuperSecret, cleanName) = ParseSecretPrefix(field.Name);

        var isRequired = field.Attributes.Any(a =>
            string.Equals(a.Name, "required", StringComparison.OrdinalIgnoreCase));

        var isReloadable = field.Attributes.Any(a =>
            string.Equals(a.Name, "reloadable", StringComparison.OrdinalIgnoreCase));

        var envAttr = field.Attributes.FirstOrDefault(a =>
            string.Equals(a.Name, "env", StringComparison.OrdinalIgnoreCase));
        var envName = envAttr?.Arguments.FirstOrDefault().Value;

        var validations = ExtractValidations(field.Attributes);

        return new ConfigFieldTrait(
            cleanName,
            field.FieldType,
            isSecret,
            isSuperSecret,
            isRequired,
            isReloadable,
            envName,
            field.DefaultValue,
            validations);
    }

    private static (bool isSecret, bool isSuperSecret, string cleanName) ParseSecretPrefix(string fieldName)
    {
        if (fieldName.StartsWith("@@")) return (false, true, fieldName[2..]);

        if (fieldName.StartsWith('@')) return (true, false, fieldName[1..]);

        return (false, false, fieldName);
    }

    private static IReadOnlyList<ValidationRule> ExtractValidations(IReadOnlyList<AttributeDefinition> attributes)
    {
        var rules = new List<ValidationRule>();

        foreach (var attr in attributes)
        {
            if (!string.Equals(attr.Name, "validate", StringComparison.OrdinalIgnoreCase)) continue;

            foreach (var arg in attr.Arguments)
            {
                var rule = ParseValidationArg(arg.Key, arg.Value);
                if (rule is not null) rules.Add(rule);
            }
        }

        return rules;
    }

    private static ValidationRule? ParseValidationArg(string key, string value)
    {
        switch (key.ToLowerInvariant())
        {
            case "range":
                return ParseRangeValidation(value);
            case "one_of":
                return new OneOfValidation([.. value.Split(',').Select(v => v.Trim())]);
            case "regex":
                return new RegexValidation(value);
            case "len":
                return ParseLengthValidation(value);
            case "custom":
                return new CustomValidation(value);
            default:
                return null;
        }
    }

    private static RangeValidation? ParseRangeValidation(string value)
    {
        var parts = value.Split(',');
        if (parts.Length == 2 &&
            double.TryParse(parts[0].Trim(), out var min) &&
            double.TryParse(parts[1].Trim(), out var max))
            return new RangeValidation(min, max);
        return null;
    }

    private static LengthValidation? ParseLengthValidation(string value)
    {
        var parts = value.Split(',');
        if (parts.Length == 2 &&
            int.TryParse(parts[0].Trim(), out var min) &&
            int.TryParse(parts[1].Trim(), out var max))
            return new LengthValidation(min, max);
        return null;
    }
}