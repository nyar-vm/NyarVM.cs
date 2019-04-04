using System.Collections.Generic;

namespace Core.Hardware;

/// <summary>
///     设备接口，描述硬件设备的名称和输入输出能力
/// </summary>
public interface IDevice
{
    /// <summary>
    ///     设备名称
    /// </summary>
    string name { get; }

    /// <summary>
    ///     输入能力集合
    /// </summary>
    IReadOnlySet<InputCapability> input_capabilities { get; }

    /// <summary>
    ///     输出能力集合
    /// </summary>
    IReadOnlySet<OutputCapability> output_capabilities { get; }
}