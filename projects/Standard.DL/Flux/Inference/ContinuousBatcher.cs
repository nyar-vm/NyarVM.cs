namespace Std.DL.Flux.Inference;

/// <summary>
///     批次中单个请求的执行状态
///     跟踪每个请求在 Continuous Batching 中的位置和阶段
/// </summary>
public sealed class BatchRequestState
{
    /// <summary>
    ///     创建批次请求状态
    /// </summary>
    /// <param name="request">推理请求</param>
    public BatchRequestState(InferenceRequest request)
    {
        Request = request;
        Handle = null;
        Phase = RequestState.Prefilling;
        CurrentPosition = 0;
        PromptLength = request.PromptTokenIds.Length;
        GeneratedCount = 0;
    }

    /// <summary>
    ///     关联的推理请求
    /// </summary>
    public InferenceRequest Request { get; }

    /// <summary>
    ///     PagedKVCache 中的请求句柄
    /// </summary>
    public RequestHandle? Handle { get; set; }

    /// <summary>
    ///     当前请求阶段
    /// </summary>
    public RequestState Phase { get; set; }

    /// <summary>
    ///     当前已处理的 token 位置（含 prompt + 已生成 token）
    /// </summary>
    public int CurrentPosition { get; set; }

    /// <summary>
    ///     Prompt token 数量
    /// </summary>
    public int PromptLength { get; }

    /// <summary>
    ///     已生成的 token 数
    /// </summary>
    public int GeneratedCount { get; set; }

    /// <summary>
    ///     是否已完成预填充
    /// </summary>
    public bool IsPrefillCompleted => Phase == RequestState.Decoding;

    /// <summary>
    ///     是否应该从批次中移除
    /// </summary>
    public bool ShouldRemove =>
        Phase is RequestState.Completed or RequestState.Failed;
}

/// <summary>
///     批次快照 —— 描述当前迭代中批次组合的完整状态
///     包含预填充请求和解码请求的分离视图
/// </summary>
public sealed class BatchState
{
    /// <summary>
    ///     创建批次快照
    /// </summary>
    /// <param name="allRequests">所有请求状态</param>
    /// <param name="prefillRequests">预填充请求</param>
    /// <param name="decodeRequests">解码请求</param>
    public BatchState(
        IReadOnlyList<BatchRequestState> allRequests,
        IReadOnlyList<BatchRequestState> prefillRequests,
        IReadOnlyList<BatchRequestState> decodeRequests)
    {
        AllRequests = allRequests;
        PrefillRequests = prefillRequests;
        DecodeRequests = decodeRequests;
    }

    /// <summary>
    ///     当前批次中所有请求的状态
    /// </summary>
    public IReadOnlyList<BatchRequestState> AllRequests { get; }

    /// <summary>
    ///     处于预填充阶段的请求
    /// </summary>
    public IReadOnlyList<BatchRequestState> PrefillRequests { get; }

    /// <summary>
    ///     处于解码阶段的请求
    /// </summary>
    public IReadOnlyList<BatchRequestState> DecodeRequests { get; }

    /// <summary>
    ///     当前批次大小
    /// </summary>
    public int BatchSize => AllRequests.Count;

    /// <summary>
    ///     预填充请求总数
    /// </summary>
    public int PrefillCount => PrefillRequests.Count;

    /// <summary>
    ///     解码请求总数
    /// </summary>
    public int DecodeCount => DecodeRequests.Count;

    /// <summary>
    ///     批次是否为空
    /// </summary>
    public bool IsEmpty => AllRequests.Count == 0;
}

/// <summary>
///     Continuous Batcher —— 迭代级批次组合管理器
///     vLLM 风格的 Continuous Batching：每个迭代动态调整批次组成
///     - 移除已完成的请求
///     - 从调度器拉取新请求填充批次
///     - 跟踪每个请求的预填充/解码状态
/// </summary>
public sealed class ContinuousBatcher
{
    #region 构造函数

    /// <summary>
    ///     创建 Continuous Batcher
    /// </summary>
    /// <param name="scheduler">推理调度器</param>
    /// <param name="maxBatchSize">最大批次大小</param>
    public ContinuousBatcher(IInferenceScheduler scheduler, int maxBatchSize = 32)
    {
        _scheduler = scheduler;
        MaxBatchSize = maxBatchSize;
        _activeStates = new Dictionary<int, BatchRequestState>();
        _pendingPrefill = [];
    }

    #endregion

    #region 字段

    private readonly IInferenceScheduler _scheduler;
    private readonly Dictionary<int, BatchRequestState> _activeStates;
    private readonly List<BatchRequestState> _pendingPrefill;

    #endregion

    #region 属性

    /// <summary>
    ///     当前活跃请求数
    /// </summary>
    public int ActiveRequestCount => _activeStates.Count;

    /// <summary>
    ///     最大批次大小
    /// </summary>
    public int MaxBatchSize { get; }

    #endregion

    #region 批次管理

    /// <summary>
    ///     提交推理请求（经由调度器排队）
    /// </summary>
    /// <param name="request">推理请求</param>
    public void SubmitRequest(InferenceRequest request)
    {
        _scheduler.SubmitRequest(request);
    }

    /// <summary>
    ///     组装下一个迭代的批次快照
    ///     1. 移除已完成/失败的请求
    ///     2. 从调度器拉取新请求填充批次
    ///     3. 返回当前批次状态快照
    /// </summary>
    /// <returns>批次快照</returns>
    public BatchState ComposeBatch()
    {
        RemoveCompletedRequests();

        var availableSlots = MaxBatchSize - _activeStates.Count;
        if (availableSlots > 0)
        {
            var newRequests = _scheduler.GetNextBatch(availableSlots);
            foreach (var request in newRequests)
            {
                var state = new BatchRequestState(request);
                _activeStates[request.RequestId] = state;
                _pendingPrefill.Add(state);
            }
        }

        return BuildBatchState();
    }

    /// <summary>
    ///     标记请求已完成
    /// </summary>
    /// <param name="requestId">请求 ID</param>
    public void MarkCompleted(int requestId)
    {
        _scheduler.MarkCompleted(requestId);
    }

    /// <summary>
    ///     获取指定请求的批次状态
    /// </summary>
    /// <param name="requestId">请求 ID</param>
    /// <returns>批次请求状态，不存在则返回 null</returns>
    public BatchRequestState? GetState(int requestId)
    {
        return _activeStates.GetValueOrDefault(requestId);
    }

    /// <summary>
    ///     将请求从预填充阶段推进到解码阶段
    /// </summary>
    /// <param name="requestId">请求 ID</param>
    public void AdvanceToDecode(int requestId)
    {
        if (_activeStates.TryGetValue(requestId, out var state))
        {
            state.Phase = RequestState.Decoding;
            _pendingPrefill.RemoveAll(s => s.Request.RequestId == requestId);
        }
    }

    /// <summary>
    ///     记录请求生成了一个 token
    /// </summary>
    /// <param name="requestId">请求 ID</param>
    /// <param name="tokenId">生成的 token ID</param>
    public void RecordTokenGenerated(int requestId, int tokenId)
    {
        if (_activeStates.TryGetValue(requestId, out var state))
        {
            state.GeneratedCount++;
            state.CurrentPosition++;
            state.Request.AppendGeneratedToken(tokenId);
        }
    }

    /// <summary>
    ///     记录请求预填充完成，更新位置
    /// </summary>
    /// <param name="requestId">请求 ID</param>
    /// <param name="prefillLength">预填充的 token 数</param>
    public void RecordPrefillCompleted(int requestId, int prefillLength)
    {
        if (_activeStates.TryGetValue(requestId, out var state))
        {
            state.CurrentPosition = prefillLength;
            AdvanceToDecode(requestId);
        }
    }

    /// <summary>
    ///     标记请求失败
    /// </summary>
    /// <param name="requestId">请求 ID</param>
    /// <param name="error">异常信息</param>
    public void MarkFailed(int requestId, Exception error)
    {
        if (_activeStates.TryGetValue(requestId, out var state))
        {
            state.Phase = RequestState.Failed;
            state.Request.Fail(error);
            _pendingPrefill.RemoveAll(s => s.Request.RequestId == requestId);
        }
    }

    #endregion

    #region 私有方法

    private void RemoveCompletedRequests()
    {
        var completedIds = new List<int>();

        foreach (var kvp in _activeStates)
            if (kvp.Value.ShouldRemove)
                completedIds.Add(kvp.Key);

        foreach (var id in completedIds)
        {
            _activeStates.Remove(id);
            _pendingPrefill.RemoveAll(s => s.Request.RequestId == id);
        }
    }

    private BatchState BuildBatchState()
    {
        var all = new List<BatchRequestState>();
        var prefill = new List<BatchRequestState>();
        var decode = new List<BatchRequestState>();

        foreach (var state in _activeStates.Values)
        {
            all.Add(state);

            if (state.Phase == RequestState.Prefilling)
                prefill.Add(state);
            else if (state.Phase == RequestState.Decoding) decode.Add(state);
        }

        return new BatchState(all, prefill, decode);
    }

    #endregion
}