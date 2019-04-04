namespace Nyar.Language.Valkyrie.Compiler.Hir.Attributes;

/// <summary>
///     `[data]` 的结构化容器形状种类。
///     这里只描述中性的字段组织方式，不等同于 `hashmap` / `btreemap`
///     之类具体容器实现，也不涉及 `json` / `toml` 等文本格式。
/// </summary>
public enum HirDataContainerKind
{
    /// <summary>
    ///     以对象字段视图承载结构化数据。
    /// </summary>
    object_fields
}