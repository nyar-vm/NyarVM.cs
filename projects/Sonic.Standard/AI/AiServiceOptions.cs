namespace Std.AI;

/// <summary>
///     AI 服务配置选项，仅包含通用配置。
///     供应商特定的配置由各供应商实现自行管理。
/// </summary>
public sealed class AiServiceOptions
{
    /// <summary>
    ///     默认模型名称。
    /// </summary>
    public string default_model { get; set; } = "gpt-4o-mini";

    /// <summary>
    ///     默认嵌入模型名称。
    /// </summary>
    public string default_embedding_model { get; set; } = "text-embedding-3-small";

    /// <summary>
    ///     默认温度参数。
    /// </summary>
    public double default_temperature { get; set; } = 0.7;

    /// <summary>
    ///     请求超时时间（秒）。
    /// </summary>
    public int request_timeout_seconds { get; set; } = 120;

    /// <summary>
    ///     最大重试次数。
    /// </summary>
    public int max_retries { get; set; } = 3;
}