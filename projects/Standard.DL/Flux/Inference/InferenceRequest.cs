namespace Std.DL.Flux.Inference;

/// <summary>
///     推理请求状态枚举
/// </summary>
public enum RequestState
{
    /// <summary>
    ///     排队等待中
    /// </summary>
    Queued,

    /// <summary>
    ///     预填充阶段（处理 prompt token）
    /// </summary>
    Prefilling,

    /// <summary>
    ///     解码阶段（逐 token 生成）
    /// </summary>
    Decoding,

    /// <summary>
    ///     已完成
    /// </summary>
    Completed,

    /// <summary>
    ///     失败
    /// </summary>
    Failed
}

/// <summary>
///     推理请求 —— 描述一次 LLM 推理请求的完整信息
///     包含输入 prompt、生成参数、流式回调和结果等待句柄
/// </summary>
public sealed class InferenceRequest
{
    /// <summary>
    ///     创建推理请求
    /// </summary>
    /// <param name="requestId">请求唯一标识</param>
    /// <param name="promptTokenIds">输入 prompt 的 token ID 序列</param>
    /// <param name="maxNewTokens">最大新生成 token 数</param>
    /// <param name="temperature">采样温度</param>
    /// <param name="topK">Top-k 采样参数</param>
    /// <param name="topP">Top-p 采样参数</param>
    /// <param name="priority">请求优先级</param>
    /// <param name="isStreaming">是否启用流式输出</param>
    /// <param name="onTokenGenerated">流式回调</param>
    public InferenceRequest(
        int requestId,
        int[] promptTokenIds,
        int maxNewTokens = 128,
        float temperature = 0.8f,
        int topK = 0,
        float topP = 1.0f,
        int priority = 0,
        bool isStreaming = false,
        Action<int>? onTokenGenerated = null)
    {
        RequestId = requestId;
        PromptTokenIds = promptTokenIds;
        MaxNewTokens = maxNewTokens;
        Temperature = temperature;
        TopK = topK;
        TopP = topP;
        Priority = priority;
        IsStreaming = isStreaming;
        OnTokenGenerated = onTokenGenerated;
        CompletionSource = new TaskCompletionSource<int[]>();
        State = RequestState.Queued;
        GeneratedTokenIds = [];
    }

    /// <summary>
    ///     请求唯一标识
    /// </summary>
    public int RequestId { get; init; }

    /// <summary>
    ///     输入 prompt 的 token ID 序列
    /// </summary>
    public int[] PromptTokenIds { get; init; }

    /// <summary>
    ///     最大新生成 token 数
    /// </summary>
    public int MaxNewTokens { get; init; }

    /// <summary>
    ///     采样温度（0 表示贪心解码）
    /// </summary>
    public float Temperature { get; init; }

    /// <summary>
    ///     Top-k 采样参数（0 表示不过滤）
    /// </summary>
    public int TopK { get; init; }

    /// <summary>
    ///     Top-p (Nucleus) 采样参数（1.0 表示不过滤）
    /// </summary>
    public float TopP { get; init; }

    /// <summary>
    ///     请求优先级（数值越大优先级越高）
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    ///     是否启用流式输出
    /// </summary>
    public bool IsStreaming { get; init; }

    /// <summary>
    ///     流式回调：每生成一个 token 时触发
    /// </summary>
    public Action<int>? OnTokenGenerated { get; init; }

    /// <summary>
    ///     结果等待句柄：调用方通过 await 获取完整输出 token 序列
    /// </summary>
    public TaskCompletionSource<int[]> CompletionSource { get; }

    /// <summary>
    ///     当前请求状态
    /// </summary>
    public RequestState State { get; set; }

    /// <summary>
    ///     已生成的 token ID 列表
    /// </summary>
    public List<int> GeneratedTokenIds { get; }

    /// <summary>
    ///     追加一个生成的 token 并触发流式回调
    /// </summary>
    /// <param name="tokenId">生成的 token ID</param>
    public void AppendGeneratedToken(int tokenId)
    {
        GeneratedTokenIds.Add(tokenId);

        if (IsStreaming && OnTokenGenerated != null) OnTokenGenerated(tokenId);
    }

    /// <summary>
    ///     标记请求完成，设置结果
    /// </summary>
    public void Complete()
    {
        State = RequestState.Completed;
        CompletionSource.TrySetResult([.. GeneratedTokenIds]);
    }

    /// <summary>
    ///     标记请求失败
    /// </summary>
    /// <param name="error">异常信息</param>
    public void Fail(Exception error)
    {
        State = RequestState.Failed;
        CompletionSource.TrySetException(error);
    }
}