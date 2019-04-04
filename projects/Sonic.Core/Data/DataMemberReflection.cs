using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Core.Data;

/// <summary>
///     统一解析共享数据成员上的键名、别名、忽略和展平元信息。
///     `config` 与 `serde` 都应复用这层规则，避免各自维护一份反射逻辑。
/// </summary>
public static class DataMemberReflection
{
    public static bool should_ignore(MemberInfo member)
    {
        return member.GetCustomAttribute<IgnoreAttribute>() is not null
               || member.GetCustomAttribute<IgnoreDataAttribute>() is not null;
    }

    public static bool should_flatten(MemberInfo member)
    {
        return member.GetCustomAttribute<FlattenAttribute>() is not null;
    }

    public static IReadOnlyList<string> get_binding_names(MemberInfo member, string fallbackName)
    {
        var names = new List<string>();

        if (member.GetCustomAttribute<KeyAttribute>()?.name is { } keyName)
        {
            names.Add(keyName);
        }
        else if (member.GetCustomAttribute<FieldAttribute>()?.name is { } fieldName)
        {
            names.Add(fieldName);
        }
        else
        {
            names.Add(fallbackName);
        }

        foreach (var alias in member.GetCustomAttributes<AliasAttribute>())
        {
            if (!string.IsNullOrWhiteSpace(alias.name) && !names.Contains(alias.name, StringComparer.Ordinal))
            {
                names.Add(alias.name);
            }
        }

        return names;
    }
}
