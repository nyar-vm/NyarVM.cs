namespace Core.Math.Tensor;

/// <summary>
///     计算设备类型枚举。
/// </summary>
public enum DeviceType
{
    /// <summary>
    ///     中央处理器。
    /// </summary>
    cpu,

    /// <summary>
    ///     NVIDIA CUDA 并行计算平台。
    /// </summary>
    cuda,

    /// <summary>
    ///     OpenCL 异构计算框架。
    /// </summary>
    open_cl,

    /// <summary>
    ///     Apple Metal 图形与计算框架。
    /// </summary>
    metal,

    /// <summary>
    ///     Vulkan 图形与计算 API。
    /// </summary>
    vulkan,

    /// <summary>
    ///     张量处理单元。
    /// </summary>
    tpu
}