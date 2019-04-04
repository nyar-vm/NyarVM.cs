using Core.Hardware;

namespace Std.Hardware;

/// <summary>
///     设备实现，描述硬件设备的名称和输入输出能力
/// </summary>
public sealed class Device : IDevice
{
    /// <summary>
    ///     初始化设备
    /// </summary>
    /// <param name="name">设备名称</param>
    /// <param name="inputCapabilities">输入能力集合</param>
    /// <param name="outputCapabilities">输出能力集合</param>
    public Device(string name, IReadOnlySet<InputCapability>? inputCapabilities = null,
        IReadOnlySet<OutputCapability>? outputCapabilities = null)
    {
        this.name = name;
        input_capabilities = inputCapabilities ?? new HashSet<InputCapability>();
        output_capabilities = outputCapabilities ?? new HashSet<OutputCapability>();
    }

    /// <summary>
    ///     设备名称
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     输入能力集合
    /// </summary>
    public IReadOnlySet<InputCapability> input_capabilities { get; }

    /// <summary>
    ///     输出能力集合
    /// </summary>
    public IReadOnlySet<OutputCapability> output_capabilities { get; }
}