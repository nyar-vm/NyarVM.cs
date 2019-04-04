using System;

namespace Core.Math.Tensor;

/// <summary>
///     指定计算设备名称的特性。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DeviceAttribute : Attribute
{
    /// <summary>
    ///     初始化 <see cref="DeviceAttribute" /> 类的新实例。
    /// </summary>
    /// <param name="deviceName">设备名称。</param>
    public DeviceAttribute(string deviceName)
    {
        device_name = deviceName;
    }

    /// <summary>
    ///     获取设备名称。
    /// </summary>
    public string device_name { get; }
}