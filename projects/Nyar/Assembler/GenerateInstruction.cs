using Std.Data.Binary.NyarIR.Data;

namespace Nyar.Assembler;

/// <summary>
///     代码生成中间表示的指令
///     使用 NyarHeadCode 枚举替代 int Opcode，使用 GenerateOperand 判别联合替代 Operand
/// </summary>
public sealed record GenerateInstruction
{
    /// <summary>
    ///     创建指令
    /// </summary>
    /// <param name="head_code">操作码</param>
    /// <param name="operands">操作数列表</param>
    public GenerateInstruction(NyarHeadCode head_code, params GenerateOperand[] operands)
    {
        this.head_code = head_code;
        this.operands = operands;
    }

    /// <summary>
    ///     创建无操作数指令
    /// </summary>
    /// <param name="head_code">操作码</param>
    public GenerateInstruction(NyarHeadCode head_code)
    {
        this.head_code = head_code;
        operands = [];
    }

    /// <summary>
    ///     指令操作码
    /// </summary>
    public NyarHeadCode head_code { get; init; }

    /// <summary>
    ///     指令操作码别名，等价于 <see cref="head_code" />
    /// </summary>
    public NyarHeadCode opcode => head_code;

    /// <summary>
    ///     指令操作数列表
    /// </summary>
    public IReadOnlyList<GenerateOperand> operands { get; init; } = [];
}