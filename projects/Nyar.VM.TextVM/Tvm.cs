namespace Nyar.VM.TextVM;

/// <summary>
/// TextVM 运行时入口。提供极简的静态方法 ISM/查找/替换。
/// </summary>
public static class Tvm
{
    /// <summary>
    /// 判断输入中是否存在匹配。
    /// </summary>
    /// <param name="engineBytes">.tvm 格式的引擎字节数据。</param>
    /// <param name="input">输入字节切片。</param>
    /// <returns>如果存在匹配则返回 true，否则返回 false。</returns>
    public static Boolean IsMatch(ReadOnlySpan<Byte> engineBytes, ReadOnlySpan<Byte> input)
    {
        CompiledUnit unit = EngineLoader.Load(engineBytes);
        return unit.IsMatch(input);
    }

    /// <summary>
    /// 查找第一个匹配区间。
    /// </summary>
    /// <param name="engineBytes">.tvm 格式的引擎字节数据。</param>
    /// <param name="input">输入字节切片。</param>
    /// <returns>第一个匹配区间，未找到时返回 null。</returns>
    public static Match? Find(ReadOnlySpan<Byte> engineBytes, ReadOnlySpan<Byte> input)
    {
        CompiledUnit unit = EngineLoader.Load(engineBytes);
        return unit.FindFirst(input);
    }

    /// <summary>
    /// 查找所有匹配区间。
    /// </summary>
    /// <param name="engineBytes">.tvm 格式的引擎字节数据。</param>
    /// <param name="input">输入字节切片。</param>
    /// <returns>所有匹配区间的序列。</returns>
    public static IEnumerable<Match> FindAll(ReadOnlySpan<Byte> engineBytes, ReadOnlySpan<Byte> input)
    {
        CompiledUnit unit = EngineLoader.Load(engineBytes);
        return unit.FindAll(input);
    }

    /// <summary>
    /// 执行替换，返回替换后的新字节序列。
    /// </summary>
    /// <param name="engineBytes">.tvm 格式的引擎字节数据。</param>
    /// <param name="input">输入字节切片。</param>
    /// <param name="replacement">替换字节序列。</param>
    /// <returns>替换后生成的新字节序列。</returns>
    public static Byte[] Replace(
        ReadOnlySpan<Byte> engineBytes, ReadOnlySpan<Byte> input, ReadOnlySpan<Byte> replacement)
    {
        CompiledUnit unit = EngineLoader.Load(engineBytes);
        return unit.Replace(input, replacement);
    }
}
