using System;

namespace Core.Compiler;

/// <summary>
///     标记类为语法定义，用于源生成器识别
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class GrammarAttribute : Attribute
{
}