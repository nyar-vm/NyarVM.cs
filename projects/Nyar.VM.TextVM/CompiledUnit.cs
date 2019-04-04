namespace Nyar.VM.TextVM;

/// <summary>
/// 编译后的执行单元基类。所有执行策略（字面量/SIMD/DFA/位并行/回溯）都继承此类。
/// </summary>
public abstract class CompiledUnit
{
    /// <summary>
    /// 判断输入中是否存在匹配。
    /// </summary>
    /// <param name="input">输入字节切片。</param>
    /// <returns>如果存在匹配则返回 true，否则返回 false。</returns>
    public abstract Boolean IsMatch(ReadOnlySpan<Byte> input);

    /// <summary>
    /// 查找第一个匹配区间。
    /// </summary>
    /// <param name="input">输入字节切片。</param>
    /// <returns>第一个匹配区间，未找到时返回 null。</returns>
    public abstract Match? FindFirst(ReadOnlySpan<Byte> input);

    /// <summary>
    /// 查找所有匹配区间。
    /// </summary>
    /// <param name="input">输入字节切片。</param>
    /// <returns>所有匹配区间的序列。</returns>
    public abstract IEnumerable<Match> FindAll(ReadOnlySpan<Byte> input);

    /// <summary>
    /// 执行替换，返回替换后的字节序列。
    /// </summary>
    /// <param name="input">输入字节切片。</param>
    /// <param name="replacement">替换字节序列。</param>
    /// <returns>替换后生成的新字节序列。</returns>
    public abstract Byte[] Replace(ReadOnlySpan<Byte> input, ReadOnlySpan<Byte> replacement);
}
