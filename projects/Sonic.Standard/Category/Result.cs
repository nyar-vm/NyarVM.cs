namespace Std.Category;

/// <summary>
///     表示一个可能成功或失败的结果，类似于 Rust 的 <c>Result&lt;T, E&gt;</c>。
///     使用 <see cref="Ok" /> 构造成功的实例，使用 <see cref="error" /> 构造失败的实例。
/// </summary>
/// <typeparam name="T">成功时包含的值类型，必须为非空类型。</typeparam>
/// <typeparam name="E">失败时包含的错误类型，必须为非空类型。</typeparam>
public readonly struct Result<T, E> where T : notnull where E : notnull
{
    private readonly T _value;
    private readonly E _error;

    private Result(T value, E error, bool isOk)
    {
        _value = value;
        _error = error;
        is_ok = isOk;
    }

    /// <summary>
    ///     获取该实例是否为成功状态。
    /// </summary>
    public bool is_ok { get; }

    /// <summary>
    ///     获取该实例是否为失败状态。
    /// </summary>
    public bool is_error => !is_ok;

    /// <summary>
    ///     创建一个成功的 <see cref="Result{T, E}" /> 实例。
    /// </summary>
    /// <param name="value">成功时包含的值。</param>
    /// <returns>包含指定值的成功实例。</returns>
    public static Result<T, E> ok(T value)
    {
        return new Result<T, E>(value, default!, true);
    }

    /// <summary>
    ///     创建一个失败的 <see cref="Result{T, E}" /> 实例。
    /// </summary>
    /// <param name="error">失败时包含的错误。</param>
    /// <returns>包含指定错误的失败实例。</returns>
    public static Result<T, E> error(E error)
    {
        return new Result<T, E>(default!, error, false);
    }

    /// <summary>
    ///     返回成功时包含的值。如果实例为失败状态，则抛出异常。
    /// </summary>
    /// <returns>成功时包含的值。</returns>
    /// <exception cref="InvalidOperationException">当实例为失败状态时抛出。</exception>
    public T unwrap()
    {
        return is_ok ? _value : throw new InvalidOperationException($"Result 为错误状态: {_error}");
    }

    /// <summary>
    ///     返回失败时包含的错误。如果实例为成功状态，则抛出异常。
    /// </summary>
    /// <returns>失败时包含的错误。</returns>
    /// <exception cref="InvalidOperationException">当实例为成功状态时抛出。</exception>
    public E unwrap_error()
    {
        return !is_ok ? _error : throw new InvalidOperationException("Result 为成功状态，无法获取错误");
    }

    /// <summary>
    ///     如果实例为成功状态，则对其值应用映射函数；否则传递原始错误。
    /// </summary>
    /// <typeparam name="U">映射结果的类型。</typeparam>
    /// <param name="mapper">映射函数。</param>
    /// <returns>映射后的 <see cref="Result{U, E}" />。</returns>
    public Result<U, E> map<U>(Func<T, U> mapper) where U : notnull
    {
        return is_ok ? Result<U, E>.ok(mapper(_value)) : Result<U, E>.error(_error);
    }

    /// <summary>
    ///     如果实例为失败状态，则对其错误应用映射函数；否则传递原始值。
    /// </summary>
    /// <typeparam name="E2">错误映射结果的类型。</typeparam>
    /// <param name="mapper">错误映射函数。</param>
    /// <returns>映射后的 <see cref="Result{T, E2}" />。</returns>
    public Result<T, E2> map_error<E2>(Func<E, E2> mapper) where E2 : notnull
    {
        return is_ok ? Result<T, E2>.ok(_value) : Result<T, E2>.error(mapper(_error));
    }

    /// <summary>
    ///     如果实例为成功状态，则对其值应用绑定函数；否则传递原始错误。
    ///     也称为 <c>FlatMap</c>。
    /// </summary>
    /// <typeparam name="U">绑定结果的类型。</typeparam>
    /// <param name="binder">绑定函数，返回一个新的 <see cref="Result{U, E}" />。</param>
    /// <returns>绑定后的 <see cref="Result{U, E}" />。</returns>
    public Result<U, E> and_then<U>(Func<T, Result<U, E>> binder) where U : notnull
    {
        return is_ok ? binder(_value) : Result<U, E>.error(_error);
    }

    /// <summary>
    ///     返回成功时包含的值，如果为失败状态则返回指定的默认值。
    /// </summary>
    /// <param name="defaultValue">默认值。</param>
    /// <returns>成功时的值或默认值。</returns>
    public T unwrap_or(T defaultValue)
    {
        return is_ok ? _value : defaultValue;
    }

    /// <summary>
    ///     返回成功时包含的值，如果为失败状态则返回类型的默认值。
    /// </summary>
    /// <returns>成功时的值或类型默认值。</returns>
    public T unwrap_or_default()
    {
        return is_ok ? _value : default!;
    }

    /// <summary>
    ///     返回成功时包含的值，如果为失败状态则调用工厂函数获取默认值。
    /// </summary>
    /// <param name="factory">默认值工厂函数，接收错误作为参数。</param>
    /// <returns>成功时的值或工厂函数的返回值。</returns>
    public T unwrap_or_else(Func<E, T> factory)
    {
        return is_ok ? _value : factory(_error);
    }

    /// <summary>
    ///     对 <see cref="Result{T, E}" /> 进行折叠，根据成功或失败状态调用不同的函数。
    /// </summary>
    /// <typeparam name="R">折叠结果的类型。</typeparam>
    /// <param name="onOk">当实例为成功状态时调用的函数。</param>
    /// <param name="onError">当实例为失败状态时调用的函数。</param>
    /// <returns>折叠函数的返回值。</returns>
    public R fold<R>(Func<T, R> onOk, Func<E, R> onError)
    {
        return is_ok ? onOk(_value) : onError(_error);
    }

    /// <summary>
    ///     如果实例为成功状态则返回自身；否则调用工厂函数获取备选 <see cref="Result{T, E}" />。
    /// </summary>
    /// <param name="alternative">备选值工厂函数。</param>
    /// <returns>自身或工厂函数的返回值。</returns>
    public Result<T, E> or_else(Func<Result<T, E>> alternative)
    {
        return is_ok ? this : alternative();
    }

    /// <summary>
    ///     对成功时包含的值执行副作用操作，然后返回自身。如果为失败状态则不执行任何操作。
    /// </summary>
    /// <param name="action">要执行的副作用操作。</param>
    /// <returns>自身。</returns>
    public Result<T, E> inspect(Action<T> action)
    {
        if (is_ok) action(_value);

        return this;
    }

    /// <summary>
    ///     对失败时包含的错误执行副作用操作，然后返回自身。如果为成功状态则不执行任何操作。
    /// </summary>
    /// <param name="action">要执行的副作用操作。</param>
    /// <returns>自身。</returns>
    public Result<T, E> inspect_error(Action<E> action)
    {
        if (!is_ok) action(_error);

        return this;
    }

    /// <summary>
    ///     将成功状态转换为 <see cref="Option{T}" /> 的 <see cref="Option{T}.some" />，
    ///     失败状态转换为 <see cref="Option{T}.none" />。
    /// </summary>
    /// <returns>包含成功值的 <see cref="Option{T}" />。</returns>
    public Option<T> ok()
    {
        return is_ok ? Option<T>.some(_value) : Option<T>.none;
    }

    /// <summary>
    ///     将失败状态转换为 <see cref="Option{E}" /> 的 <see cref="Option{T}.some" />，
    ///     成功状态转换为 <see cref="Option{T}.none" />。
    /// </summary>
    /// <returns>包含错误的 <see cref="Option{E}" />。</returns>
    public Option<E> err()
    {
        return !is_ok ? Option<E>.some(_error) : Option<E>.none;
    }

    /// <summary>
    ///     判断两个 <see cref="Result{T, E}" /> 实例是否相等。
    /// </summary>
    /// <param name="other">要比较的实例。</param>
    /// <returns>如果两个实例相等返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public bool equals(Result<T, E> other)
    {
        if (is_ok != other.is_ok) return false;

        return is_ok
            ? EqualityComparer<T>.Default.Equals(_value, other._value)
            : EqualityComparer<E>.Default.Equals(_error, other._error);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Result<T, E> other && equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return is_ok
            ? HashCode.Combine(_value, is_ok)
            : HashCode.Combine(_error, is_ok);
    }

    /// <summary>
    ///     判断两个 <see cref="Result{T, E}" /> 实例是否相等。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>如果两个实例相等返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public static bool operator ==(Result<T, E> left, Result<T, E> right)
    {
        return left.equals(right);
    }

    /// <summary>
    ///     判断两个 <see cref="Result{T, E}" /> 实例是否不相等。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>如果两个实例不相等返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public static bool operator !=(Result<T, E> left, Result<T, E> right)
    {
        return !left.equals(right);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return is_ok ? $"Ok({_value})" : $"Error({_error})";
    }
}