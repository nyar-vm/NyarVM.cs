namespace Nyar.Assembler;

/// <summary>
///     代码生成阶段的标签标记。
/// </summary>
/// <param name="name">标签名。</param>
/// <param name="InstructionIndex">标签绑定的指令索引；表示跳转到该索引对应指令之前。</param>
public sealed record GenerateLabelMarker(string name, int instruction_index);