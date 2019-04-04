using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sonic.Data.Generator;

internal static class DataGeneratorAttributeFacts
{
    public const string data_attribute_full_name = "Core.Data.DataAttribute";

    public static readonly string[] field_attribute_full_names =
    [
        "Core.Data.FieldAttribute",
        "Core.Data.KeyAttribute",
        "Sonic.Standard.Data.Attributes.FieldAttribute",
        "Sonic.Standard.Data.FieldAttribute"
    ];

    public static readonly string[] ignore_attribute_full_names =
    [
        "Core.Data.IgnoreAttribute",
        "Core.Data.IgnoreDataAttribute",
        "Sonic.Standard.Data.Attributes.IgnoreDataAttribute",
        "Sonic.Standard.Data.IgnoreDataAttribute"
    ];

    public static readonly string[] alias_attribute_full_names =
    [
        "Core.Data.AliasAttribute"
    ];

    public static bool has_any_attribute(ImmutableArray<AttributeData> attributes, IEnumerable<string> fullNames)
    {
        return get_first_attribute(attributes, fullNames) is not null;
    }

    public static AttributeData? get_first_attribute(ImmutableArray<AttributeData> attributes, IEnumerable<string> fullNames)
    {
        foreach (var fullName in fullNames)
        {
            var attribute = attributes.FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == $"global::{fullName}");
            if (attribute is not null)
            {
                return attribute;
            }
        }

        return null;
    }

    public static string? get_explicit_binding_name(ImmutableArray<AttributeData> attributes)
    {
        var fieldAttr = get_first_attribute(attributes, field_attribute_full_names);
        if (fieldAttr is null)
        {
            return null;
        }

        if (try_get_attribute_string_argument(fieldAttr, out var constructorName) &&
            !string.IsNullOrWhiteSpace(constructorName))
        {
            return constructorName;
        }

        if (try_get_named_attribute_string_argument(fieldAttr, "name", out var namedName) &&
            !string.IsNullOrWhiteSpace(namedName))
        {
            return namedName;
        }

        return null;
    }

    public static ImmutableArray<string> get_binding_names(ImmutableArray<AttributeData> attributes, string fallbackName)
    {
        var builder = ImmutableArray.CreateBuilder<string>();
        add_unique_name(builder, get_explicit_binding_name(attributes) ?? fallbackName);

        foreach (var attribute in get_attributes(attributes, alias_attribute_full_names))
        {
            if (try_get_attribute_string_argument(attribute, out var aliasName))
            {
                add_unique_name(builder, aliasName);
            }
        }

        return builder.ToImmutable();
    }

    public static bool try_get_attribute_string_argument(AttributeData attribute, out string? value)
    {
        if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string name)
        {
            value = name;
            return true;
        }

        value = null;
        return false;
    }

    public static bool try_get_named_attribute_string_argument(AttributeData attribute, string argumentName,
        out string? value)
    {
        foreach (var named in attribute.NamedArguments)
        {
            if (named.Key == argumentName && named.Value.Value is string stringValue)
            {
                value = stringValue;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static IEnumerable<AttributeData> get_attributes(ImmutableArray<AttributeData> attributes,
        IEnumerable<string> fullNames)
    {
        foreach (var attribute in attributes)
        {
            var fullName = attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (fullName is null)
            {
                continue;
            }

            if (fullNames.Any(candidate => fullName == $"global::{candidate}"))
            {
                yield return attribute;
            }
        }
    }

    private static void add_unique_name(ImmutableArray<string>.Builder builder, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (builder.Any(existing => string.Equals(existing, name, StringComparison.Ordinal)))
        {
            return;
        }

        builder.Add(name);
    }
}
