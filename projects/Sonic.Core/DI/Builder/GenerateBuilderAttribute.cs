using System;

namespace Core.DI.Builder;

/// <summary>
///     标记类或结构体以生成对应的构建器类型
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class GenerateBuilderAttribute : Attribute
{
}