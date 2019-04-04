using System;

namespace Core.Compiler;

/// <summary>
///     标记类需要自动生成词法分析器
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class GenerateLexerAttribute : Attribute
{
}