using System.Collections.Generic;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;
using TextSpan = Std.Text.TextSpan;

namespace Nyar.Language.Valkyrie.Diagnostics;

/// <summary>
///     Valkyrie 诊断规则的唯一注册入口，集中管理规则名、稳定编号、默认级别、<c>system</c>、
///     中文描述与默认消息模板，并负责生成展示码与语义层复合码。
/// </summary>
public static class ValkyrieDiagnosticRegistry
{
    private static readonly IReadOnlyList<ValkyrieDiagnosticDescriptor> descriptors = build_descriptors();
    private static readonly IReadOnlyDictionary<string, ValkyrieDiagnosticDescriptor> byRuleName = build_by_rule_name();
    private static readonly IReadOnlyDictionary<int, ValkyrieDiagnosticDescriptor> byStableCode = build_by_stable_code();

    /// <summary>
    ///     全量规则枚举，按稳定编号升序排列。
    /// </summary>
    public static IReadOnlyList<ValkyrieDiagnosticDescriptor> all => descriptors;

    /// <summary>
    ///     按规则名查询 descriptor。
    /// </summary>
    public static bool try_get_by_rule_name(
        string ruleName,
        out ValkyrieDiagnosticDescriptor descriptor)
    {
        return byRuleName.TryGetValue(ruleName, out descriptor);
    }

    /// <summary>
    ///     按稳定编号查询 descriptor。
    /// </summary>
    public static bool try_get_by_stable_code(
        int stableCode,
        out ValkyrieDiagnosticDescriptor descriptor)
    {
        return byStableCode.TryGetValue(stableCode, out descriptor);
    }

    /// <summary>
    ///     返回指定规则面的默认 descriptor 集合。
    ///     <para>
    ///         <see cref="ValkyrieDiagnosticSystem.lint_core" /> 会并入 <see cref="ValkyrieDiagnosticSystem.check_core" />；
    ///         <see cref="ValkyrieDiagnosticSystem.lint_style" /> 仅返回自身。
    ///     </para>
    /// </summary>
    public static IReadOnlyList<ValkyrieDiagnosticDescriptor> get_default_rules(
        ValkyrieDiagnosticSystem system)
    {
        var result = new List<ValkyrieDiagnosticDescriptor>();
        foreach (var descriptor in descriptors)
        {
            if (matches_system(descriptor.system, system))
            {
                result.Add(descriptor);
            }
        }

        return result;
    }

    /// <summary>
    ///     根据最终严重级别生成展示码首段，例如 <c>E0005</c> 或 <c>W0101</c>。
    /// </summary>
    public static string create_display_code(
        ValkyrieDiagnosticDescriptor descriptor,
        DiagnosticSeverity effectiveSeverity,
        int digits = 4)
    {
        var prefix = resolve_severity_prefix(effectiveSeverity);
        return $"{prefix}{descriptor.format_stable_code(digits)}";
    }

    /// <summary>
    ///     产出语义层复合码，格式为 <c>&lt;display_code&gt; &lt;rule_name&gt;</c>。
    /// </summary>
    public static string create_diagnostic_code(
        ValkyrieDiagnosticDescriptor descriptor,
        DiagnosticSeverity effectiveSeverity,
        int digits = 4)
    {
        var displayCode = create_display_code(descriptor, effectiveSeverity, digits);
        return $"{displayCode} {descriptor.rule_name}";
    }

    /// <summary>
    ///     构造 <see cref="Diagnostic" />，由 registry 完成 descriptor 查询与稳定编号映射。
    /// </summary>
    public static Diagnostic create_diagnostic(
        string ruleName,
        TextSpan span,
        DiagnosticSeverity effectiveSeverity,
        string message,
        DiagnosticRelatedSpan[]? relatedSpans = null,
        string[]? hints = null,
        string? filePath = null,
        SourceSpan sourceSpan = default,
        int digits = 4)
    {
        if (!try_get_by_rule_name(ruleName, out var descriptor))
        {
            return new Diagnostic(
                span,
                message,
                effectiveSeverity,
                code: null,
                source: new DiagnosticSource(
                    filePath,
                    sourceSpan,
                    hints,
                    relatedSpans));
        }

        return new Diagnostic(
            span,
            message,
            effectiveSeverity,
            code: descriptor.stable_code,
            source: new DiagnosticSource(
                filePath,
                sourceSpan,
                hints,
                relatedSpans));
    }

    private static bool matches_system(
        ValkyrieDiagnosticSystem descriptorSystem,
        ValkyrieDiagnosticSystem requested)
    {
        if (descriptorSystem == requested)
        {
            return true;
        }

        return requested == ValkyrieDiagnosticSystem.lint_core &&
               descriptorSystem == ValkyrieDiagnosticSystem.check_core;
    }

    private static string resolve_severity_prefix(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.fatal => "F",
            DiagnosticSeverity.error => "E",
            DiagnosticSeverity.warning => "W",
            DiagnosticSeverity.info => "I",
            DiagnosticSeverity.hint => "H",
            _ => severity.ToString().Substring(0, 1).ToUpperInvariant()
        };
    }

    private static IReadOnlyDictionary<string, ValkyrieDiagnosticDescriptor> build_by_rule_name()
    {
        var dict = new Dictionary<string, ValkyrieDiagnosticDescriptor>(System.StringComparer.Ordinal);
        foreach (var descriptor in descriptors)
        {
            dict[descriptor.rule_name] = descriptor;
        }

        return dict;
    }

    private static IReadOnlyDictionary<int, ValkyrieDiagnosticDescriptor> build_by_stable_code()
    {
        var dict = new Dictionary<int, ValkyrieDiagnosticDescriptor>();
        foreach (var descriptor in descriptors)
        {
            dict[descriptor.stable_code] = descriptor;
        }

        return dict;
    }

    private static ValkyrieDiagnosticDescriptor[] build_descriptors()
    {
        var list = new List<ValkyrieDiagnosticDescriptor>();

        register_check_core_errors(list);
        register_lint_core_warnings(list);
        register_p1_errors(list);
        register_p1_warnings(list);
        register_lint_style(list);

        list.Sort((left, right) => left.stable_code.CompareTo(right.stable_code));
        return [.. list];
    }

    private static void add(
        List<ValkyrieDiagnosticDescriptor> list,
        string ruleName,
        int stableCode,
        DiagnosticSeverity defaultSeverity,
        DiagnosticSeverity floorSeverity,
        ValkyrieDiagnosticSystem system,
        string title,
        string messageTemplate)
    {
        list.Add(new ValkyrieDiagnosticDescriptor(
            ruleName,
            stableCode,
            defaultSeverity,
            floorSeverity,
            system,
            title,
            messageTemplate));
    }

    private static void register_check_core_errors(List<ValkyrieDiagnosticDescriptor> list)
    {
        add(list, ValkyrieRuleNames.IMPORT_NOT_FOUND, 1,
            DiagnosticSeverity.error, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core,
            "`using` / `using!` 的 module_path 不存在",
            "`using` 指向的模块路径 `{0}` 不存在。");

        add(list, ValkyrieRuleNames.IMPORT_SELECTION_NOT_FOUND, 2,
            DiagnosticSeverity.error, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core,
            "选择导入项不存在",
            "`using` 的选择项 `{0}` 不存在于目标模块中。");

        add(list, ValkyrieRuleNames.IMPORT_EMPTY_SELECTION, 3,
            DiagnosticSeverity.error, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core,
            "`using foo.{}` 空选择列表",
            "`using` 的选择列表为空，请提供至少一个导入项。");

        add(list, ValkyrieRuleNames.IMPORT_DUPLICATE_SELECTION, 4,
            DiagnosticSeverity.error, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core,
            "单条导入内重复选择同名项",
            "`using` 选择项 `{0}` 在同一条导入中重复声明。");

        add(list, ValkyrieRuleNames.IMPORT_ALIAS_CONFLICT, 5,
            DiagnosticSeverity.error, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core,
            "同一 namespace 中两个不同导入使用相同 alias",
            "导入别名 `{0}` 冲突：`{1}` 与当前 namespace 中已有导入使用了相同别名。");

        add(list, ValkyrieRuleNames.IMPORT_ALIAS_SHADOWS_LOCAL, 6,
            DiagnosticSeverity.error, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core,
            "alias 与当前作用域已有声明冲突",
            "导入别名 `{0}` 与当前作用域已有声明同名，造成遮蔽。");

        add(list, ValkyrieRuleNames.REEXPORT_TARGET_NOT_FOUND, 7,
            DiagnosticSeverity.error, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core,
            "`using!` 导出的目标不存在或不可见",
            "`using!` 导出的目标 `{0}` 不存在或不可见。");

        add(list, ValkyrieRuleNames.REEXPORT_CONFLICT, 8,
            DiagnosticSeverity.error, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core,
            "多个 reexport 生成相同可见名且来源不同",
            "多个 reexport 生成了相同的可见名 `{0}`，但来源不同。");

        add(list, ValkyrieRuleNames.NAMESPACE_PRIMARY_CONFLICT, 9,
            DiagnosticSeverity.error, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core,
            "同一 namespace 有多个 `namespace!` 主入口",
            "同一 namespace `{0}` 有多个 `namespace!` 主入口。");

        add(list, ValkyrieRuleNames.NAMESPACE_PRIMARY_DUPLICATE_IN_FILE, 10,
            DiagnosticSeverity.error, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core,
            "单文件内重复声明 `namespace!`",
            "文件 `{0}` 内重复声明 `namespace!`。");

        add(list, ValkyrieRuleNames.REEXPORT_CYCLE, 11,
            DiagnosticSeverity.error, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core,
            "reexport 图存在导致公开面不可确定的环",
            "reexport 图中存在环：{0}，公开面不可确定。");

        add(list, ValkyrieRuleNames.IMPORT_RESOLVE_AMBIGUOUS, 12,
            DiagnosticSeverity.error, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core,
            "未限定名命中多个 import 来源",
            "未限定名 `{0}` 命中多个 import 来源，解析存在歧义。");
    }

    private static void register_lint_core_warnings(List<ValkyrieDiagnosticDescriptor> list)
    {
        add(list, ValkyrieRuleNames.DUPLICATE_USING, 101,
            DiagnosticSeverity.warning, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_core,
            "同一 namespace 下重复导入完全相同的目标",
            "重复的 `using` 导入：`{0}` 已在当前 namespace 中声明过。");

        add(list, ValkyrieRuleNames.REDUNDANT_REEXPORT, 102,
            DiagnosticSeverity.warning, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_core,
            "对当前 namespace 自己执行 `using!`",
            "`using! {0}` 是冗余的：同一 namespace 中定义的符号天然可导出，无需再次 reexport。");

        add(list, ValkyrieRuleNames.UNUSED_USING, 103,
            DiagnosticSeverity.warning, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_core,
            "`using` 未被当前文件使用",
            "`using {0}` 未被当前文件使用，可以移除。");

        add(list, ValkyrieRuleNames.UNUSED_IMPORT_ALIAS, 104,
            DiagnosticSeverity.warning, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_core,
            "alias 声明后未被使用",
            "导入别名 `{0}` 声明后未被使用，可以移除。");

        add(list, ValkyrieRuleNames.NAMESPACE_PRIMARY_NOT_IN_UNDERSCORE, 105,
            DiagnosticSeverity.warning, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_core,
            "`namespace!` 不在 `_.v`",
            "`namespace!` 应当位于 `_.v` 入口文件中，当前出现在 `{0}`。");

        add(list, ValkyrieRuleNames.UNDERSCORE_FILE_HAS_IMPLEMENTATION, 106,
            DiagnosticSeverity.warning, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_core,
            "`_.v` 中出现具体实现",
            "`_.v` 入口文件应当只包含聚合声明，不应出现具体实现 `{0}`。");

        add(list, ValkyrieRuleNames.REEXPORT_OUTSIDE_PRIMARY_ENTRY, 107,
            DiagnosticSeverity.warning, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_core,
            "`using!` 出现在非主入口文件",
            "`using!` reexport 出现在非主入口文件 `{0}` 中。");
    }

    private static void register_p1_errors(List<ValkyrieDiagnosticDescriptor> list)
    {
        add(list, ValkyrieRuleNames.NAMESPACE_PRIMARY_SCOPE_CONFLICT, 201,
            DiagnosticSeverity.error, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core,
            "`namespace!` 与普通 `namespace` 组合形成冲突语义",
            "`namespace!` 与普通 `namespace` 组合在 `{0}` 形成冲突语义。");

        add(list, ValkyrieRuleNames.IMPORT_GRAPH_CONFLICT, 202,
            DiagnosticSeverity.error, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core,
            "多跳导出图汇入同名不同实体",
            "多跳导出图汇入同名实体 `{0}`，但实际指向不同实体。");

        add(list, ValkyrieRuleNames.IMPORT_ALIAS_RESERVED_NAME, 203,
            DiagnosticSeverity.error, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core,
            "alias 占用保留名或关键字",
            "导入别名 `{0}` 占用了保留名或关键字，请更换名称。");
    }

    private static void register_p1_warnings(List<ValkyrieDiagnosticDescriptor> list)
    {
        add(list, ValkyrieRuleNames.REDUNDANT_PLAIN_USING_IN_ENTRY, 301,
            DiagnosticSeverity.warning, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_core,
            "入口文件中的普通 `using` 没有实际收益",
            "入口文件 `{0}` 中的普通 `using` 没有实际收益，可考虑移除。");

        add(list, ValkyrieRuleNames.DEEP_REEXPORT_CHAIN, 302,
            DiagnosticSeverity.warning, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_core,
            "reexport 链过深，影响可维护性",
            "reexport 链深度为 {0}，超过建议阈值，影响可维护性。");

        add(list, ValkyrieRuleNames.REEXPORT_CYCLE_RECOVERABLE, 303,
            DiagnosticSeverity.warning, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_core,
            "存在可化简的导出环",
            "存在可化简的导出环：{0}。");
    }

    private static void register_lint_style(List<ValkyrieDiagnosticDescriptor> list)
    {
        add(list, ValkyrieRuleNames.IMPORT_ORDER_STYLE, 401,
            DiagnosticSeverity.warning, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_style,
            "`using` / `using!` 排序不符合约定",
            "`using` / `using!` 排序不符合项目约定。");

        add(list, ValkyrieRuleNames.NAMESPACE_DECL_ORDER_STYLE, 402,
            DiagnosticSeverity.warning, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_style,
            "`namespace!`、`using`、实现声明顺序不符合约定",
            "`namespace!`、`using`、实现声明的顺序不符合项目约定。");

        add(list, ValkyrieRuleNames.ALIAS_NAME_STYLE, 403,
            DiagnosticSeverity.warning, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_style,
            "alias 命名不符合项目风格",
            "导入别名 `{0}` 的命名不符合项目风格。");

        add(list, ValkyrieRuleNames.ENTRY_FILE_MISSING_REEXPORTS, 404,
            DiagnosticSeverity.warning, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_style,
            "主入口未聚合预期公开项",
            "主入口文件 `{0}` 未聚合预期公开项。");

        add(list, ValkyrieRuleNames.REEXPORT_CHAIN_TOO_DEEP, 405,
            DiagnosticSeverity.warning, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_style,
            "reexport 深度超过建议阈值",
            "reexport 深度 {0} 超过建议阈值 {1}。");

        add(list, ValkyrieRuleNames.IMPORT_CAN_BE_SIMPLIFIED, 406,
            DiagnosticSeverity.warning, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_style,
            "选择导入或 alias 可以简化",
            "`using` 选择导入 `{0}` 可以简化。");

        add(list, ValkyrieRuleNames.ENTRY_FILE_TOO_COMPLEX, 407,
            DiagnosticSeverity.warning, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_style,
            "`_.v` 虽无实现，但聚合规模过大影响维护",
            "`_.v` 入口文件聚合规模为 {0}，超过建议阈值，影响维护。");
    }
}
