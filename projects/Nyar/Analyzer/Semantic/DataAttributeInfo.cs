namespace Nyar.Analyzer.Semantic;

/// <summary>
///     `[data]` 类型的统一元信息。
/// </summary>
public sealed record DataAttributeInfo(
    string name,
    string qualified_name,
    string declaration_kind,
    IReadOnlyList<DataFieldInfo> fields);

/// <summary>
///     `[data]` 字段的统一元信息。
/// </summary>
public sealed record DataFieldInfo(
    string name,
    IType field_type,
    string binding_name,
    IReadOnlyList<string> aliases,
    bool ignore,
    bool flatten,
    int? order,
    bool has_default_value,
    string? default_value_text,
    bool skip_when_null,
    bool skip_when_default);