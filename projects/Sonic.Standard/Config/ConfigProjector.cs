using System.Collections;
using System.Reflection;
using Core.Data;
using Std.Config.Node;

namespace Std.Config;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigPathAttribute : Attribute
{
    public ConfigPathAttribute(string path)
    {
        this.path = path;
    }

    public string path { get; }
}

public enum MergeMode
{
    replace,
    deep_merge,
    append,
    union
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class MergeAttribute : Attribute
{
    public MergeAttribute(MergeMode mode)
    {
        this.mode = mode;
    }

    public MergeMode mode { get; }
}

[Obsolete("请改用 [ConfigPath] 或共享的 [Key]。")]
[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigProjectAttribute : Attribute
{
    public ConfigProjectAttribute(string path)
    {
        this.path = path;
    }

    public string path { get; }
}

[Obsolete("请改用 [Merge]。")]
public enum ConfigMergeMode
{
    replace,
    deep_merge,
    append,
    union
}

[Obsolete("请改用 [Merge]。")]
[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigMergeAttribute : Attribute
{
    public ConfigMergeAttribute(ConfigMergeMode mode)
    {
        this.mode = mode;
    }

    public ConfigMergeMode mode { get; }
}

/// <summary>
///     将 Sonic 的 `ConfigNode` 投影到配置 DTO。
///     merge/projection 行为由属性特性声明，不再散落在语言层 loader 中手写。
/// </summary>
public static class ConfigProjector
{
    public static T project<T>(ObjectConfigNode root) where T : new()
    {
        var target = new T();
        project_into(target, root);
        return target;
    }

    public static void project_into(object target, ObjectConfigNode root)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(root);

        foreach (var property in target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || !property.CanWrite || DataMemberReflection.should_ignore(property))
            {
                continue;
            }

            if (DataMemberReflection.should_flatten(property))
            {
                if (!try_project_flattened_object(target, root, property))
                {
                    continue;
                }

                continue;
            }

            var node = find_binding_node(root, property);
            if (node is null)
            {
                continue;
            }

            var mergeMode = get_merge_mode(property);
            var currentValue = property.GetValue(target);
            if (!try_project_value(node, property.PropertyType, currentValue, mergeMode, out var projected))
            {
                continue;
            }

            property.SetValue(target, projected);
        }
    }

    private static bool try_project_flattened_object(object target, ObjectConfigNode root, PropertyInfo property)
    {
        var currentValue = property.GetValue(target);
        var nested = currentValue ?? Activator.CreateInstance(property.PropertyType);
        if (nested is null)
        {
            return false;
        }

        project_into(nested, root);
        property.SetValue(target, nested);
        return true;
    }

    private static ConfigNode? get_path(ObjectConfigNode root, string path)
    {
        ConfigNode? current = root;
        foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            current = current is ObjectConfigNode obj ? obj.get_field(part) : null;
            if (current is null)
            {
                return null;
            }
        }

        return current;
    }

    private static ConfigNode? find_binding_node(ObjectConfigNode root, PropertyInfo property)
    {
        foreach (var path in get_binding_paths(property))
        {
            if (get_path(root, path) is { } node)
            {
                return node;
            }
        }

        return null;
    }

    private static bool try_project_value(
        ConfigNode node,
        Type targetType,
        object? currentValue,
        MergeMode mergeMode,
        out object? projected)
    {
        var nullableUnderlying = Nullable.GetUnderlyingType(targetType);
        if (nullableUnderlying is not null)
        {
            return try_project_value(node, nullableUnderlying, currentValue, mergeMode, out projected);
        }

        if (targetType == typeof(string))
        {
            projected = node.as_string();
            return projected is not null;
        }

        if (targetType == typeof(bool))
        {
            if (!bool.TryParse(node.as_string(), out var boolValue))
            {
                projected = null;
                return false;
            }

            projected = boolValue;
            return true;
        }

        if (targetType == typeof(int))
        {
            if (!int.TryParse(node.as_string(), out var intValue))
            {
                projected = null;
                return false;
            }

            projected = intValue;
            return true;
        }

        if (targetType.IsEnum)
        {
            var raw = node.as_string();
            if (raw is null)
            {
                projected = null;
                return false;
            }

            if (Enum.TryParse(targetType, raw, true, out var enumValue))
            {
                projected = enumValue;
                return true;
            }

            projected = null;
            return false;
        }

        if (typeof(IDictionary).IsAssignableFrom(targetType))
        {
            return try_project_dictionary(node, targetType, currentValue, mergeMode, out projected);
        }

        if (targetType != typeof(string) && typeof(IList).IsAssignableFrom(targetType))
        {
            return try_project_list(node, targetType, currentValue, mergeMode, out projected);
        }

        if (node is not ObjectConfigNode objectNode)
        {
            return try_project_scalar_object(node, targetType, out projected);
        }

        var useExisting = mergeMode == MergeMode.deep_merge && currentValue is not null;
        var nested = useExisting ? currentValue : Activator.CreateInstance(targetType);
        if (nested is null)
        {
            projected = null;
            return false;
        }

        project_into(nested, objectNode);
        projected = nested;
        return true;
    }

    private static bool try_project_scalar_object(ConfigNode node, Type targetType, out object? projected)
    {
        var target = Activator.CreateInstance(targetType);
        if (target is null)
        {
            projected = null;
            return false;
        }

        var scalarProperty = targetType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(property =>
                property is { CanRead: true, CanWrite: true }
                && get_binding_path(property) == ".");
        if (scalarProperty is null)
        {
            projected = null;
            return false;
        }

        var mergeMode = get_merge_mode(scalarProperty);
        if (!try_project_value(node, scalarProperty.PropertyType, scalarProperty.GetValue(target), mergeMode, out var value))
        {
            projected = null;
            return false;
        }

        scalarProperty.SetValue(target, value);
        projected = target;
        return true;
    }

    private static bool try_project_dictionary(
        ConfigNode node,
        Type targetType,
        object? currentValue,
        MergeMode mergeMode,
        out object? projected)
    {
        if (node is not ObjectConfigNode objectNode)
        {
            projected = null;
            return false;
        }

        var genericArguments = targetType.GetGenericArguments();
        if (genericArguments.Length != 2 || genericArguments[0] != typeof(string))
        {
            projected = null;
            return false;
        }

        var valueType = genericArguments[1];
        var dictionary = currentValue as IDictionary ?? Activator.CreateInstance(targetType) as IDictionary;
        if (dictionary is null)
        {
            projected = null;
            return false;
        }

        if (mergeMode == MergeMode.replace)
        {
            dictionary.Clear();
        }

        foreach (var (key, child) in objectNode.enumerate_fields())
        {
            if (!try_project_value(child, valueType, dictionary[key], MergeMode.replace, out var value))
            {
                continue;
            }

            dictionary[key] = value;
        }

        projected = dictionary;
        return true;
    }

    private static bool try_project_list(
        ConfigNode node,
        Type targetType,
        object? currentValue,
        MergeMode mergeMode,
        out object? projected)
    {
        if (node is not ArrayConfigNode arrayNode)
        {
            projected = null;
            return false;
        }

        var elementType = targetType.GetGenericArguments().FirstOrDefault() ?? typeof(object);
        var list = currentValue as IList ?? Activator.CreateInstance(targetType) as IList;
        if (list is null)
        {
            projected = null;
            return false;
        }

        if (mergeMode == MergeMode.replace)
        {
            list.Clear();
        }

        var values = new List<object?>();
        foreach (var element in arrayNode.enumerate_array())
        {
            if (try_project_value(element, elementType, null, MergeMode.replace, out var item))
            {
                values.Add(item);
            }
        }

        if (mergeMode == MergeMode.union)
        {
            foreach (var value in values.Where(value => !contains(list, value)))
            {
                list.Add(value);
            }
        }
        else
        {
            foreach (var value in values)
            {
                list.Add(value);
            }
        }

        projected = list;
        return true;
    }

    private static bool contains(IList list, object? value)
    {
        foreach (var item in list)
        {
            if (Equals(item, value))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<string> get_binding_paths(PropertyInfo property)
    {
        var paths = new List<string>();

        if (property.GetCustomAttribute<ConfigPathAttribute>()?.path is { } configPath)
        {
            paths.Add(configPath);
        }
        else
        {
            foreach (var name in DataMemberReflection.get_binding_names(property, property.Name))
            {
                if (!paths.Contains(name, StringComparer.Ordinal))
                {
                    paths.Add(name);
                }
            }

            if (property.GetCustomAttribute<ConfigProjectAttribute>()?.path is { } legacyPath
                && !paths.Contains(legacyPath, StringComparer.Ordinal))
            {
                paths.Add(legacyPath);
            }
        }

        if (property.GetCustomAttribute<ConfigProjectAttribute>()?.path is { } compatPath
            && !paths.Contains(compatPath, StringComparer.Ordinal))
        {
            paths.Add(compatPath);
        }

        return paths;
    }

    private static string get_binding_path(PropertyInfo property)
    {
        return get_binding_paths(property).FirstOrDefault() ?? property.Name;
    }

    private static MergeMode get_merge_mode(PropertyInfo property)
    {
        if (property.GetCustomAttribute<MergeAttribute>() is { } merge)
        {
            return merge.mode;
        }

        if (property.GetCustomAttribute<ConfigMergeAttribute>() is { } legacyMerge)
        {
            return legacyMerge.mode switch
            {
                ConfigMergeMode.replace => MergeMode.replace,
                ConfigMergeMode.deep_merge => MergeMode.deep_merge,
                ConfigMergeMode.append => MergeMode.append,
                ConfigMergeMode.union => MergeMode.union,
                _ => get_default_merge_mode(property.PropertyType)
            };
        }

        return get_default_merge_mode(property.PropertyType);
    }

    private static MergeMode get_default_merge_mode(Type targetType)
    {
        if (targetType != typeof(string) && typeof(IList).IsAssignableFrom(targetType))
        {
            return MergeMode.replace;
        }

        if (typeof(IDictionary).IsAssignableFrom(targetType))
        {
            return MergeMode.deep_merge;
        }

        if (!targetType.IsValueType && targetType != typeof(string))
        {
            return MergeMode.deep_merge;
        }

        return MergeMode.replace;
    }
}
