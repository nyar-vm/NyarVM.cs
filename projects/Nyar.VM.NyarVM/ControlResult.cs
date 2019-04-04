namespace Nyar.VM.NyarVM;

/// <summary>
///     控制流结果
/// </summary>
public enum ControlResult
{
    /// <summary>
    ///     继续执行
    /// </summary>
    @continue,

    /// <summary>
    ///     返回
    /// </summary>
    @return,

    /// <summary>
    ///     抛出异常
    /// </summary>
    @throw,

    /// <summary>
    ///     让出执行权
    /// </summary>
    yield,

    /// <summary>
    ///     断点或单步中断
    /// </summary>
    @break
}