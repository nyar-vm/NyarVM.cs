namespace Nyar.PackageManager.Build;

/// <summary>
///     运行契约 — 描述如何运行编译产物
/// </summary>
public interface IRunContract
{
    /// <summary>
    ///     逻辑入口名称
    /// </summary>
    string logical_entry { get; }

    /// <summary>
    ///     物理入口文件路径
    /// </summary>
    string physical_entry { get; }

    /// <summary>
    ///     调用形态
    /// </summary>
    string invocation_shape { get; }

    /// <summary>
    ///     验证命令
    /// </summary>
    string validation_command { get; }
}