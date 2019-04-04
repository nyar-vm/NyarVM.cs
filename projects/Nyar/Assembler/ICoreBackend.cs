namespace Nyar.Assembler;

/// <summary>
///     Core 方言代码生成后端标记接口。
///     Core 方言适用于没有 GC 的原生平台（Native）。
///     实现此后端的平台不需要 GC 支持，适用于 bare-metal 或系统级编译。
/// </summary>
/// <typeparam name="TTarget">目标平台数据结构类型</typeparam>
public interface ICoreBackend<TTarget> : ICodeGenBackend<TTarget>
{
}