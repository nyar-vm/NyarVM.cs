using System;

namespace Core.Media;

/// <summary>
///     硬件设备上下文接口，管理硬件加速设备的初始化与资源释放。
/// </summary>
public interface IDeviceContext : IDisposable
{
    /// <summary>
    ///     获取设备名称。
    /// </summary>
    string device_name { get; }

    /// <summary>
    ///     初始化硬件设备上下文。
    /// </summary>
    void initialize();
}