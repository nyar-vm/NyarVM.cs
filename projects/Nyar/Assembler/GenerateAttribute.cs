namespace Nyar.Assembler;

/// <summary>
///     元编译函数属性（如 [clr("System.Console", "System.Console", "Write")]）。
/// </summary>
public sealed record GenerateAttribute(string name, IReadOnlyList<string> arguments);