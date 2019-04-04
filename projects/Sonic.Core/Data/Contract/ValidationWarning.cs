namespace Core.Data.Contract;

/// <summary>
///     验证警告，描述字段级别的验证警告信息。
/// </summary>
public sealed class ValidationWarning
{
    /// <summary>
    ///     初始化验证警告。
    /// </summary>
    /// <param name="fieldName">字段名称。</param>
    /// <param name="message">警告描述信息。</param>
    /// <param name="code">警告代码。</param>
    public ValidationWarning(string fieldName, string message, string? code = null)
    {
        field_name = fieldName;
        this.message = message;
        this.code = code;
    }

    /// <summary>
    ///     产生警告的字段名称。
    /// </summary>
    public string field_name { get; }

    /// <summary>
    ///     警告描述信息。
    /// </summary>
    public string message { get; }

    /// <summary>
    ///     警告代码，用于程序化识别警告类型。
    /// </summary>
    public string? code { get; }
}