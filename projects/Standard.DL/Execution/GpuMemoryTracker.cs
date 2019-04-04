namespace Std.DL.Execution;

/// <summary>
///     GPU 显存追踪器 —— 模拟 CUDA 显存的分配、释放和使用率
/// </summary>
public static class GpuMemoryTracker
{
    /// <summary>GPU 总显存（模拟 8GB）</summary>
    public const long TotalMemoryBytes = 8L * 1024 * 1024 * 1024;

    private static readonly object Lock = new();
    private static readonly HashSet<Guid> ActiveTensors = [];

    /// <summary>
    ///     当前已分配显存字节数
    /// </summary>
    public static long AllocatedBytes { get; private set; }

    /// <summary>
    ///     峰值显存使用字节数
    /// </summary>
    public static long PeakBytes { get; private set; }

    /// <summary>
    ///     已分配的张量数量
    /// </summary>
    public static int ActiveTensorCount
    {
        get
        {
            lock (Lock)
            {
                return ActiveTensors.Count;
            }
        }
    }

    /// <summary>
    ///     获取显存利用率（0.0-1.0）
    /// </summary>
    public static float MemoryUtilization => TotalMemoryBytes > 0
        ? (float)AllocatedBytes / TotalMemoryBytes
        : 0.0f;

    /// <summary>
    ///     检查是否有足够显存
    /// </summary>
    /// <param name="requestedBytes">请求的字节数</param>
    /// <returns>是否有足够空间</returns>
    public static bool HasEnoughMemory(long requestedBytes)
    {
        lock (Lock)
        {
            return AllocatedBytes + requestedBytes <= TotalMemoryBytes;
        }
    }

    /// <summary>
    ///     注册 GPU 张量（模拟显存分配），OOM 时抛出异常
    /// </summary>
    /// <param name="tensor">GPU 张量</param>
    /// <exception cref="OutOfMemoryException">显存不足时抛出</exception>
    public static void Register(GpuTensor tensor)
    {
        lock (Lock)
        {
            if (AllocatedBytes + tensor.MemoryBytes > TotalMemoryBytes)
                throw new OutOfMemoryException(
                    $"GPU 显存不足：需要 {tensor.MemoryBytes} 字节，" +
                    $"当前已用 {AllocatedBytes} 字节，总量 {TotalMemoryBytes} 字节（{TotalMemoryBytes / 1024 / 1024 / 1024}GB）");

            ActiveTensors.Add(tensor.Id);
            AllocatedBytes += tensor.MemoryBytes;

            if (AllocatedBytes > PeakBytes) PeakBytes = AllocatedBytes;
        }
    }

    /// <summary>
    ///     注销 GPU 张量（模拟显存释放）
    /// </summary>
    /// <param name="tensor">GPU 张量</param>
    public static void Unregister(GpuTensor tensor)
    {
        lock (Lock)
        {
            if (ActiveTensors.Remove(tensor.Id))
            {
                AllocatedBytes -= tensor.MemoryBytes;
                if (AllocatedBytes < 0) AllocatedBytes = 0;
            }
        }
    }

    /// <summary>
    ///     重置显存统计
    /// </summary>
    public static void Reset()
    {
        lock (Lock)
        {
            ActiveTensors.Clear();
            AllocatedBytes = 0;
            PeakBytes = 0;
        }
    }
}