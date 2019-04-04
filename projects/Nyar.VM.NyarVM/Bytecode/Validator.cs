using Nyar.Types;
using Std.Data.Binary.NyarIR.Validate;

namespace Nyar.VM.NyarVM.Bytecode;

/// <summary>
///     代码字节流验证器。
///     它把运行时模块转换为二进制数据模型，再委托 `NyarValidator` 验证模块代码字节流。
/// </summary>
public sealed class BytecodeValidator
{
    private readonly NyarValidator _inner = new();

    /// <summary>
    ///     验证模块代码字节流的安全性。
    /// </summary>
    /// <param name="module">要验证的模块。</param>
    /// <param name="codeBytes">编码后的模块代码字节流。</param>
    /// <param name="diagnostics">验证诊断信息。</param>
    /// <returns>验证是否通过。</returns>
    public bool validate(NyarModule module, byte[] codeBytes, out List<string> diagnostics)
    {
        var moduleData = NyarModuleConverter.to_module_data(module);
        return _inner.validate(moduleData, codeBytes, out diagnostics);
    }
}
