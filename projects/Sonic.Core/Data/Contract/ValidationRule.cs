namespace Core.Data.Contract;

/// <summary>
///     验证规则，描述单个字段的验证约束。
/// </summary>
public sealed class ValidationRule
{
    /// <summary>
    ///     初始化验证规则。
    /// </summary>
    /// <param name="ruleType">规则类型标识。</param>
    /// <param name="errorMessage">验证失败时的错误消息。</param>
    public ValidationRule(string ruleType, string? errorMessage = null)
    {
        rule_type = ruleType;
        error_message = errorMessage;
    }

    /// <summary>
    ///     规则类型标识，如 "required"、"string_length"、"range" 等。
    /// </summary>
    public string rule_type { get; }

    /// <summary>
    ///     验证失败时的错误消息。
    /// </summary>
    public string? error_message { get; }
}