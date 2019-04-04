namespace Nyar.Assembler;

/// <summary>
///     元编译常量池。
/// </summary>
public sealed class GenerateConstantPool
{
    private readonly List<double> _float64_s = [];
    private readonly List<long> _int64_s = [];
    private readonly List<string> _strings = [];

    /// <summary>
    ///     字符串常量集合
    /// </summary>
    public IReadOnlyList<string> strings => _strings;

    /// <summary>
    ///     整数常量集合
    /// </summary>
    public IReadOnlyList<long> int64_s => _int64_s;

    /// <summary>
    ///     浮点常量集合
    /// </summary>
    public IReadOnlyList<double> float64_s => _float64_s;

    /// <summary>
    ///     添加字符串常量
    /// </summary>
    /// <param name="value">字符串值。</param>
    /// <returns>常量索引。</returns>
    public int add_string(string value)
    {
        var index = _strings.IndexOf(value);
        if (index >= 0) return index;

        _strings.Add(value);
        return _strings.Count - 1;
    }

    /// <summary>
    ///     添加整数常量
    /// </summary>
    /// <param name="value">整数值。</param>
    /// <returns>常量索引。</returns>
    public int add_int64(long value)
    {
        var index = _int64_s.IndexOf(value);
        if (index >= 0) return index;

        _int64_s.Add(value);
        return _int64_s.Count - 1;
    }

    /// <summary>
    ///     添加浮点常量
    /// </summary>
    /// <param name="value">浮点值。</param>
    /// <returns>常量索引。</returns>
    public int add_float64(double value)
    {
        var index = _float64_s.IndexOf(value);
        if (index >= 0) return index;

        _float64_s.Add(value);
        return _float64_s.Count - 1;
    }
}