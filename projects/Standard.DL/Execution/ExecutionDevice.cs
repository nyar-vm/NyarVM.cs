namespace Std.DL.Execution;

/// <summary>执行设备类型</summary>
public enum ExecutionDevice
{
    /// <summary>CPU</summary>
    Cpu,

    /// <summary>CUDA GPU</summary>
    Cuda,

    /// <summary>OpenCL</summary>
    OpenCL,

    /// <summary>Metal</summary>
    Metal,

    /// <summary>Vulkan</summary>
    Vulkan
}