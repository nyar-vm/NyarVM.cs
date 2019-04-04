using System;

namespace Core.Collection.ADT;

/// <summary>
///     标记类型为模式匹配生成目标，用于编译器自动生成匹配逻辑
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class GenerateMatchAttribute : Attribute
{
}