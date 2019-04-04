using System;

namespace Core.Marker;

/// <summary>
///     标记仅供 Sonic.Standard 内部使用的类型，生成器应忽略这些类型以避免误暴露。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
public sealed class SonicInternalAttribute : Attribute
{
}