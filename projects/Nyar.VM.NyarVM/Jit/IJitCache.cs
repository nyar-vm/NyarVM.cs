namespace Nyar.VM.NyarVM.Jit;

/// <summary>
///     JIT 编译结果缓存接口
///     用于持久化热点方法的编译信息，避免 VM 重启后重复识别热点
///     实际 LightDB 实现将在 M3（2026-08）提供
/// </summary>
public interface IJitCache
{
    /// <summary>
    ///     记录已编译的热点函数（在编译成功后调用）
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <param name="functionIndex">函数索引。</param>
    void record_compilation(string moduleName, int functionIndex);

    /// <summary>
    ///     检查函数是否曾被编译（用于 VM 启动时加速首次编译决策）
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <param name="functionIndex">函数索引。</param>
    /// <returns>是否曾在之前会话中被编译。</returns>
    bool was_compiled_previously(string moduleName, int functionIndex);

    /// <summary>
    ///     获取模块的热点函数列表（用于启动时预热）
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <returns>历史上被编译过的函数索引列表。</returns>
    IReadOnlyList<int> get_previously_compiled_functions(string moduleName);

    /// <summary>
    ///     使指定模块的所有编译记录失效
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    void invalidate(string moduleName);

    /// <summary>
    ///     使指定函数的编译记录失效
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <param name="functionIndex">函数索引。</param>
    void invalidate(string moduleName, int functionIndex);
}