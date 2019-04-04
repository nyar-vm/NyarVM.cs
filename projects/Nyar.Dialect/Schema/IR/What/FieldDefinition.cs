using Nyar.Dialect.Schema.IR.Common;
using Nyar.Dialect.Schema.IR.Types;

namespace Nyar.Dialect.Schema.IR.What;

public sealed class FieldDefinition
{
    public FieldDefinition(string name, SchemaType fieldType, bool isOptional = false, object? defaultValue = null,
        IReadOnlyList<AttributeDefinition>? attributes = null, string? docComment = null,
        IReadOnlyList<string>? dtoGroups = null,
        int sourceLine = 0, int sourceColumn = 0)
    {
        this.name = name;
        field_type = fieldType;
        is_optional = isOptional;
        default_value = defaultValue;
        this.attributes = attributes ?? [];
        doc_comment = docComment;
        dto_groups = dtoGroups ?? extract_dto_groups(attributes);
        source_line = sourceLine;
        source_column = sourceColumn;
    }

    public string name { get; }
    public SchemaType field_type { get; }
    public bool is_optional { get; }
    public object? default_value { get; }
    public IReadOnlyList<AttributeDefinition> attributes { get; }
    public string? doc_comment { get; }
    public int source_line { get; }
    public int source_column { get; }

    /// <summary>
    ///     字段所属的 DTO 组列表（来自 [dto(group)] 属性）
    /// </summary>
    public IReadOnlyList<string> dto_groups { get; }

    public bool is_primary_key => attributes.Any(a => a.name is "key" or "Key");
    public bool is_unique_key => attributes.Any(a => a.name is "unique" or "Unique");
    public bool is_foreign_key => attributes.Any(a => a.name is "ref" or "fk" or "Ref" or "Fk");
    public bool is_flatten => attributes.Any(a => a.name is "flatten" or "Flatten");

    /// <summary>
    ///     从属性列表中提取 dto 组名
    /// </summary>
    private static List<string> extract_dto_groups(IReadOnlyList<AttributeDefinition>? attributes)
    {
        var groups = new List<string>();
        if (attributes is null) return groups;

        foreach (var attr in attributes)
            if (attr.name is "dto" or "Dto")
                foreach (var arg in attr.arguments)
                {
                    var groupName = !string.IsNullOrEmpty(arg.Value) ? arg.Value : arg.Key;
                    if (!string.IsNullOrEmpty(groupName)) groups.Add(groupName);
                }

        return groups;
    }
}