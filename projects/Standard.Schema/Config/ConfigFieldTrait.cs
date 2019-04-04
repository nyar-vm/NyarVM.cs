namespace Hermes.Config;

/// <summary>
///     配置字段特征 — 从 FieldDefinition 提取的 config 元数据
/// </summary>
public sealed class ConfigFieldTrait
{
    public ConfigFieldTrait(
        string fieldName,
        SchemaType fieldType,
        bool isSecret,
        bool isSuperSecret,
        bool isRequired,
        bool isReloadable,
        string? envName,
        object? defaultValue,
        IReadOnlyList<ValidationRule> validations)
    {
        FieldName = fieldName;
        FieldType = fieldType;
        IsSecret = isSecret;
        IsSuperSecret = isSuperSecret;
        IsRequired = isRequired;
        IsReloadable = isReloadable;
        EnvName = envName;
        DefaultValue = defaultValue;
        Validations = validations;
    }

    /// <summary>
    ///     字段名（去掉 @/@@ 前缀后的原始名称）
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    ///     字段类型
    /// </summary>
    public SchemaType FieldType { get; }

    /// <summary>
    ///     是否为机密字段（@ 前缀）
    /// </summary>
    public bool IsSecret { get; }

    /// <summary>
    ///     是否为超级机密字段（@@ 前缀）
    /// </summary>
    public bool IsSuperSecret { get; }

    /// <summary>
    ///     是否必填（[required]）
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    ///     是否可热重载（[reloadable]）
    /// </summary>
    public bool IsReloadable { get; }

    /// <summary>
    ///     环境变量名（[env("NAME")]），仅原始类型字段允许
    /// </summary>
    public string? EnvName { get; }

    /// <summary>
    ///     schema 中声明的默认值
    /// </summary>
    public object? DefaultValue { get; }

    /// <summary>
    ///     验证规则列表
    /// </summary>
    public IReadOnlyList<ValidationRule> Validations { get; }

    /// <summary>
    ///     字段是否为原始类型
    /// </summary>
    public bool IsPrimitiveType =>
        FieldType is PrimitiveType;
}