namespace Std.Data.Text.Valkyrie.AST.Neural;

/// <summary>
///     Neural 推理配置声明，用于控制模型的推理行为
/// </summary>
/// <para>示例：</para>
/// <code>
/// inference {
///     device = "gpu";
///     precision = "fp16";
///     kv_cache = true;
///     kv_cache_dtype = "fp16";
///     max_batch_size = 8;
///     max_seq_len = 2048;
/// }
/// </code>
public sealed record NeuralInferenceConfigDecl : ValkyrieNode
{
    /// <summary>
    ///     是否启用 KV 缓存（用于 Transformer 推理加速）
    /// </summary>
    public bool kv_cache_enabled { get; init; }

    /// <summary>
    ///     KV 缓存数据类型（如 <c>"fp16"</c>、<c>"fp32"</c>）
    /// </summary>
    public string? kv_cache_dtype { get; init; }

    /// <summary>
    ///     最大批处理大小
    /// </summary>
    public int? max_batch_size { get; init; }

    /// <summary>
    ///     最大序列长度
    /// </summary>
    public int? max_seq_len { get; init; }

    /// <summary>
    ///     额外配置条目（用于未来扩展）
    /// </summary>
    public IReadOnlyList<ConfigEntry> entries { get; init; } = [];
}