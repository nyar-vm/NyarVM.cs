using Std.Data.Text.Syntax;

namespace Std.Data.Text.Javap;

/// <summary>
///     javap -c 反汇编输出
/// </summary>
/// <param name="source_file">源文件名。</param>
/// <param name="class_decl">类声明。</param>
/// <param name="span">源码位置。</param>
public sealed record JvpDisassembly(string? source_file, JvpClassDeclaration class_decl, TextSpan span = default)
    : JvpAstNode(span);