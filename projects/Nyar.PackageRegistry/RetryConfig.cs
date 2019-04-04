namespace Nyar.PackageRegistry;

/// <summary>
///     注册表重试配置
/// </summary>
public sealed class RetryConfig
{
    /// <summary>
    ///     最大重试次数，默认 3
    /// </summary>
    public int max_retries { get; init; } = 3;

    /// <summary>
    ///     初始延迟，默认 500ms
    /// </summary>
    public TimeSpan initial_delay { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    ///     延迟倍数，默认 2（指数退避）
    /// </summary>
    public double backoff_multiplier { get; init; } = 2.0;

    /// <summary>
    ///     最大延迟上限，默认 10s
    /// </summary>
    public TimeSpan max_delay { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     判断响应是否应重试，默认仅对 429 和 5xx 重试
    /// </summary>
    public Func<HttpResponseMessage, bool>? should_retry { get; init; }

    /// <summary>
    ///     默认重试配置
    /// </summary>
    public static RetryConfig @default { get; } = new();
}