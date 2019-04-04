using System.Collections.Immutable;

namespace Sonic.Data.Generator.Config;

/// <summary>
///     标记了 <c>[Config]</c> 的类型中单个属性的编译期提取信息。
/// </summary>
internal readonly record struct ConfigPropertyInfo
{
    /// <summary>
    ///     配置键名（已按命名约定转换，或被 [Property] 覆盖）。
    /// </summary>
    public readonly string config_key;

    /// <summary>
    ///     编译期提取的默认值表达式，无初始化器时为 null。
    /// </summary>
    public readonly string? default_value_expr;

    /// <summary>
    ///     当属性类型为 <c>Dictionary&lt;string,T&gt;</c> 时，T 的完全限定名；否则为 null。
    /// </summary>
    public readonly string? dict_value_type;

    /// <summary>
    ///     属性类型的完全限定名，如 <c>global::System.Collections.Generic.List&lt;global::System.String&gt;</c>。
    /// </summary>
    public readonly string fully_qualified_type;

    /// <summary>
    ///     属性类型本身是否标记了 [Config] 特性（用于递归生成）。
    /// </summary>
    public readonly bool is_config_type;

    /// <summary>
    ///     属性类型是否为可空类型。
    /// </summary>
    public readonly bool is_nullable;

    /// <summary>
    ///     是否标记了 [Required] 特性。
    /// </summary>
    public readonly bool is_required;

    /// <summary>
    ///     是否标记了 [Sensitive] 特性。
    /// </summary>
    public readonly bool is_sensitive;

    /// <summary>
    ///     属性类型是否为值类型。
    /// </summary>
    public readonly bool is_value_type;

    /// <summary>
    ///     当属性类型为 <c>List&lt;T&gt;</c> 时，T 的完全限定名；否则为 null。
    /// </summary>
    public readonly string? list_element_type;

    /// <summary>
    ///     合并策略。
    /// </summary>
    public readonly CollectionMergeStrategy merge_strategy;

    /// <summary>
    ///     属性名称。
    /// </summary>
    public readonly string name;

    /// <summary>
    ///     该属性上的验证特性列表。
    /// </summary>
    public readonly ImmutableArray<ValidationInfo> validations;

    /// <summary>
    ///     使用所有字段初始化 <see cref="ConfigPropertyInfo" /> 的新实例。
    /// </summary>
    public ConfigPropertyInfo(
        string name,
        string fully_qualified_type,
        bool is_value_type,
        bool is_nullable,
        string config_key,
        CollectionMergeStrategy merge_strategy,
        bool is_sensitive,
        bool is_required,
        string? default_value_expr,
        ImmutableArray<ValidationInfo> validations,
        bool is_config_type,
        string? list_element_type,
        string? dict_value_type)
    {
        this.name = name;
        this.fully_qualified_type = fully_qualified_type;
        this.is_value_type = is_value_type;
        this.is_nullable = is_nullable;
        this.config_key = config_key;
        this.merge_strategy = merge_strategy;
        this.is_sensitive = is_sensitive;
        this.is_required = is_required;
        this.default_value_expr = default_value_expr;
        this.validations = validations;
        this.is_config_type = is_config_type;
        this.list_element_type = list_element_type;
        this.dict_value_type = dict_value_type;
    }
}