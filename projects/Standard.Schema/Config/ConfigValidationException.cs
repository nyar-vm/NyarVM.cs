namespace Hermes.Config;

/// <summary>
///     配置验证异常 — Fail-fast 时抛出
/// </summary>
public sealed class ConfigValidationException : Exception
{
    public ConfigValidationException(IReadOnlyList<ConfigValidationError> errors)
        : base(BuildMessage(errors))
    {
        Errors = errors;
    }

    /// <summary>
    ///     验证错误列表
    /// </summary>
    public IReadOnlyList<ConfigValidationError> Errors { get; }

    private static string BuildMessage(IReadOnlyList<ConfigValidationError> errors)
    {
        var message = $"配置验证失败，共 {errors.Count} 个错误：";
        foreach (var error in errors) message += $"\n  [{error.ClassName}] {error.FieldName}: {error.Message}";
        return message;
    }
}

/// <summary>
///     单个配置验证错误
/// </summary>
public sealed class ConfigValidationError
{
    public ConfigValidationError(string className, string fieldName, string message, string? actualValue = null)
    {
        ClassName = className;
        FieldName = fieldName;
        Message = message;
        ActualValue = actualValue;
    }

    /// <summary>
    ///     类名
    /// </summary>
    public string ClassName { get; }

    /// <summary>
    ///     字段名
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    ///     错误消息
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     实际值（如果可用），用于调试
    /// </summary>
    public string? ActualValue { get; }
}