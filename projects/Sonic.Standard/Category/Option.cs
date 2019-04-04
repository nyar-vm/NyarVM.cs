namespace Std.Category;

/// <summary>
///     表示一个可能存在或不存在的值，类似于 Rust 的 <c>Option&lt;T&gt;</c>。
///     使用 <see cref="some" /> 构造有值的实例，使用 <see cref="none" /> 获取空实例。
/// </summary>
/// <typeparam name="T">包装值的类型，必须为非空类型。</typeparam>
public readonly struct Option<T> where T : notnull
{
    private readonly T _value;

    private Option(T value)
    {
        _value = value;
        is_some = true;
    }

    /// <summary>
    ///     获取该实例是否包含值。
    /// </summary>
    public bool is_some { get; }

    /// <summary>
    ///     获取该实例是否为空。
    /// </summary>
    public bool is_none => !is_some;

    /// <summary>
    ///     获取包含的值。如果实例为 <see cref="is_none" />，则抛出异常。
    /// </summary>
    /// <exception cref="InvalidOperationException">当实例为空时抛出。</exception>
    public T value => is_some ? _value : throw new InvalidOperationException("Option 为 None，无法获取值");

    /// <summary>
    ///     创建一个包含指定值的 <see cref="Option{T}" /> 实例。
    /// </summary>
    /// <param name="value">要包装的值。</param>
    /// <returns>包含指定值的 <see cref="Option{T}" /> 实例。</returns>
    public static Option<T> some(T value)
    {
        return new Option<T>(value);
    }

    /// <summary>
    ///     获取一个空的 <see cref="Option{T}" /> 实例。
    /// </summary>
    public static Option<T> none => default;

    /// <summary>
    ///     对 <see cref="Option{T}" /> 进行模式匹配，根据是否有值调用不同的函数。
    /// </summary>
    /// <typeparam name="TResult">返回结果的类型。</typeparam>
    /// <param name="some">当实例包含值时调用的函数。</param>
    /// <param name="none">当实例为空时调用的函数。</param>
    /// <returns>匹配函数的返回值。</returns>
    public TResult match<TResult>(Func<T, TResult> some, Func<TResult> none)
    {
        return is_some ? some(_value) : none();
    }

    /// <summary>
    ///     如果实例包含值，则对其应用映射函数；否则返回 <see cref="none" />。
    /// </summary>
    /// <typeparam name="TResult">映射结果的类型。</typeparam>
    /// <param name="mapper">映射函数。</param>
    /// <returns>映射后的 <see cref="Option{TResult}" />。</returns>
    public Option<TResult> map<TResult>(Func<T, TResult> mapper) where TResult : notnull
    {
        return is_some ? Option<TResult>.some(mapper(_value)) : Option<TResult>.none;
    }

    /// <summary>
    ///     如果实例包含值，则对其应用绑定函数；否则返回 <see cref="none" />。
    ///     也称为 <c>FlatMap</c>。
    /// </summary>
    /// <typeparam name="TResult">绑定结果的类型。</typeparam>
    /// <param name="binder">绑定函数，返回一个新的 <see cref="Option{TResult}" />。</param>
    /// <returns>绑定后的 <see cref="Option{TResult}" />。</returns>
    public Option<TResult> bind<TResult>(Func<T, Option<TResult>> binder) where TResult : notnull
    {
        return is_some ? binder(_value) : Option<TResult>.none;
    }

    /// <summary>
    ///     返回包含的值，如果为空则返回指定的默认值。
    /// </summary>
    /// <param name="defaultValue">默认值。</param>
    /// <returns>包含的值或默认值。</returns>
    public T unwrap_or(T defaultValue)
    {
        return is_some ? _value : defaultValue;
    }

    /// <summary>
    ///     返回包含的值，如果为空则返回类型的默认值。
    /// </summary>
    /// <returns>包含的值或类型默认值。</returns>
    public T unwrap_or_default()
    {
        return is_some ? _value : default!;
    }

    /// <summary>
    ///     返回包含的值，如果为空则调用工厂函数获取默认值。
    /// </summary>
    /// <param name="factory">默认值工厂函数。</param>
    /// <returns>包含的值或工厂函数的返回值。</returns>
    public T unwrap_or_else(Func<T> factory)
    {
        return is_some ? _value : factory();
    }

    /// <summary>
    ///     如果实例包含值且满足谓词，则返回自身；否则返回 <see cref="none" />。
    /// </summary>
    /// <param name="predicate">过滤谓词。</param>
    /// <returns>过滤后的 <see cref="Option{T}" />。</returns>
    public Option<T> filter(Func<T, bool> predicate)
    {
        return is_some && predicate(_value) ? this : none;
    }

    /// <summary>
    ///     如果实例包含值则返回自身；否则返回备选 <see cref="Option{T}" />。
    /// </summary>
    /// <param name="alternative">备选的 <see cref="Option{T}" />。</param>
    /// <returns>自身或备选值。</returns>
    public Option<T> or(Option<T> alternative)
    {
        return is_some ? this : alternative;
    }

    /// <summary>
    ///     如果实例包含值则返回自身；否则调用工厂函数获取备选 <see cref="Option{T}" />。
    /// </summary>
    /// <param name="alternative">备选值工厂函数。</param>
    /// <returns>自身或工厂函数的返回值。</returns>
    public Option<T> or_else(Func<Option<T>> alternative)
    {
        return is_some ? this : alternative();
    }

    /// <summary>
    ///     将两个 <see cref="Option{T}" /> 合并为一个，使用组合函数合并两者的值。
    ///     如果任一实例为空，则返回 <see cref="none" />。
    /// </summary>
    /// <typeparam name="U">另一个 <see cref="Option{T}" /> 的值类型。</typeparam>
    /// <typeparam name="R">组合结果的类型。</typeparam>
    /// <param name="other">另一个 <see cref="Option{T}" />。</param>
    /// <param name="combiner">组合函数。</param>
    /// <returns>组合后的 <see cref="Option{R}" />。</returns>
    public Option<R> zip<U, R>(Option<U> other, Func<T, U, R> combiner) where U : notnull where R : notnull
    {
        return is_some && other.is_some
            ? Option<R>.some(combiner(_value, other._value))
            : Option<R>.none;
    }

    /// <summary>
    ///     展平嵌套的 <see cref="Option{Option{T}}" /> 为 <see cref="Option{T}" />。
    /// </summary>
    /// <param name="nested">嵌套的 <see cref="Option{T}" />。</param>
    /// <returns>展平后的 <see cref="Option{T}" />。</returns>
    public static Option<T> flatten(Option<Option<T>> nested)
    {
        return nested.is_some ? nested._value : none;
    }

    /// <summary>
    ///     对包含的值执行副作用操作，然后返回自身。如果为空则不执行任何操作。
    /// </summary>
    /// <param name="action">要执行的副作用操作。</param>
    /// <returns>自身。</returns>
    public Option<T> inspect(Action<T> action)
    {
        if (is_some) action(_value);

        return this;
    }

    /// <summary>
    ///     判断两个 <see cref="Option{T}" /> 实例是否相等。
    /// </summary>
    /// <param name="other">要比较的实例。</param>
    /// <returns>如果两个实例相等返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public bool equals(Option<T> other)
    {
        if (is_some != other.is_some) return false;

        return !is_some || EqualityComparer<T>.Default.Equals(_value, other._value);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Option<T> other && equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return is_some ? HashCode.Combine(_value, is_some) : HashCode.Combine(is_some);
    }

    /// <summary>
    ///     判断两个 <see cref="Option{T}" /> 实例是否相等。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>如果两个实例相等返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public static bool operator ==(Option<T> left, Option<T> right)
    {
        return left.equals(right);
    }

    /// <summary>
    ///     判断两个 <see cref="Option{T}" /> 实例是否不相等。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>如果两个实例不相等返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public static bool operator !=(Option<T> left, Option<T> right)
    {
        return !left.equals(right);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return is_some ? $"Some({_value})" : "None";
    }
}