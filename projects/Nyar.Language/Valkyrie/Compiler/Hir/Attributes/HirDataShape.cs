namespace Nyar.Language.Valkyrie.Compiler.Hir.Attributes;

/// <summary>
///     `[data]` 类型在 HIR 中的归一化数据形状。
/// </summary>
public sealed record HirDataShape
{
    public HirDataContainerKind container_kind { get; init; }
    public IReadOnlyList<HirDataField> fields { get; init; }

    
    /// <summary>
    ///     `[data]` 类型在 HIR 中的归一化数据形状。
    /// </summary>
    public HirDataShape(HirDataContainerKind container_kind, IReadOnlyList<HirDataField> fields)
    {
        this.container_kind = container_kind;
        this.fields = fields;
    }

    /// <summary>
    ///     枚举参与序列化的字段，忽略显式 `[ignore]` 字段。
    /// </summary>
    public IReadOnlyList<HirDataField> enumerate_serializable_fields()
    {
        return
        [
            .. fields
                .Select((field, index) => new { field, index })
                .Where(item => !item.field.ignore)
                .OrderBy(item => item.field.order ?? int.MaxValue)
                .ThenBy(item => item.index)
                .Select(item => item.field)
        ];
    }

    /// <summary>
    ///     按主绑定名或别名解析反序列化目标字段。
    /// </summary>
    public HirDataField? find_deserializable_field(string fieldName)
    {
        return enumerate_serializable_fields()
            .FirstOrDefault(field => field.matches_deserializable_name(fieldName));
    }



}