namespace Std.DL.Flux.Inference;

/// <summary>
///     调度策略枚举
/// </summary>
public enum SchedulingStrategy
{
    /// <summary>
    ///     先来先服务（FCFS）
    /// </summary>
    FCFS,

    /// <summary>
    ///     优先级调度
    /// </summary>
    Priority
}

/// <summary>
///     推理调度器接口 —— 管理推理请求的排队和调度
///     负责从等待队列中选取下一批要处理的请求
/// </summary>
public interface IInferenceScheduler
{
    /// <summary>
    ///     当前活跃（正在处理）的请求数
    /// </summary>
    int ActiveCount { get; }

    /// <summary>
    ///     等待队列中的请求数
    /// </summary>
    int WaitingCount { get; }

    /// <summary>
    ///     提交推理请求到等待队列
    /// </summary>
    /// <param name="request">推理请求</param>
    void SubmitRequest(InferenceRequest request);

    /// <summary>
    ///     获取下一批待处理的请求
    /// </summary>
    /// <param name="maxCount">最大批次数</param>
    /// <returns>待处理的请求列表</returns>
    IReadOnlyList<InferenceRequest> GetNextBatch(int maxCount);

    /// <summary>
    ///     标记请求已完成，从活跃集合中移除
    /// </summary>
    /// <param name="requestId">请求 ID</param>
    void MarkCompleted(int requestId);
}

/// <summary>
///     先来先服务调度器 —— 按请求到达顺序依次调度
///     适用于公平性优先的场景
/// </summary>
public sealed class FcfsScheduler : IInferenceScheduler
{
    private readonly HashSet<int> _activeRequestIds;
    private readonly object _lock;
    private readonly Queue<InferenceRequest> _waitingQueue;

    /// <summary>
    ///     创建先来先服务调度器
    /// </summary>
    public FcfsScheduler()
    {
        _waitingQueue = new Queue<InferenceRequest>();
        _activeRequestIds = [];
        _lock = new object();
    }

    /// <summary>
    ///     当前活跃（正在处理）的请求数
    /// </summary>
    public int ActiveCount => _activeRequestIds.Count;

    /// <summary>
    ///     等待队列中的请求数
    /// </summary>
    public int WaitingCount => _waitingQueue.Count;

    /// <summary>
    ///     提交推理请求到等待队列
    /// </summary>
    /// <param name="request">推理请求</param>
    public void SubmitRequest(InferenceRequest request)
    {
        lock (_lock)
        {
            request.State = RequestState.Queued;
            _waitingQueue.Enqueue(request);
        }
    }

    /// <summary>
    ///     获取下一批待处理的请求（按到达顺序）
    /// </summary>
    /// <param name="maxCount">最大批次数</param>
    /// <returns>待处理的请求列表</returns>
    public IReadOnlyList<InferenceRequest> GetNextBatch(int maxCount)
    {
        lock (_lock)
        {
            var batch = new List<InferenceRequest>();

            while (batch.Count < maxCount && _waitingQueue.Count > 0)
            {
                var request = _waitingQueue.Dequeue();
                _activeRequestIds.Add(request.RequestId);
                batch.Add(request);
            }

            return batch;
        }
    }

    /// <summary>
    ///     标记请求已完成，从活跃集合中移除
    /// </summary>
    /// <param name="requestId">请求 ID</param>
    public void MarkCompleted(int requestId)
    {
        lock (_lock)
        {
            _activeRequestIds.Remove(requestId);
        }
    }
}

/// <summary>
///     优先级调度器 —— 按请求优先级从高到低调度
///     相同优先级的请求按到达顺序排列
///     适用于延迟敏感型请求需要优先处理的场景
/// </summary>
public sealed class PriorityScheduler : IInferenceScheduler
{
    private readonly HashSet<int> _activeRequestIds;
    private readonly object _lock;
    private readonly List<InferenceRequest> _waitingList;

    /// <summary>
    ///     创建优先级调度器
    /// </summary>
    public PriorityScheduler()
    {
        _waitingList = [];
        _activeRequestIds = [];
        _lock = new object();
    }

    /// <summary>
    ///     当前活跃（正在处理）的请求数
    /// </summary>
    public int ActiveCount => _activeRequestIds.Count;

    /// <summary>
    ///     等待队列中的请求数
    /// </summary>
    public int WaitingCount => _waitingList.Count;

    /// <summary>
    ///     提交推理请求到等待列表
    /// </summary>
    /// <param name="request">推理请求</param>
    public void SubmitRequest(InferenceRequest request)
    {
        lock (_lock)
        {
            request.State = RequestState.Queued;
            _waitingList.Add(request);
        }
    }

    /// <summary>
    ///     获取下一批待处理的请求（按优先级从高到低）
    /// </summary>
    /// <param name="maxCount">最大批次数</param>
    /// <returns>待处理的请求列表</returns>
    public IReadOnlyList<InferenceRequest> GetNextBatch(int maxCount)
    {
        lock (_lock)
        {
            _waitingList.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            var batch = new List<InferenceRequest>();
            var takeCount = System.Math.Min(maxCount, _waitingList.Count);

            for (var i = 0; i < takeCount; i++)
            {
                var request = _waitingList[i];
                _activeRequestIds.Add(request.RequestId);
                batch.Add(request);
            }

            _waitingList.RemoveRange(0, takeCount);

            return batch;
        }
    }

    /// <summary>
    ///     标记请求已完成，从活跃集合中移除
    /// </summary>
    /// <param name="requestId">请求 ID</param>
    public void MarkCompleted(int requestId)
    {
        lock (_lock)
        {
            _activeRequestIds.Remove(requestId);
        }
    }
}