namespace Galatea.Tests;

/// <summary>
///     GPU 内存相关测试集合 —— 强制串行执行以防止静态 GpuMemoryTracker 竞态
/// </summary>
[CollectionDefinition("GpuMemoryTests", DisableParallelization = true)]
public sealed class GpuMemoryTestCollection
{
}