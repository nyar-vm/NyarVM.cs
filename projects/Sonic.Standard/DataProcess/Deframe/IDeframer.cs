namespace Std.DataProcess.Deframe;

/// <summary>
///     解帧器：从字节流提取帧载荷�?/// 解帧器是有状态的，内部维护缓冲区以处理粘�?半包�?/// 先调�?<see cref="try_get_next_frame" /> 检查是否有完整帧，再通过
///     <see cref="current_frame" /> 获取载荷数据�?/// <see cref="current_frame" /> 返回�?<see cref="ReadOnlySpan{T}" /> 在下�?
///     <see cref="feed" /> �?<see cref="reset" /> 前有效�?///
/// </summary>
public interface IDeframer : IDisposable
{
    /// <summary>
    ///     当前帧的载荷数据。仅�?<see cref="try_get_next_frame" /> 返回 <c>true</c> 后有效�?    /// 数据指向内部缓冲区，在下�?<see cref="feed" /> �?
    ///     <see cref="reset" /> 前有效�?    ///
    /// </summary>
    ReadOnlySpan<byte> current_frame { get; }

    /// <summary>
    ///     喂入新收到的数据，内部累积�?    ///
    /// </summary>
    /// <param name="data">新收到的字节数据�?/param>
    void feed(ReadOnlySpan<byte> data);

    /// <summary>
    ///     尝试获取一个完整帧的载荷。若无足够数据返�?false�?    /// 成功后可访问 <see cref="current_frame" /> 获取载荷数据�?    ///
    /// </summary>
    /// <returns>成功获取帧返�?<c>true</c>，数据不足返�?<c>false</c>�?/returns>
    bool try_get_next_frame();

    /// <summary>
    ///     异步等待下一帧可�?    ///
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果帧可用返�?true</returns>
    ValueTask<bool> wait_for_frame(CancellationToken cancellationToken = default);

    /// <summary>
    ///     重置内部状态，丢弃未处理数�?    ///
    /// </summary>
    void reset();
}