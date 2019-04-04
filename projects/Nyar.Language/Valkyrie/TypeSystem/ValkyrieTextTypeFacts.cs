using Nyar.Analyzer.Semantic;

namespace Nyar.Language.Valkyrie.TypeSystem;

/// <summary>
///     Valkyrie 文本类型的统一权威入口。
/// </summary>
public static class ValkyrieTextTypeFacts
{
    public const string literal_text_name = "literal_text";
    public const string literal_char_name = "literal_char";
    public const string literal_text_kind_tag = "literal_text";
    public const string literal_char_kind_tag = "literal_char";
    public const string char_name = "char";
    public const string utf8_name = "utf8";
    public const string utf16_name = "utf16";
    public const string utf32_name = "utf32";
    public const string c_str_name = "c_str";
    public const string owned_text_kind_tag = "owned_text";

    public static bool is_legacy_text_ref_name(string? typeName)
    {
        return string.Equals(typeName, "&text", StringComparison.Ordinal)
               || string.Equals(typeName, "text_ref", StringComparison.Ordinal);
    }

    /// <summary>
    ///     阻止历史遗留的 `text_ref` / `&text` 继续进入正式类型主链。
    /// </summary>
    public static void ensure_no_legacy_text_ref_type(string? typeName, string stageName)
    {
        if (!is_legacy_text_ref_name(typeName))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Valkyrie 在 {stageName} 阶段禁止继续使用历史遗留文本类型 `{typeName}`，请显式改用 `utf8`、`utf16`、`utf32` 或 `c_str`。");
    }

    public static bool is_literal_text_name(string? typeName)
    {
        return string.Equals(typeName, literal_text_name, StringComparison.Ordinal);
    }

    public static bool is_literal_char_name(string? typeName)
    {
        return string.Equals(typeName, literal_char_name, StringComparison.Ordinal);
    }

    public static bool is_char_name(string? typeName)
    {
        return string.Equals(typeName, char_name, StringComparison.Ordinal);
    }

    public static bool is_pre_hir_literal_name(string? typeName)
    {
        return is_literal_text_name(typeName) || is_literal_char_name(typeName);
    }

    public static void ensure_no_pre_hir_literal_type(string? typeName, string stageName)
    {
        if (!is_pre_hir_literal_name(typeName))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Valkyrie 在 {stageName} 阶段禁止继续使用字面量占位类型 `{typeName}`，请先收敛为 `char`、`utf8`、`utf16` 等实际类型。");
    }

    public static bool is_owned_text_name(string? typeName)
    {
        return typeName is utf8_name or utf16_name or utf32_name or c_str_name;
    }

    /// <summary>
    ///     判断类型名是否为 owned_text 对应的 class/struct 类型名。
    ///     例如 <c>Utf8Text</c>、<c>Utf16Text</c>、<c>Utf32Text</c>、<c>AsciiText</c>。
    /// </summary>
    public static bool is_owned_text_class_name(string? typeName)
    {
        return typeName is "Utf8Text" or "Utf16Text" or "Utf32Text" or "AsciiText";
    }

    public static bool is_text_like_name(string? typeName)
    {
        var normalizedTypeName = normalize_semantic_type_name(typeName);
        return is_owned_text_name(normalizedTypeName) || is_literal_text_name(normalizedTypeName);
    }

    /// <summary>
    ///     判断类型名是否仍在使用历史遗留的宽泛文本别名。
    /// </summary>
    [Obsolete("请勿再调用 legacy `string` 检测接口；请在语义绑定阶段直接拒绝该旧类型名。", true)]
    public static bool is_legacy_text_alias(string? typeName)
    {
        throw new NotSupportedException(
            "请勿再调用 legacy `string` 检测接口；请在语义绑定阶段直接拒绝该旧类型名。");
    }

    /// <summary>
    ///     在指定编译阶段阻止遗留文本别名继续向后传播。
    /// </summary>
    [Obsolete("请勿再调用 legacy `string` 报错接口；请在调用点直接抛出错误。", true)]
    public static void ensure_no_legacy_text_alias(string? typeName, string stageName)
    {
        throw new NotSupportedException(
            "请勿再调用 legacy `string` 报错接口；请在调用点直接抛出错误。");
    }

    public static string normalize_semantic_type_name(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return string.Empty;
        }

        // 类型系统内部统一使用 `∷` 作为限定名分隔符，同时兼容源码中的 `::` 与 `.` 简写。
        return typeName
            .Replace("::", "∷", StringComparison.Ordinal)
            .Replace(".", "∷", StringComparison.Ordinal)
            .Trim();
    }

    public static string normalize_display_type_name(string? typeName)
    {
        return normalize_semantic_type_name(typeName);
    }

    /// <summary>
    ///     收敛预 `HIR` 阶段遗留的字面量占位类型，并返回后续阶段统一使用的规范类型名。
    /// </summary>
    public static string canonicalize_post_literal_type_name(string? typeName)
    {
        return finalize_pre_hir_literal_type_name(typeName);
    }

    public static bool semantically_equals(string? leftTypeName, string? rightTypeName)
    {
        return string.Equals(
            normalize_semantic_type_name(leftTypeName),
            normalize_semantic_type_name(rightTypeName),
            StringComparison.Ordinal);
    }

    public static IType? try_create_semantic_text_type(string? typeName)
    {
        if (is_legacy_text_alias_core(typeName) || is_legacy_text_ref_name(typeName))
        {
            return null;
        }

        typeName = normalize_semantic_type_name(typeName);
        if (is_literal_text_name(typeName)) return create_semantic_literal_text_type();
        if (is_literal_char_name(typeName)) return create_semantic_literal_char_type();

        if (string.Equals(typeName, char_name, StringComparison.Ordinal)) return create_semantic_char_type();

        if (is_owned_text_name(typeName)) return create_semantic_owned_text_type(typeName!);

        return null;
    }

    private static bool is_legacy_text_alias_core(string? typeName)
    {
        return string.Equals(typeName, "string", StringComparison.Ordinal)
               || string.Equals(typeName, "String", StringComparison.Ordinal);
    }

    public static NamedType create_semantic_literal_text_type()
    {
        return new NamedType(literal_text_name, literal_text_kind_tag);
    }

    public static NamedType create_semantic_literal_char_type()
    {
        return new NamedType(literal_char_name, literal_char_kind_tag);
    }

    public static PrimitiveType create_semantic_char_type()
    {
        return new PrimitiveType(char_name);
    }

    public static string finalize_pre_hir_literal_type_name(string? typeName)
    {
        return typeName switch
        {
            literal_char_name => char_name,
            literal_text_name => utf8_name,
            _ => normalize_semantic_type_name(typeName)
        };
    }

    public static NamedType create_semantic_owned_text_type(string typeName)
    {
        return new NamedType(typeName, owned_text_kind_tag);
    }

    /// <summary>
    ///     根据 owned_text 类型名推断对应的 class/struct 类型名。
    ///     例如 <c>utf16</c> → <c>Utf16Text</c>，<c>utf8</c> → <c>Utf8Text</c>。
    /// </summary>
    public static string? try_get_class_name_for_owned_text(string? typeName)
    {
        return typeName switch
        {
            utf8_name => "Utf8Text",
            utf16_name => "Utf16Text",
            utf32_name => "Utf32Text",
            "ascii" => "AsciiText",
            _ => null
        };
    }
}
