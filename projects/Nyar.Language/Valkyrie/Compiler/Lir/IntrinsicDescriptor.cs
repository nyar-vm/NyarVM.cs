using Std.Data.Binary.NyarIR.Data;
using GenerateValueType = Nyar.Assembler.GenerateValueType;

namespace Nyar.Language.Valkyrie.Compiler.Lir;

/// <summary>
///     intrinsic 的共享降级描述。
/// </summary>
internal sealed record IntrinsicDescriptor(
    string name,
    int argument_count,
    GenerateValueType result_type,
    IntrinsicLoweringKind lowering_kind,
    NyarHeadCode head_code = NyarHeadCode.nop,
    string? static_call_target = null);
