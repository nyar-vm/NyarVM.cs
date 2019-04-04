namespace Nyar.Language.Valkyrie.Diagnostics;

/// <summary>
///     Valkyrie 诊断规则名常量，集中所有 <c>VALKYRIE_</c> 规则名以避免散落硬编码。
/// </summary>
public static class ValkyrieRuleNames
{
    #region P0 Error (check_core)

    /// <summary>
    ///     `using` / `using!` 的 module_path 不存在。
    /// </summary>
    public const string IMPORT_NOT_FOUND = "VALKYRIE_IMPORT_NOT_FOUND";

    /// <summary>
    ///     选择导入项不存在。
    /// </summary>
    public const string IMPORT_SELECTION_NOT_FOUND = "VALKYRIE_IMPORT_SELECTION_NOT_FOUND";

    /// <summary>
    ///     `using foo.{}` 空选择列表。
    /// </summary>
    public const string IMPORT_EMPTY_SELECTION = "VALKYRIE_IMPORT_EMPTY_SELECTION";

    /// <summary>
    ///     单条导入内重复选择同名项。
    /// </summary>
    public const string IMPORT_DUPLICATE_SELECTION = "VALKYRIE_IMPORT_DUPLICATE_SELECTION";

    /// <summary>
    ///     同一 namespace 中两个不同导入使用相同 alias。
    /// </summary>
    public const string IMPORT_ALIAS_CONFLICT = "VALKYRIE_IMPORT_ALIAS_CONFLICT";

    /// <summary>
    ///     alias 与当前作用域已有声明冲突。
    /// </summary>
    public const string IMPORT_ALIAS_SHADOWS_LOCAL = "VALKYRIE_IMPORT_ALIAS_SHADOWS_LOCAL";

    /// <summary>
    ///     `using!` 导出的目标不存在或不可见。
    /// </summary>
    public const string REEXPORT_TARGET_NOT_FOUND = "VALKYRIE_REEXPORT_TARGET_NOT_FOUND";

    /// <summary>
    ///     多个 reexport 生成相同可见名且来源不同。
    /// </summary>
    public const string REEXPORT_CONFLICT = "VALKYRIE_REEXPORT_CONFLICT";

    /// <summary>
    ///     同一 namespace 有多个 `namespace!` 主入口。
    /// </summary>
    public const string NAMESPACE_PRIMARY_CONFLICT = "VALKYRIE_NAMESPACE_PRIMARY_CONFLICT";

    /// <summary>
    ///     单文件内重复声明 `namespace!`。
    /// </summary>
    public const string NAMESPACE_PRIMARY_DUPLICATE_IN_FILE = "VALKYRIE_NAMESPACE_PRIMARY_DUPLICATE_IN_FILE";

    /// <summary>
    ///     reexport 图存在导致公开面不可确定的环。
    /// </summary>
    public const string REEXPORT_CYCLE = "VALKYRIE_REEXPORT_CYCLE";

    /// <summary>
    ///     未限定名命中多个 import 来源。
    /// </summary>
    public const string IMPORT_RESOLVE_AMBIGUOUS = "VALKYRIE_IMPORT_RESOLVE_AMBIGUOUS";

    #endregion

    #region P0 Warning (lint_core)

    /// <summary>
    ///     同一 namespace 下重复导入完全相同的目标。
    /// </summary>
    public const string DUPLICATE_USING = "VALKYRIE_DUPLICATE_USING";

    /// <summary>
    ///     对当前 namespace 自己执行 `using!`。
    /// </summary>
    public const string REDUNDANT_REEXPORT = "VALKYRIE_REDUNDANT_REEXPORT";

    /// <summary>
    ///     `using` 未被当前文件使用。
    /// </summary>
    public const string UNUSED_USING = "VALKYRIE_UNUSED_USING";

    /// <summary>
    ///     alias 声明后未被使用。
    /// </summary>
    public const string UNUSED_IMPORT_ALIAS = "VALKYRIE_UNUSED_IMPORT_ALIAS";

    /// <summary>
    ///     `namespace!` 不在 `_.v`。
    /// </summary>
    public const string NAMESPACE_PRIMARY_NOT_IN_UNDERSCORE = "VALKYRIE_NAMESPACE_PRIMARY_NOT_IN_UNDERSCORE";

    /// <summary>
    ///     `_.v` 中出现具体实现。
    /// </summary>
    public const string UNDERSCORE_FILE_HAS_IMPLEMENTATION = "VALKYRIE_UNDERSCORE_FILE_HAS_IMPLEMENTATION";

    /// <summary>
    ///     `using!` 出现在非主入口文件。
    /// </summary>
    public const string REEXPORT_OUTSIDE_PRIMARY_ENTRY = "VALKYRIE_REEXPORT_OUTSIDE_PRIMARY_ENTRY";

    #endregion

    #region P1 Error (check_core)

    /// <summary>
    ///     `namespace!` 与普通 `namespace` 组合形成冲突语义。
    /// </summary>
    public const string NAMESPACE_PRIMARY_SCOPE_CONFLICT = "VALKYRIE_NAMESPACE_PRIMARY_SCOPE_CONFLICT";

    /// <summary>
    ///     多跳导出图汇入同名不同实体。
    /// </summary>
    public const string IMPORT_GRAPH_CONFLICT = "VALKYRIE_IMPORT_GRAPH_CONFLICT";

    /// <summary>
    ///     alias 占用保留名或关键字。
    /// </summary>
    public const string IMPORT_ALIAS_RESERVED_NAME = "VALKYRIE_IMPORT_ALIAS_RESERVED_NAME";

    #endregion

    #region P1 Warning (lint_core)

    /// <summary>
    ///     入口文件中的普通 `using` 没有实际收益。
    /// </summary>
    public const string REDUNDANT_PLAIN_USING_IN_ENTRY = "VALKYRIE_REDUNDANT_PLAIN_USING_IN_ENTRY";

    /// <summary>
    ///     reexport 链过深，影响可维护性。
    /// </summary>
    public const string DEEP_REEXPORT_CHAIN = "VALKYRIE_DEEP_REEXPORT_CHAIN";

    /// <summary>
    ///     存在可化简的导出环。
    /// </summary>
    public const string REEXPORT_CYCLE_RECOVERABLE = "VALKYRIE_REEXPORT_CYCLE_RECOVERABLE";

    #endregion

    #region lint_style

    /// <summary>
    ///     `using` / `using!` 排序不符合约定。
    /// </summary>
    public const string IMPORT_ORDER_STYLE = "VALKYRIE_IMPORT_ORDER_STYLE";

    /// <summary>
    ///     `namespace!`、`using`、实现声明顺序不符合约定。
    /// </summary>
    public const string NAMESPACE_DECL_ORDER_STYLE = "VALKYRIE_NAMESPACE_DECL_ORDER_STYLE";

    /// <summary>
    ///     alias 命名不符合项目风格。
    /// </summary>
    public const string ALIAS_NAME_STYLE = "VALKYRIE_ALIAS_NAME_STYLE";

    /// <summary>
    ///     主入口未聚合预期公开项。
    /// </summary>
    public const string ENTRY_FILE_MISSING_REEXPORTS = "VALKYRIE_ENTRY_FILE_MISSING_REEXPORTS";

    /// <summary>
    ///     reexport 深度超过建议阈值。
    /// </summary>
    public const string REEXPORT_CHAIN_TOO_DEEP = "VALKYRIE_REEXPORT_CHAIN_TOO_DEEP";

    /// <summary>
    ///     选择导入或 alias 可以简化。
    /// </summary>
    public const string IMPORT_CAN_BE_SIMPLIFIED = "VALKYRIE_IMPORT_CAN_BE_SIMPLIFIED";

    /// <summary>
    ///     `_.v` 虽无实现，但聚合规模过大影响维护。
    /// </summary>
    public const string ENTRY_FILE_TOO_COMPLEX = "VALKYRIE_ENTRY_FILE_TOO_COMPLEX";

    #endregion
}
