using Nyar.Assembler;
using Nyar.Types.Targets;
using Nyar.VM.NyarVM.Bytecode;
using Std.Data.Text.Diagnostics;

namespace Nyar.VM.NyarVM;

/// <summary>
///     `NyarVM` 后端，将 `GenerateModule` 发射为 `.nyar` 模块字节流。
/// </summary>
public sealed class NyarVmBackend : IStandardBackend<byte[]>
{
    /// <inheritdoc />
    public string name => "NyarVM";

    /// <inheritdoc />
    public IReadOnlyList<TargetArch> supported_archs => [TargetArch.nyar_vm];

    /// <inheritdoc />
    public bool validate(GenerateModule module, out List<Diagnostic> diagnostics)
    {
        diagnostics = [];

        if (module.has_witness_dispatch)
        {
            diagnostics.Add(new Diagnostic(
                default,
                "`NyarVM` 后端尚未接通 witness 值传递与运行时选择，当前不能直接发射包含 witness 分派的模块。",
                DiagnosticSeverity.error));
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public OutputSpec<byte[]> compile(GenerateModule module, CompilationOptions options)
    {
        var runtimeModule = CompilationUnitSerializer.serialize(module);
        var encoded = NyarModuleConverter.encode(runtimeModule);

        return new OutputSpec<byte[]>
        {
            data = encoded,
            file_extension = ".nyar",
            media_type = "application/octet-stream"
        };
    }

    /// <inheritdoc />
    OutputSpec ICodeGenBackend.compile(GenerateModule module, CompilationOptions options)
    {
        return compile(module, options);
    }
}