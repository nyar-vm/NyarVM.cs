using Microsoft.CodeAnalysis;

namespace Sonic.Data.Generator;

/// <summary>
///     `DataSerializeGenerator` 的最小占位实现。
///     当前仅保证源生成器程序集可编译，避免损坏的生成逻辑阻塞 `legion` 构建链。
/// </summary>
[Generator]
public sealed class DataSerializeGenerator : IIncrementalGenerator
{
    /// <summary>
    ///     初始化增量生成器。
    /// </summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 当前不注册任何输出。
    }
}
