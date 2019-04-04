namespace Nyar.VM.TextVM;

/// <summary>
/// TextVM 运行时环境。管理 LRU 缓存、后台投机编译线程和同步降级策略。
/// 用于动态（用户输入）查询场景。
/// </summary>
public class TextVM
{
    private readonly DynamicConfig _config;
    private readonly Dictionary<String, CompiledUnit> _lruCache;
    private readonly Object _cacheLock = new Object();
    private readonly BackgroundCompiler _bgCompiler;
    private Int64 _nextToken = 1;

    /// <summary>
    /// 创建 TextVM 实例。
    /// </summary>
    /// <param name="config">动态查询配置。若为 null 则使用默认配置。</param>
    public TextVM(DynamicConfig? config = null)
    {
        _config = config ?? new DynamicConfig();
        _lruCache = new Dictionary<String, CompiledUnit>(_config.LruCacheSize);
        _bgCompiler = new BackgroundCompiler();
    }

    /// <summary>
    /// 通知 VM 用户正在输入新模式，触发后台投机编译。
    /// </summary>
    /// <param name="pattern">模式字符串。</param>
    /// <param name="encoding">目标编码。</param>
    /// <param name="operation">操作类型。</param>
    /// <returns>分配给此查询的版本令牌。</returns>
    public Int64 NotifyTyping(String pattern, TextEncoding encoding, TvmOperation operation)
    {
        Int64 token = Interlocked.Increment(ref _nextToken);
        DynamicQuery query = new DynamicQuery(pattern, encoding, operation, token);
        _bgCompiler.Enqueue(query, _config);
        return token;
    }

    /// <summary>
    /// 执行动态查询。三级路由：LRU 缓存 → 后台投机结果 → 同步降级。
    /// </summary>
    /// <param name="query">要执行的动态查询。</param>
    /// <param name="input">待搜索的输入字节切片。</param>
    /// <returns>所有匹配结果的枚举。</returns>
    public IEnumerable<Match> Execute(DynamicQuery query, ReadOnlySpan<Byte> input)
    {
        // Level 1: Check LRU cache
        lock (_cacheLock)
        {
            if (_lruCache.TryGetValue(query.Pattern, out CompiledUnit? cached))
            {
                return cached.FindAll(input);
            }
        }

        // Level 2: Try background compilation result
        CompiledUnit? bgUnit = _bgCompiler.TryTake(query.Token);
        if (bgUnit is not null)
        {
            lock (_cacheLock)
            {
                _lruCache[query.Pattern] = bgUnit;
            }
            return bgUnit.FindAll(input);
        }

        // Level 3: Sync fallback
        CompiledUnit fallback = BuildFallback(query);
        return fallback.FindAll(input);
    }

    private CompiledUnit BuildFallback(DynamicQuery query)
    {
        // Use literal bytes of the pattern for Two-Way BM
        Byte[] patternBytes = System.Text.Encoding.UTF8.GetBytes(query.Pattern);
        return new LiteralExecutor(patternBytes, query.Encoding);
    }
}
