using System;

namespace Core.DI;

/// <summary>
///     标记类以生成对应的工厂类型
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class GenerateFactoryAttribute : Attribute
{
}