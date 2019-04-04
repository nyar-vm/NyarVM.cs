namespace Core.Data.Contract;

/// <summary>
///     验证错误，描述字段级别的验证失败信息。
/// </summary>
public sealed class ValidationError
{
    /// <summary>
    ///     初始化验证错误。
    /// </summary>
    /// <param name="fieldName">字段名称。</param>
    /// <param name="message">错误描述信息。</param>
    /// <param name="code">错误代码。</param>
    public ValidationError(string fieldName, string message, string? code = null)
    {
        field_name = fieldName;
        this.message = message;
        this.code = code;
    }

    /// <summary>
    ///     发生错误的字段名称。
    /// </summary>
    public string field_name { get; }

    /// <summary>
    ///     错误描述信息。
    /// </summary>
    public string message { get; }

    /// <summary>
    ///     错误代码，用于程序化识别错误类型。
    /// </summary>
    public string? code { get; }
}