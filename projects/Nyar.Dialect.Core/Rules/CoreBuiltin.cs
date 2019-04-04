namespace Nyar.Dialect.Core;

/// <summary>
///     Core 方言内置函数 ID（映射到 Literal&lt;long&gt; 的 long 值）
///     ID 范围: 0x9101 ~ 0x91FF
/// </summary>
public enum CoreBuiltin : long
{
    vm_alloc = 0x9101,
    vm_free = 0x9102,
    vm_load = 0x9103,
    vm_store = 0x9104,
    vm_call = 0x9111,
    vm_branch = 0x9112,
    vm_phi = 0x9113,
    vm_perform = 0x9121,
    vm_handle = 0x9122
}