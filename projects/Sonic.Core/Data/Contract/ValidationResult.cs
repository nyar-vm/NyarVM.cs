using System.Collections.Immutable;

namespace Core.Data.Contract;

/// <summary>
///     验证结果，包含验证过程中产生的错误和警告。
/// </summary>
public sealed class ValidationResult
{
    private readonly ImmutableArray<ValidationError>.Builder _errorsBuilder;
    private readonly ImmutableArray<ValidationWarning>.Builder _warningsBuilder;

    /// <summary>
    ///     初始化空的验证结果，用于增量构建。
    /// </summary>
    public ValidationResult()
    {
        _errorsBuilder = ImmutableArray.CreateBuilder<ValidationError>();
        _warningsBuilder = ImmutableArray.CreateBuilder<ValidationWarning>();
    }

    /// <summary>
    ///     使用已有的错误和警告列表初始化验证结果。
    /// </summary>
    /// <param name="errors">验证错误列表。</param>
    /// <param name="warnings">验证警告列表。</param>
    public ValidationResult(ImmutableArray<ValidationError> errors, ImmutableArray<ValidationWarning> warnings)
    {
        _errorsBuilder = ImmutableArray.CreateBuilder<ValidationError>(errors.Length);
        _errorsBuilder.AddRange(errors);
        _warningsBuilder = ImmutableArray.CreateBuilder<ValidationWarning>(warnings.Length);
        _warningsBuilder.AddRange(warnings);
    }

    /// <summary>
    ///     验证产生的错误列表。
    /// </summary>
    public ImmutableArray<ValidationError> errors => _errorsBuilder.ToImmutable();

    /// <summary>
    ///     验证产生的警告列表。
    /// </summary>
    public ImmutableArray<ValidationWarning> warnings => _warningsBuilder.ToImmutable();

    /// <summary>
    ///     添加一条验证错误。
    /// </summary>
    /// <param name="fieldName">发生错误的字段名称。</param>
    /// <param name="message">错误描述信息。</param>
    /// <param name="code">错误代码。</param>
    public void add_error(string fieldName, string message, string? code = null)
    {
        _errorsBuilder.Add(new ValidationError(fieldName, message, code));
    }

    /// <summary>
    ///     添加一条验证警告。
    /// </summary>
    /// <param name="fieldName">产生警告的字段名称。</param>
    /// <param name="message">警告描述信息。</param>
    /// <param name="code">警告代码。</param>
    public void add_warning(string fieldName, string message, string? code = null)
    {
        _warningsBuilder.Add(new ValidationWarning(fieldName, message, code));
    }
}