using Nyar.VM.TextVM.Compiler;

namespace Nyar.VM.TextVM;

/// <summary>
/// 静态查询。在构造时执行全量静态分析和编译，将执行单元绑定到实例。
/// 运行时调用 <see cref="Execute"/> 无锁、无查表、零分配。
/// </summary>
public class StaticQuery
{
    private readonly CompiledUnit _unit;
    private readonly Byte[]? _compiledBytes;

    /// <summary>
    /// 编译正则模式为静态查询。
    /// </summary>
    /// <param name="pattern">正则表达式模式字符串。</param>
    /// <param name="encoding">目标编码。</param>
    /// <param name="operation">操作类型。</param>
    /// <param name="config">编译配置。</param>
    /// <returns>编译后的静态查询实例。</returns>
    public static StaticQuery Compile(
        String pattern,
        TextEncoding encoding,
        TvmOperation operation,
        StaticConfig? config = null)
    {
        config ??= new StaticConfig();

        // 解析模式
        var (node, error) = PatternParser.Parse(pattern);
        if (node is null)
        {
            throw new ArgumentException($"模式语法错误: {error}", nameof(pattern));
        }

        // 布尔代数折叠
        AstNode folded = BooleanFolder.Fold(node, out var warnings);

        // 静态分析
        var analysis = StaticAnalyzer.Analyze(folded, encoding);

        // 根据复杂度选择执行路径
        if (analysis.Complexity == Complexity.Literal)
        {
            // 字面量路径：提取字节并创建 LiteralExecutor
            byte[] literalBytes = ExtractLiteralBytes(folded, encoding);
            return new StaticQuery(new LiteralExecutor(literalBytes, encoding));
        }

        if (analysis.Complexity == Complexity.ContextFree && config.BacktrackTimeoutMs > 0)
        {
            // 上下文无关路径：编译为 BacktrackExecutor
            var (program, captureCount) = BacktrackCompiler.Compile(folded);
            return new StaticQuery(new BacktrackExecutor(program, captureCount, config.BacktrackTimeoutMs));
        }

        // 常规路径：构建 DFA 并序列化为完整 .tvm 格式
        var builder = new DFABuilder(folded, config.MaxDfaStates);
        builder.Build();
        byte[] dfaTable = builder.Serialize();
        byte[] tvmBytes = AssembleTvmFile(dfaTable, builder.StateCount);
        CompiledUnit unit = EngineLoader.Load(tvmBytes);
        return new StaticQuery(unit, tvmBytes);
    }

    /// <summary>
    /// 组装完整的 .tvm 文件：TvmHeader(32B) + 裸 DFA 表。
    /// </summary>
    /// <param name="dfaTable">DFABuilder 序列化的裸 DFA 表数据。</param>
    /// <param name="stateCount">DFA 状态数量。</param>
    /// <returns>完整的 .tvm 字节数组。</returns>
    private static Byte[] AssembleTvmFile(Byte[] dfaTable, Int32 stateCount)
    {
        Int32 totalSize = TvmHeader.HeaderSize + dfaTable.Length;
        Byte[] buffer = new Byte[totalSize];
        Span<Byte> span = buffer.AsSpan();

        TvmHeader header = new TvmHeader(
            version: 1,
            encoding: 0,
            operation: (Byte)TvmOperation.Find,
            flags: 0,
            minMatchLen: 0,
            literalPrefixOffset: 0,
            dfaTableOffset: TvmHeader.HeaderSize,
            dfaStateCount: (UInt32)stateCount,
            totalSize: (UInt32)totalSize);
        header.Write(span);

        dfaTable.AsSpan().CopyTo(span[TvmHeader.HeaderSize..]);
        return buffer;
    }

    /// <summary>
    /// 从字面量模式中提取字节序列。
    /// </summary>
    /// <param name="node">已折叠的 AST 节点。</param>
    /// <param name="encoding">文本编码类型。</param>
    /// <returns>字面量模式的 UTF-8 编码字节数组。</returns>
    private static Byte[] ExtractLiteralBytes(AstNode node, TextEncoding encoding)
    {
        if (node is LiteralNode lit)
        {
            return System.Text.Encoding.UTF8.GetBytes(lit.Value);
        }
        return System.Text.Encoding.UTF8.GetBytes(node.ToString() ?? "");
    }

    /// <summary>
    /// 执行存在判定。
    /// </summary>
    /// <param name="input">待搜索的输入字节切片。</param>
    /// <returns>若存在匹配则返回 true，否则返回 false。</returns>
    public Boolean Exists(ReadOnlySpan<Byte> input)
    {
        return _unit.IsMatch(input);
    }

    /// <summary>
    /// 执行查找。
    /// </summary>
    /// <param name="input">待搜索的输入字节切片。</param>
    /// <returns>若找到匹配则返回 <see cref="Match"/> 实例，否则返回 null。</returns>
    public Match? Find(ReadOnlySpan<Byte> input)
    {
        return _unit.FindFirst(input);
    }

    /// <summary>
    /// 查找所有匹配。
    /// </summary>
    /// <param name="input">待搜索的输入字节切片。</param>
    /// <returns>所有匹配的 <see cref="Match"/> 列表。</returns>
    public IEnumerable<Match> FindAll(ReadOnlySpan<Byte> input)
    {
        return _unit.FindAll(input);
    }

    /// <summary>
    /// 执行替换。
    /// </summary>
    /// <param name="input">待处理的输入字节切片。</param>
    /// <param name="replacement">替换内容的字节切片。</param>
    /// <returns>替换后的字节数组。</returns>
    public Byte[] Replace(ReadOnlySpan<Byte> input, ReadOnlySpan<Byte> replacement)
    {
        return _unit.Replace(input, replacement);
    }

    /// <summary>
    /// 创建静态查询实例（无编译字节，用于字面量和回溯路径）。
    /// </summary>
    /// <param name="unit">编译后的执行单元。</param>
    internal StaticQuery(CompiledUnit unit)
    {
        _unit = unit;
        _compiledBytes = null;
    }

    /// <summary>
    /// 创建静态查询实例（DFA 路径，携带完整 .tvm 字节）。
    /// </summary>
    /// <param name="unit">编译后的执行单元。</param>
    /// <param name="compiledBytes">完整的 .tvm 字节数组。</param>
    internal StaticQuery(CompiledUnit unit, Byte[] compiledBytes)
    {
        _unit = unit;
        _compiledBytes = compiledBytes;
    }

    /// <summary>
    /// 获取编译后的 .tvm 字节数组，DFA 路径返回完整文件，其他路径返回 null。
    /// </summary>
    internal Byte[]? GetCompiledBytes()
    {
        return _compiledBytes;
    }

    /// <summary>
    /// 从 .tvm 字节数组反序列化创建静态查询。
    /// </summary>
    /// <param name="tvmBytes">完整的 .tvm 字节数组。</param>
    /// <param name="encoding">文本编码类型。</param>
    /// <param name="operation">操作类型。</param>
    /// <returns>编译后的静态查询实例。</returns>
    public static StaticQuery FromBytes(Byte[] tvmBytes, TextEncoding encoding, TvmOperation operation)
    {
        CompiledUnit unit = EngineLoader.Load(tvmBytes);
        return new StaticQuery(unit);
    }
}
