namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     一个虚拟的用于标记元属性的节点
/// </summary>
/// <para>支持泛型：</para>
/// <code>
/// /// document line1
/// /// document line2
/// [attr1, attr2]
/// [attr3()]
/// mod1 mod2 class ClassName
/// {
/// 
/// }
/// </code>
public sealed record Annotations
{
    /// <summary>
    ///     文档注释列表
    /// </summary>
    public IReadOnlyList<DocumentComment> documents { get; init; } = [];

    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<AttributeList> attribute_lists { get; init; } = [];

    /// <summary>
    ///     修饰符列表（如 <c>public</c>、<c>abstract</c>）
    /// </summary>
    public IReadOnlyList<IdentifierNode> modifiers { get; init; } = [];

    /// <summary>
    ///     扁平化后的属性项列表
    /// </summary>
    public IReadOnlyList<AttributeItem> attributes()
    {
        if (attribute_lists.Count == 0) return [];

        var items = new List<AttributeItem>();
        foreach (var attributeList in attribute_lists)
            if (attributeList.items.Count > 0)
                items.AddRange(attributeList.items);

        return items;
    }

    public IReadOnlyList<string> modifier_texts()
    {
        if (modifiers.Count == 0) return [];

        var names = new List<string>(modifiers.Count);
        foreach (var modifier in modifiers) names.Add(modifier.name);

        return names;
    }


    public string document_text()
    {
        return string.Join(Environment.NewLine, documents);
    }
}