namespace Core.Math.Tensor;

/// <summary>
///     计算设备接口，描述执行张量运算的硬件设备。
/// </summary>
public interface IComputeDevice
{
    /// <summary>
    ///     获取设备名称。
    /// </summary>
    string name { get; }

    /// <summary>
    ///     获取设备类型。
    /// </summary>
    DeviceType type { get; }
}