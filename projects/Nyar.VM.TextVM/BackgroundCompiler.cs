namespace Nyar.VM.TextVM;

/// <summary>
/// 后台编译器。在用户按键间隙投机编译动态查询。
/// 支持撤销令牌机制：新按键使旧令牌失效，丢弃半成品。
/// </summary>
internal class BackgroundCompiler
{
    private readonly Dictionary<Int64, CompiledUnit> _results = new Dictionary<Int64, CompiledUnit>();
    private CancellationTokenSource? _currentCts;
    private Int64 _currentToken;

    /// <summary>
    /// 将查询加入后台编译队列。
    /// </summary>
    /// <param name="query">要编译的动态查询。</param>
    /// <param name="config">动态查询配置。</param>
    public void Enqueue(DynamicQuery query, DynamicConfig config)
    {
        // Cancel any previous compilation task
        _currentCts?.Cancel();
        _currentCts = new CancellationTokenSource();
        _currentToken = query.Token;

        CancellationToken token = _currentCts.Token;
        Int64 taskToken = query.Token;

        // Start background compilation
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(config.IdleCompilationDelayMs, token);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                // Simple fallback: create LiteralExecutor from the pattern bytes
                Byte[] patternBytes = System.Text.Encoding.UTF8.GetBytes(query.Pattern);
                CompiledUnit unit = new LiteralExecutor(patternBytes, query.Encoding);

                lock (_results)
                {
                    // Only store if this token is still current
                    if (_currentToken == taskToken && !token.IsCancellationRequested)
                    {
                        _results[taskToken] = unit;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Task was cancelled, discard results silently
            }
        }, token);
    }

    /// <summary>
    /// 尝试获取指定令牌对应的编译结果。若尚未完成或已撤销，返回 null。
    /// </summary>
    /// <param name="token">版本令牌。</param>
    /// <returns>若编译已完成则返回 <see cref="CompiledUnit"/> 实例，否则返回 null。</returns>
    public CompiledUnit? TryTake(Int64 token)
    {
        lock (_results)
        {
            if (_results.TryGetValue(token, out CompiledUnit? unit))
            {
                _results.Remove(token);
                return unit;
            }

            return null;
        }
    }
}
