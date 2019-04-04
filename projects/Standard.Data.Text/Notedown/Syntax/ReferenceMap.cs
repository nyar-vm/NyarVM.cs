namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     双向引用映射，记录文档中所有正向引用和反向引用
///     正向：引用者路径 → 被引用目标
///     反向：被引用目标标识符 → 所有引用者的路径列表
///     锚点：锚点标识符 → 锚点定义的块位置
/// </summary>
public sealed class ReferenceMap
{
    /// <summary>
    ///     正向引用字典：引用者路径 → 引用目标
    /// </summary>
    public IReadOnlyDictionary<ReferencePath, ReferenceTarget> forward { get; init; } =
        new Dictionary<ReferencePath, ReferenceTarget>();

    /// <summary>
    ///     反向引用字典：目标标识符 → 所有引用此目标的路径列表
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<ReferencePath>> backward { get; init; } =
        new Dictionary<string, IReadOnlyList<ReferencePath>>();

    /// <summary>
    ///     锚点注册表：锚点标识符 → 锚点定义所在的位置（Header ID、CodeBlock ID、Div ID 等）
    ///     有了这个可以回答"锚点 #intro 在文档中的哪个块定义的？"
    /// </summary>
    public IReadOnlyDictionary<string, ReferencePath> anchors { get; init; } = new Dictionary<string, ReferencePath>();

    /// <summary>
    ///     空引用映射
    /// </summary>
    public static ReferenceMap empty { get; } = new();

    /// <summary>
    ///     获取引用给定目标的所有来源路径（反向引用查询）
    /// </summary>
    public IReadOnlyList<ReferencePath> get_back_references(string targetId)
    {
        if (backward.TryGetValue(targetId, out var refs)) return refs;

        return [];
    }

    /// <summary>
    ///     获取给定路径的引用目标
    /// </summary>
    public ReferenceTarget? get_target(ReferencePath path)
    {
        if (forward.TryGetValue(path, out var target)) return target;

        return null;
    }

    /// <summary>
    ///     获取锚点定义的位置
    /// </summary>
    public ReferencePath? get_anchor(string anchorId)
    {
        if (anchors.TryGetValue(anchorId, out var path)) return path;

        return null;
    }

    /// <summary>
    ///     查询锚点是否被引用（反向引用查询）
    ///     例如："谁引用了 #intro 这个锚点？"
    /// </summary>
    public IReadOnlyList<ReferencePath> get_anchor_refs(string anchorId)
    {
        return get_back_references($"#{anchorId}");
    }

    /// <summary>
    ///     从文档中遍历收集所有引用和锚点定义，构建完整的双向索引
    /// </summary>
    public static ReferenceMap build(NotedownDocument document)
    {
        var forward = new Dictionary<ReferencePath, ReferenceTarget>();
        var backward = new Dictionary<string, List<ReferencePath>>();
        var anchors = new Dictionary<string, ReferencePath>();

        for (var blockIdx = 0; blockIdx < document.blocks.Count; blockIdx++)
            collect_from_block(document.blocks[blockIdx], blockIdx, forward, backward, anchors);

        var backwardRo = new Dictionary<string, IReadOnlyList<ReferencePath>>();
        foreach (var (key, list) in backward) backwardRo[key] = list.AsReadOnly();

        return new ReferenceMap
        {
            forward = forward,
            backward = backwardRo,
            anchors = anchors
        };
    }

    private static void collect_from_block(
        NotedownBlock block,
        int blockIdx,
        Dictionary<ReferencePath, ReferenceTarget> forward,
        Dictionary<string, List<ReferencePath>> backward,
        Dictionary<string, ReferencePath> anchors)
    {
        switch (block)
        {
            case Header header:
                collect_from_inlines(header.inlines, blockIdx, forward, backward);
                collect_anchor(header.attr.id, blockIdx, anchors);
                break;

            case Para para:
                collect_from_inlines(para.inlines, blockIdx, forward, backward);
                break;

            case Plain plain:
                collect_from_inlines(plain.inlines, blockIdx, forward, backward);
                break;

            case CodeBlock codeBlock:
                collect_anchor(codeBlock.attr.id, blockIdx, anchors);
                break;

            case Table table:
                collect_from_table(table, blockIdx, forward, backward);
                break;

            case BlockQuote blockQuote:
                for (var i = 0; i < blockQuote.children.Count; i++)
                    collect_from_block(blockQuote.children[i], blockIdx, forward, backward, anchors);

                break;

            case OrderedList orderedList:
                foreach (var item in orderedList.items)
                foreach (var itemBlock in item)
                    collect_from_block(itemBlock, blockIdx, forward, backward, anchors);

                break;

            case BulletList bulletList:
                foreach (var item in bulletList.items)
                foreach (var itemBlock in item)
                    collect_from_block(itemBlock, blockIdx, forward, backward, anchors);

                break;

            case DefinitionList definitionList:
                foreach (var item in definitionList.items)
                {
                    collect_from_inlines(item.term, blockIdx, forward, backward);

                    foreach (var definition in item.definitions)
                    foreach (var defBlock in definition)
                        collect_from_block(defBlock, blockIdx, forward, backward, anchors);
                }

                break;

            case Div div:
                collect_anchor(div.attr.id, blockIdx, anchors);

                for (var i = 0; i < div.children.Count; i++)
                    collect_from_block(div.children[i], blockIdx, forward, backward, anchors);

                break;

            case LineBlock lineBlock:
                for (var i = 0; i < lineBlock.lines.Count; i++)
                    collect_from_inlines(lineBlock.lines[i], blockIdx, forward, backward);

                break;
        }
    }

    private static void collect_from_inlines(
        IReadOnlyList<NotedownInline> inlines,
        int blockIdx,
        Dictionary<ReferencePath, ReferenceTarget> forward,
        Dictionary<string, List<ReferencePath>> backward)
    {
        for (var inlineIdx = 0; inlineIdx < inlines.Count; inlineIdx++)
            collect_from_inline(inlines[inlineIdx], blockIdx, inlineIdx, forward, backward);
    }

    private static void collect_from_inline(
        NotedownInline inline,
        int blockIdx,
        int inlineIdx,
        Dictionary<ReferencePath, ReferenceTarget> forward,
        Dictionary<string, List<ReferencePath>> backward)
    {
        switch (inline)
        {
            case Link link:
            {
                if (!string.IsNullOrEmpty(link.target.url))
                {
                    var kind = link.target.url.StartsWith('#')
                        ? ReferenceKind.internal_anchor
                        : ReferenceKind.external_url;
                    var target = new ReferenceTarget(kind, link.target.url, link.target.title);
                    var path = new ReferencePath { block_index = blockIdx, inline_index = inlineIdx };
                    forward[path] = target;
                    add_backward(backward, target.target_id, path);
                }

                collect_from_inlines(link.inlines, blockIdx, forward, backward);
                break;
            }
            case Image image:
            {
                if (!string.IsNullOrEmpty(image.target.url))
                {
                    var target = ReferenceTarget.img(image.target.url, image.target.title);
                    var path = new ReferencePath { block_index = blockIdx, inline_index = inlineIdx };
                    forward[path] = target;
                    add_backward(backward, target.target_id, path);
                }

                collect_from_inlines(image.inlines, blockIdx, forward, backward);
                break;
            }
            case Cite cite:
            {
                foreach (var citation in cite.citations)
                {
                    var target = ReferenceTarget.bib_key(citation.id);
                    var path = new ReferencePath { block_index = blockIdx, inline_index = inlineIdx };
                    forward[path] = target;
                    add_backward(backward, target.target_id, path);
                }

                break;
            }
            case Note note:
            {
                var target = ReferenceTarget.footnote($"fn_{blockIdx}_{inlineIdx}");
                var path = new ReferencePath { block_index = blockIdx, inline_index = inlineIdx };
                forward[path] = target;
                add_backward(backward, target.target_id, path);
                break;
            }
            case Emph emph:
                collect_from_inlines(emph.inlines, blockIdx, forward, backward);
                break;
            case Strong strong:
                collect_from_inlines(strong.inlines, blockIdx, forward, backward);
                break;
            case Strikeout strikeout:
                collect_from_inlines(strikeout.inlines, blockIdx, forward, backward);
                break;
            case Superscript superscript:
                collect_from_inlines(superscript.inlines, blockIdx, forward, backward);
                break;
            case Subscript subscript:
                collect_from_inlines(subscript.inlines, blockIdx, forward, backward);
                break;
            case SmallCaps smallCaps:
                collect_from_inlines(smallCaps.inlines, blockIdx, forward, backward);
                break;
            case Quoted quoted:
                collect_from_inlines(quoted.inlines, blockIdx, forward, backward);
                break;
            case Span span:
                collect_from_inlines(span.inlines, blockIdx, forward, backward);
                break;
        }
    }

    private static void collect_from_table(
        Table table,
        int blockIdx,
        Dictionary<ReferencePath, ReferenceTarget> forward,
        Dictionary<string, List<ReferencePath>> backward)
    {
        foreach (var row in table.head.rows)
        foreach (var cell in row.cells)
            collect_from_inlines(cell, blockIdx, forward, backward);

        foreach (var body in table.bodies)
        {
            foreach (var row in body.head_rows)
            foreach (var cell in row.cells)
                collect_from_inlines(cell, blockIdx, forward, backward);

            foreach (var row in body.rows)
            foreach (var cell in row.cells)
                collect_from_inlines(cell, blockIdx, forward, backward);
        }

        foreach (var row in table.foot.rows)
        foreach (var cell in row.cells)
            collect_from_inlines(cell, blockIdx, forward, backward);
    }

    private static void collect_anchor(
        string? id,
        int blockIdx,
        Dictionary<string, ReferencePath> anchors)
    {
        if (string.IsNullOrEmpty(id)) return;

        var path = new ReferencePath { block_index = blockIdx, inline_index = -1 };
        anchors[id] = path;
    }

    private static void add_backward(Dictionary<string, List<ReferencePath>> backward, string targetId,
        ReferencePath path)
    {
        if (!backward.TryGetValue(targetId, out var list))
        {
            list = [];
            backward[targetId] = list;
        }

        list.Add(path);
    }
}