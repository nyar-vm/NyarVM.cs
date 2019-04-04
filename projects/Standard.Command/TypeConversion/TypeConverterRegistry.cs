namespace Std.Command.TypeConversion;

/// <summary>
///     类型转换器注册表，管理所有类型转换器
/// </summary>
public sealed class TypeConverterRegistry
{
    private readonly Dictionary<Type, object> _converters = new();

    /// <summary>
    ///     创建类型转换器注册表并自动注册内置转换器
    /// </summary>
    public TypeConverterRegistry()
    {
        register_default_converters();
    }

    /// <summary>
    ///     全局默认实例，自动注册内置转换器
    /// </summary>
    public static TypeConverterRegistry @default { get; } = new();

    /// <summary>
    ///     注册类型转换器
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="converter">转换器实例</param>
    public void register<T>(ITypeConverter<T> converter)
    {
        _converters[typeof(T)] = converter;
    }

    /// <summary>
    ///     执行类型转换
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="input">输入字符串</param>
    /// <param name="result">转换结果</param>
    /// <returns>转换是否成功</returns>
    public bool try_convert<T>(string input, out T result)
    {
        if (_converters.TryGetValue(typeof(T), out var converterObj) && converterObj is ITypeConverter<T> converter)
            return converter.try_convert(input, out result);

        result = default!;
        return false;
    }

    /// <summary>
    ///     执行类型转换（非泛型版本），转换失败时抛出异常
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="targetType">目标类型</param>
    /// <returns>转换结果</returns>
    /// <exception cref="FormatException">转换失败时抛出</exception>
    public object? convert(string input, Type targetType)
    {
        if (_converters.TryGetValue(targetType, out var converterObj))
        {
            var converterType = typeof(ITypeConverter<>).MakeGenericType(targetType);
            var tryConvertMethod = converterType.GetMethod(nameof(ITypeConverter<int>.try_convert))!;
            var parameters = new object[] { input, null! };
            var success = (bool)tryConvertMethod.Invoke(converterObj, parameters)!;
            if (success) return parameters[1];
        }

        throw new FormatException($"无法将 \"{input}\" 转换为类型 {targetType.Name}");
    }

    /// <summary>
    ///     检查是否已注册指定类型的转换器
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>是否已注册</returns>
    public bool has_converter<T>()
    {
        return _converters.ContainsKey(typeof(T));
    }

    private void register_default_converters()
    {
        register(new Int32Converter());
        register(new BooleanConverter());
        register(new Float64Converter());
        register(new Float32Converter());
        register(new StringConverter());
        register(new DateTimeConverter());
        register(new Int64Converter());
    }
}