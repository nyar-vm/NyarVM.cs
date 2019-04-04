namespace Std.Config;

/// <summary>
///     配置验证异常，聚合所有验证错误信息。
/// </summary>
public sealed class ConfigValidationException : Exception
{
    /// <summary>
    ///     使用验证错误列表初始化 <see cref="ConfigValidationException" /> 的新实例。
    /// </summary>
    /// <param name="errors">验证错误列表。</param>
    public ConfigValidationException(IReadOnlyList<string> errors)
        : base($"配置验证失败: {string.Join(", ", errors)}")
    {
        this.errors = errors;
    }

    /// <summary>
    ///     获取验证错误列表。
    /// </summary>
    public IReadOnlyList<string> errors { get; }
}