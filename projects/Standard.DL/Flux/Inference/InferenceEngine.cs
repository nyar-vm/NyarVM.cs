namespace Std.DL.Flux.Inference;

/// <summary>
///     推理引擎 —— Continuous Batching 推理系统的核心编排器
///     将模型前向函数、PagedKVCache、调度器和批次管理器组合为完整的推理管线
///     支持多请求并发、预填充/解码分离、流式输出
/// </summary>
public sealed class InferenceEngine
{
    #region 构造函数

    /// <summary>
    ///     创建推理引擎
    /// </summary>
    /// <param name="forwardFn">模型前向函数：inputIds [batch, seqLen] → logits [batch, seqLen, vocabSize]</param>
    /// <param name="kvCache">PagedKVCache 实例</param>
    /// <param name="scheduler">推理调度器</param>
    /// <param name="maxBatchSize">最大批次大小</param>
    /// <param name="vocabSize">词表大小</param>
    /// <param name="eosTokenId">终止符 token ID（-1 表示不检测 EOS）</param>
    public InferenceEngine(
        Func<ArrayND, ArrayND> forwardFn,
        PagedKVCache kvCache,
        IInferenceScheduler scheduler,
        int maxBatchSize = 32,
        int vocabSize = 32000,
        int eosTokenId = -1)
    {
        _forwardFn = forwardFn;
        KVCache = kvCache;
        _batcher = new ContinuousBatcher(scheduler, maxBatchSize);
        MaxBatchSize = maxBatchSize;
        VocabSize = vocabSize;
        _eosTokenId = eosTokenId;
        _nextRequestId = 0;
        IsRunning = false;
    }

    #endregion

    #region 解码处理

    private void ProcessDecodeRequests(BatchState batchState, CancellationToken cancellationToken = default)
    {
        if (batchState.DecodeCount == 0) return;

        var decodeStates = batchState.DecodeRequests;
        var batchSize = decodeStates.Count;

        var inputIds = ArrayND.Zeros(batchSize, 1);
        var spanInput = inputIds.AsWriteSpan();

        for (var i = 0; i < batchSize; i++)
        {
            var state = decodeStates[i];
            var lastToken = state.Request.GeneratedTokenIds[^1];
            spanInput[i] = lastToken;
        }

        var logits = _forwardFn(inputIds);

        var completedIds = new List<int>();

        for (var i = 0; i < batchSize; i++)
        {
            var state = decodeStates[i];

            try
            {
                var requestLogits = ExtractBatchItemLogits(logits, i);
                var newToken = SampleFromLogits(requestLogits, state.Request);

                _batcher.RecordTokenGenerated(state.Request.RequestId, newToken);

                if (CheckCompletion(state)) completedIds.Add(state.Request.RequestId);
            }
            catch (Exception ex)
            {
                _batcher.MarkFailed(state.Request.RequestId, ex);
                completedIds.Add(state.Request.RequestId);
            }
        }

        foreach (var id in completedIds)
        {
            var state = _batcher.GetState(id);
            if (state?.Handle != null) KVCache.ReleaseRequest(state.Handle);

            _batcher.MarkCompleted(id);
        }
    }

    #endregion

    #region 字段

    private readonly Func<ArrayND, ArrayND> _forwardFn;
    private readonly ContinuousBatcher _batcher;
    private readonly int _eosTokenId;
    private int _nextRequestId;

    #endregion

    #region 属性

    /// <summary>
    ///     当前活跃请求数
    /// </summary>
    public int ActiveRequestCount => _batcher.ActiveRequestCount;

    /// <summary>
    ///     最大批次大小
    /// </summary>
    public int MaxBatchSize { get; }

    /// <summary>
    ///     词表大小
    /// </summary>
    public int VocabSize { get; }

    /// <summary>
    ///     是否正在运行
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    ///     PagedKVCache 实例
    /// </summary>
    public PagedKVCache KVCache { get; }

    #endregion

    #region 公开方法

    /// <summary>
    ///     提交推理请求并返回结果等待句柄
    ///     调用方可通过 await 获取完整输出 token 序列
    /// </summary>
    /// <param name="promptTokenIds">输入 prompt 的 token ID 序列</param>
    /// <param name="maxNewTokens">最大新生成 token 数</param>
    /// <param name="temperature">采样温度</param>
    /// <param name="topK">Top-k 采样参数</param>
    /// <param name="topP">Top-p 采样参数</param>
    /// <param name="priority">请求优先级</param>
    /// <param name="isStreaming">是否启用流式输出</param>
    /// <param name="onTokenGenerated">流式回调</param>
    /// <returns>结果等待句柄</returns>
    public Task<int[]> SubmitAsync(
        int[] promptTokenIds,
        int maxNewTokens = 128,
        float temperature = 0.8f,
        int topK = 0,
        float topP = 1.0f,
        int priority = 0,
        bool isStreaming = false,
        Action<int>? onTokenGenerated = null)
    {
        var requestId = Interlocked.Increment(ref _nextRequestId);

        var request = new InferenceRequest(
            requestId,
            promptTokenIds,
            maxNewTokens,
            temperature,
            topK,
            topP,
            priority,
            isStreaming,
            onTokenGenerated);

        _batcher.SubmitRequest(request);

        return request.CompletionSource.Task;
    }

    /// <summary>
    ///     主推理循环 —— 持续运行直到取消
    ///     每个迭代：组装批次 → 预填充新请求 → 解码 → 采样 → 更新状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = true;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var batchState = _batcher.ComposeBatch();

                if (batchState.IsEmpty)
                {
                    await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                ProcessPrefillRequests(batchState);
                ProcessDecodeRequests(batchState, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <summary>
    ///     执行单步推理迭代（用于外部驱动循环）
    /// </summary>
    /// <returns>是否处理了任何请求</returns>
    public bool Step()
    {
        var batchState = _batcher.ComposeBatch();

        if (batchState.IsEmpty) return false;

        ProcessPrefillRequests(batchState);
        ProcessDecodeRequests(batchState);

        return true;
    }

    #endregion

    #region 预填充处理

    private void ProcessPrefillRequests(BatchState batchState)
    {
        foreach (var state in batchState.PrefillRequests)
            try
            {
                PrefillRequest(state);
            }
            catch (Exception ex)
            {
                _batcher.MarkFailed(state.Request.RequestId, ex);
            }
    }

    /// <summary>
    ///     预填充单个请求：一次性处理所有 prompt token
    ///     将 prompt 的 KV Cache 写入 PagedKVCache，获取最后一个 token 的 logits
    /// </summary>
    /// <param name="state">批次请求状态</param>
    private void PrefillRequest(BatchRequestState state)
    {
        var request = state.Request;
        var promptLen = request.PromptTokenIds.Length;

        var maxSeqLen = promptLen + request.MaxNewTokens;
        var handle = KVCache.CreateRequest(maxSeqLen);
        state.Handle = handle;

        var inputIds = ArrayND.Zeros(1, promptLen);
        var spanInput = inputIds.AsWriteSpan();
        for (var i = 0; i < promptLen; i++) spanInput[i] = request.PromptTokenIds[i];

        var logits = _forwardFn(inputIds);

        var lastTokenLogits = ExtractLastTokenLogits(logits, promptLen);
        var newToken = SampleFromLogits(lastTokenLogits, request);

        _batcher.RecordTokenGenerated(request.RequestId, newToken);
        _batcher.RecordPrefillCompleted(request.RequestId, promptLen);

        CheckCompletion(state);
    }

    #endregion

    #region 采样与完成检测

    /// <summary>
    ///     从 logits 中采样 token
    /// </summary>
    /// <param name="logits">logits [vocabSize]</param>
    /// <param name="request">推理请求（获取采样参数）</param>
    /// <returns>采样的 token ID</returns>
    private int SampleFromLogits(float[] logits, InferenceRequest request)
    {
        var logitsArray = ArrayND.FromArray(logits, 1, logits.Length);

        var sampled = Samplers.GenerateToken(
            logitsArray,
            request.Temperature,
            request.TopK,
            request.TopP);

        return (int)sampled.AsSpan()[0];
    }

    /// <summary>
    ///     检查请求是否完成（达到最大 token 数或遇到 EOS）
    /// </summary>
    /// <param name="state">批次请求状态</param>
    /// <returns>是否已完成</returns>
    private bool CheckCompletion(BatchRequestState state)
    {
        var request = state.Request;

        if (state.GeneratedCount >= request.MaxNewTokens)
        {
            request.Complete();
            return true;
        }

        if (_eosTokenId >= 0 && request.GeneratedTokenIds.Count > 0)
        {
            var lastToken = request.GeneratedTokenIds[^1];
            if (lastToken == _eosTokenId)
            {
                request.Complete();
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Logits 提取

    /// <summary>
    ///     从 3D logits 中提取最后一个 token 位置的 logits
    /// </summary>
    /// <param name="logits">模型输出 logits [1, seqLen, vocabSize]</param>
    /// <param name="seqLen">序列长度</param>
    /// <returns>最后一个 token 的 logits [vocabSize]</returns>
    private float[] ExtractLastTokenLogits(ArrayND logits, int seqLen)
    {
        var vocabSize = logits.Shape[^1];
        var span = logits.AsSpan();
        var result = new float[vocabSize];

        var offset = (seqLen - 1) * vocabSize;
        for (var i = 0; i < vocabSize; i++) result[i] = span[offset + i];

        return result;
    }

    /// <summary>
    ///     从批次 logits 中提取指定请求的 logits
    /// </summary>
    /// <param name="logits">模型输出 logits [batch, 1, vocabSize]</param>
    /// <param name="batchIdx">批次中的请求索引</param>
    /// <returns>该请求的 logits [vocabSize]</returns>
    private float[] ExtractBatchItemLogits(ArrayND logits, int batchIdx)
    {
        var vocabSize = logits.Shape[^1];
        var span = logits.AsSpan();
        var result = new float[vocabSize];

        var offset = batchIdx * vocabSize;
        for (var i = 0; i < vocabSize; i++) result[i] = span[offset + i];

        return result;
    }

    #endregion
}