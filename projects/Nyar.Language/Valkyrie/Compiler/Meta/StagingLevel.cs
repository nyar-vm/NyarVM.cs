namespace Nyar.Language.Valkyrie.Compiler.Meta;

// =====================================================================
// Staging Level
// =====================================================================

/// <summary>
///     MSP staging 层级。
///     Level 0 = 运行时代码，Level 1 = 一层 meta，Level 2 = 二层 meta……
///     每个 level 持有自己的 bindings、target 和 parent 引用。
/// </summary>
public sealed class StagingLevel
{
    private readonly Dictionary<string, MetaValue> _bindings = new();

    /// <summary>
    ///     初始化一层 staging 环境。
    /// </summary>
    /// <param name="level">层级索引（0 = runtime）</param>
    /// <param name="canonicalTriple">此层的目标 CanonicalTriple</param>
    /// <param name="arch">架构标签</param>
    /// <param name="impl">实现</param>
    /// <param name="spec">规格</param>
    /// <param name="abi">ABI</param>
    /// <param name="parent">父层级（null 表示最外层）</param>
    public StagingLevel(int level, string canonicalTriple, string arch, string impl, string spec, string abi,
        StagingLevel? parent = null)
    {
        this.level = level;
        canonical_triple = canonicalTriple;
        this.arch = arch;
        this.impl = impl;
        this.spec = spec;
        this.abi = abi;
        this.parent = parent;
    }

    /// <summary>
    ///     层级索引
    /// </summary>
    public int level { get; }

    /// <summary>
    ///     完整 CanonicalTriple
    /// </summary>
    public string canonical_triple { get; }

    /// <summary>
    ///     架构标签（如 clr、jvm、wasm32）
    /// </summary>
    public string arch { get; }

    /// <summary>
    ///     实现标签（如 microsoft、openjdk）
    /// </summary>
    public string impl { get; }

    /// <summary>
    ///     规格标签（如 windows、linux、browser）
    /// </summary>
    public string spec { get; }

    /// <summary>
    ///     ABI 标签（如 managed、wasm、msvc）
    /// </summary>
    public string abi { get; }

    /// <summary>
    ///     完整 target 字符串
    /// </summary>
    public string target => canonical_triple;

    /// <summary>
    ///     兼容别名
    /// </summary>
    public string vendor => impl;

    /// <summary>
    ///     兼容别名
    /// </summary>
    public string os => spec;

    /// <summary>
    ///     父层级（null 表示最外层）
    /// </summary>
    public StagingLevel? parent { get; }

    /// <summary>
    ///     当前层的变量绑定
    /// </summary>
    public IReadOnlyDictionary<string, MetaValue> bindings => _bindings;

    /// <summary>
    ///     在当前层绑定一个变量。
    /// </summary>
    public void bind(string name, MetaValue value)
    {
        _bindings[name] = value;
    }

    /// <summary>
    ///     在当前层及祖先层中查找变量绑定。
    ///     Level 0 的变量可被 Level 1 的代码通过 Escape 引用。
    /// </summary>
    public MetaValue? lookup(string name)
    {
        if (_bindings.TryGetValue(name, out var value)) return value;

        return parent?.lookup(name);
    }

    /// <summary>
    ///     从完整的 CanonicalTriple 创建 Level 1 staging 环境。
    /// </summary>
    public static StagingLevel from_canonical_triple(string canonicalTriple)
    {
        var segments = canonicalTriple.Split('-');
        return new StagingLevel(
            1,
            canonicalTriple,
            segments.Length > 0 ? segments[0] : string.Empty,
            segments.Length > 1 ? segments[1] : string.Empty,
            segments.Length > 2 ? segments[2] : string.Empty,
            segments.Length > 3 ? segments[3] : string.Empty);
    }

    /// <summary>
    ///     从当前层派生更深一层的 staging 环境（Level + 1）。
    /// </summary>
    public StagingLevel deeper()
    {
        return new StagingLevel(level + 1, canonical_triple, arch, impl, spec, abi, this);
    }
}

// =====================================================================
// Meta Value
// =====================================================================

// =====================================================================
// Meta Stager
// =====================================================================