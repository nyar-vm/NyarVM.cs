using Std.Data.Text.Syntax;

namespace Std.Data.Text.Javap;

/// <summary>
///     Code 段
/// </summary>
/// <param name="instructions">指令列表。</param>
/// <param name="span">源码位置。</param>
public sealed record JvpCodeSection(List<JvpInstruction> instructions, TextSpan span = default) : JvpAstNode(span);