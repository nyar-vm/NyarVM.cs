using Nyar.Language.Valkyrie.Compiler.Hir._Ref;

namespace Nyar.Language.Valkyrie.Compiler.Hir._Def;

/// <summary>
///     表示枚举、标志或联合的变体定义。
/// </summary>
public sealed record HirVariantDef
{
    /// <summary>
    ///     变体名称。
    /// </summary>
    public string name { get; init; }

    public HirTypeRef? payload_type { get; init; }

    /// <summary>
    ///     变体字段列表；仅 `unite/union` 的具名字段载荷会保留。
    /// </summary>
    public IReadOnlyList<HirFieldDef> fields { get; init; }

    /// <summary>
    ///     整数判别值；`Enums/Flags` 使用显式值或自动递增，`Unite/Union` 使用自动递增或 <c>[tag(N)]</c> 显式指定。
    /// </summary>
    public long? discriminant { get; init; }

    /// <summary>
    ///     表示枚举、标志或联合的变体定义。
    /// </summary>
    /// <param name="name">变体名称</param>
    /// <param name="payload_type">变体携带的数据类型引用，无载荷时为 `null`</param>
    /// <param name="fields">变体字段列表；仅 `unite/union` 的具名字段载荷会保留</param>
    /// <param name="discriminant">整数判别值；`Enums/Flags` 使用显式值或自动递增，`Unite/Union` 使用自动递增或 <c>[tag(N)]</c> 显式指定</param>
    public HirVariantDef(string name,
        HirTypeRef? payload_type,
        IReadOnlyList<HirFieldDef> fields,
        long? discriminant)
    {
        this.name = name;
        this.payload_type = payload_type;
        this.fields = fields;
        this.discriminant = discriminant;
    }

    public void deconstruct(out string name, out HirTypeRef? payloadType, out IReadOnlyList<HirFieldDef> fields, out long? discriminant)
    {
        name = this.name;
        payloadType = this.payload_type;
        fields = this.fields;
        discriminant = this.discriminant;
    }
}
