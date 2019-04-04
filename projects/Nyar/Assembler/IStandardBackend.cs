namespace Nyar.Assembler;

/// <summary>
///     Standard 方言代码生成后端标记接口。
///     Standard 方言适用于有 GC 的平台（NyarVM、WASM、JVM、CLR）。
///     实现此后端的平台必须支持 GC 语义。
/// </summary>
/// <typeparam name="TTarget">目标平台数据结构类型</typeparam>
public interface IStandardBackend<TTarget> : ICodeGenBackend<TTarget>
{
}