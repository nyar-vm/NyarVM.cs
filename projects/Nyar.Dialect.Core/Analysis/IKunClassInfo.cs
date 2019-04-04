using Nyar.IR.Intent;

namespace Nyar.Dialect.Core;

/// <summary>
///     Oa 类型分析结果
/// </summary>
/// <param name="type">类型标注。</param>
/// <param name="effects">效应集。</param>
public sealed record IKunClassInfo(TypeAnnotation type, EffectSet effects)
{
    /// <summary>
    ///     未知类型信息
    /// </summary>
    public static readonly IKunClassInfo unknown = new(TypeAnnotation.unknown, EffectSet.pure);
}