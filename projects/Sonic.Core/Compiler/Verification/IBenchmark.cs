using System;

namespace Core.Compiler.Verification;

/// <summary>
///     基准测试接口，定义基准测试的契约
/// </summary>
public interface IBenchmark
{
    /// <summary>
    ///     获取已消耗的时间
    /// </summary>
    TimeSpan elapsed { get; }

    /// <summary>
    ///     运行基准测试
    /// </summary>
    void run();
}