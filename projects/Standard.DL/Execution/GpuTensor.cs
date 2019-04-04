using Std.DL.Flux;

namespace Std.DL.Execution;

/// <summary>
///     GPU 张量 —— 模拟 CUDA 显存上的张量分配和生命周期
/// </summary>
public sealed class GpuTensor : IDisposable
{
    private bool _disposed;

    /// <summary>
    ///     创建 GPU 张量
    /// </summary>
    /// <param name="shape">形状</param>
    public GpuTensor(int[] shape)
    {
        Shape = (int[])shape.Clone();
        Size = Shape.Aggregate(1, (a, b) => a * b);
        MemoryBytes = Size * sizeof(float);
        CpuData = ArrayND.Zeros(shape);
    }

    /// <summary>
    ///     创建 GPU 张量（从现有数据）
    /// </summary>
    /// <param name="data">源数据</param>
    public GpuTensor(ArrayND data)
    {
        Shape = (int[])data.Shape.Clone();
        Size = data.Size;
        MemoryBytes = Size * sizeof(float);
        CpuData = data.Clone();
    }

    /// <summary>
    ///     创建 GPU 张量（从现有 GpuTensor 的数据）
    /// </summary>
    /// <param name="data">源 GPU 张量</param>
    /// <param name="shape">新形状</param>
    public GpuTensor(GpuTensor data, int[] shape)
    {
        Shape = (int[])shape.Clone();
        Size = Shape.Aggregate(1, (a, b) => a * b);
        MemoryBytes = Size * sizeof(float);
        CpuData = data.CpuData.Reshape(shape);
    }

    /// <summary>张量形状</summary>
    public int[] Shape { get; }

    /// <summary>总元素数</summary>
    public int Size { get; }

    /// <summary>占用显存字节数</summary>
    public long MemoryBytes { get; }

    /// <summary>GpuTensor 唯一标识</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>关联的 CPU 副本（用于 Mock 实现中的实际计算）</summary>
    internal ArrayND CpuData { get; }

    /// <summary>
    ///     释放 GPU 显存
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     析构函数
    /// </summary>
    ~GpuTensor()
    {
        Dispose(false);
    }

    /// <summary>
    ///     获取 Span（CPU 端数据——仅用于 Mock 模式）
    /// </summary>
    public ReadOnlySpan<float> AsSpan()
    {
        return CpuData.AsSpan();
    }

    /// <summary>
    ///     获取可写 Span（CPU 端数据——仅用于 Mock 模式）
    /// </summary>
    public Span<float> AsWriteSpan()
    {
        return CpuData.AsWriteSpan();
    }

    /// <summary>
    ///     拷贝到目标 Span
    /// </summary>
    public void CopyTo(Span<float> destination)
    {
        CpuData.AsSpan().CopyTo(destination);
    }

    /// <summary>
    ///     从源 Span 拷贝数据到 GPU 张量
    /// </summary>
    public void CopyFrom(ReadOnlySpan<float> source)
    {
        source.CopyTo(CpuData.AsWriteSpan());
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    /// <param name="disposing">是否由 Dispose 调用</param>
    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        GpuMemoryTracker.Unregister(this);

        if (disposing) CpuData.Dispose();

        _disposed = true;
    }
}