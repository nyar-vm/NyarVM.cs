using System;

namespace Core.Compiler.Token;

/// <summary>
///     标记枚举为词法单元类型，用于源生成器识别
/// </summary>
[AttributeUsage(AttributeTargets.Enum)]
public sealed class TokenAttribute : Attribute
{
}