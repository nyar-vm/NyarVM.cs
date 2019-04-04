using System.Runtime.CompilerServices;

namespace Nyar.VM.TextVM;

/// <summary>
/// 引擎加载器。从 .tvm 字节数据中加载并缓存 CompiledUnit。
/// 根据 TvmHeader 的标志位路由到对应的执行器类型。
/// </summary>
public static class EngineLoader
{
    /// <summary>
    /// 编译单元缓存，以字节数组为键进行弱引用缓存。
    /// </summary>
    private static readonly ConditionalWeakTable<Byte[], CompiledUnit> _cache = new();

    /// <summary>
    /// 同步锁，用于缓存写入的线程安全。
    /// </summary>
    private static readonly Object _lock = new();

    /// <summary>
    /// 从 .tvm 字节数据中加载编译后的执行单元。
    /// 首次加载时会解析头部并创建对应的执行器，后续调用返回缓存实例。
    /// </summary>
    /// <param name="engineBytes">.tvm 格式的引擎字节数据。</param>
    /// <returns>编译后的执行单元。</returns>
    /// <exception cref="InvalidDataException">引擎字节数据无效时引发。</exception>
    public static CompiledUnit Load(ReadOnlySpan<Byte> engineBytes)
    {
        Byte[] key = [.. engineBytes];

        if (_cache.TryGetValue(key, out CompiledUnit? existing))
        {
            return existing;
        }

        CompiledUnit unit = CreateUnit(key);

        lock (_lock)
        {
            // 双重检查锁定，防止并发重复创建
            if (!_cache.TryGetValue(key, out _))
            {
                _cache.Add(key, unit);
            }
        }

        return unit;
    }

    /// <summary>
    /// 根据头部信息创建对应的执行单元。
    /// </summary>
    /// <param name="engineBytes">.tvm 格式的引擎字节数据。</param>
    /// <returns>创建的编译单元实例。</returns>
    private static CompiledUnit CreateUnit(Byte[] engineBytes)
    {
        TvmHeader header = TvmHeader.Read(engineBytes.AsSpan());
        TextEncoding encoding = (TextEncoding)header.Encoding;

        if ((header.Flags & TvmFlags.IsLiteral) != 0)
        {
            return CreateLiteralExecutor(engineBytes, header, encoding);
        }

        if ((header.Flags & TvmFlags.IsNfa) != 0)
        {
            return CreateNfaExecutor(engineBytes, header, encoding);
        }

        return CreateDfaExecutor(engineBytes, header, encoding);
    }

    /// <summary>
    /// 从引擎字节数据中提取模式并创建字面量执行器。
    /// </summary>
    /// <param name="engineBytes">引擎字节数据。</param>
    /// <param name="header">已解析的头部信息。</param>
    /// <param name="encoding">文本编码类型。</param>
    /// <returns>字面量执行器实例。</returns>
    private static LiteralExecutor CreateLiteralExecutor(
        Byte[] engineBytes, TvmHeader header, TextEncoding encoding)
    {
        Int32 patternOffset = (Int32)header.LiteralPrefixOffset;
        Int32 patternLen = (Int32)header.MinMatchLen;
        Byte[] pattern = [.. engineBytes.AsSpan(patternOffset, patternLen)];

        return new LiteralExecutor(pattern, encoding);
    }

    /// <summary>
    /// 从引擎字节数据中提取 NFA 表并创建位并行 NFA 执行器。
    /// </summary>
    /// <param name="engineBytes">引擎字节数据。</param>
    /// <param name="header">已解析的头部信息。</param>
    /// <param name="encoding">文本编码类型。</param>
    /// <returns>位并行 NFA 执行器实例。</returns>
    private static BitParallelNfaExecutor CreateNfaExecutor(
        Byte[] engineBytes, TvmHeader header, TextEncoding encoding)
    {
        Int32 nfaOffset = (Int32)header.DfaTableOffset;
        Int32 nfaLen = engineBytes.Length - nfaOffset;
        Byte[] nfaData = [.. engineBytes.AsSpan(nfaOffset, nfaLen)];

        return new BitParallelNfaExecutor(nfaData, encoding);
    }

    /// <summary>
    /// 从引擎字节数据中提取 DFA 表并创建 DFA 执行器。
    /// </summary>
    /// <param name="engineBytes">引擎字节数据。</param>
    /// <param name="header">已解析的头部信息。</param>
    /// <param name="encoding">文本编码类型。</param>
    /// <returns>DFA 执行器实例。</returns>
    private static DfaExecutor CreateDfaExecutor(
        Byte[] engineBytes, TvmHeader header, TextEncoding encoding)
    {
        Int32 dfaOffset = (Int32)header.DfaTableOffset;
        Int32 dfaLen = engineBytes.Length - dfaOffset;
        Byte[] dfaData = [.. engineBytes.AsSpan(dfaOffset, dfaLen)];

        return new DfaExecutor(dfaData, encoding);
    }
}
