using Nyar.Types;

namespace Nyar.VM.NyarVM.GC;

/// <summary>
///     GC Roots 提供者接口
///     实现此接口以向 GC 提供根引用集合
/// </summary>
public interface IGcRootProvider
{
    /// <summary>
    ///     获取所有 GC Root 值
    ///     包括值栈、帧局部变量、全局变量、闭包上值等
    /// </summary>
    /// <returns>GC Root 值的枚举。</returns>
    IEnumerable<Value> get_root_values();
}