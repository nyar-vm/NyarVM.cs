namespace Core.Data.Contract;

/// <summary>
///     验证器接口，定义对类型实例进行验证的契约。
/// </summary>
/// <typeparam name="T">待验证的类型。</typeparam>
public interface IValidator<T>
{
    /// <summary>
    ///     验证指定实例并返回验证结果。
    /// </summary>
    /// <param name="instance">待验证的实例。</param>
    /// <returns>包含错误和警告的验证结果。</returns>
    ValidationResult validate(T instance);
}