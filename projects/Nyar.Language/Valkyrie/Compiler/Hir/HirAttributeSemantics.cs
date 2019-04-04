using Nyar.Language.Valkyrie.Compiler.Hir.Attributes;
using Nyar.Types.Externals;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

public static class HirAttributeSemantics
{
    public static IReadOnlyList<ExternalImport> collect_callable_external_import_links(IReadOnlyList<HirAttribute> attributes)
    {
        return collect_external_import_links(attributes, HirExternalImportSubject.callable);
    }

    public static IReadOnlyList<ExternalImport> collect_type_external_import_links(IReadOnlyList<HirAttribute> attributes)
    {
        return collect_external_import_links(attributes, HirExternalImportSubject.type);
    }

    private static IReadOnlyList<ExternalImport> collect_external_import_links(IReadOnlyList<HirAttribute> attributes, HirExternalImportSubject subject) {
        var externalImportLinks = new Dictionary<string, ExternalImport>(StringComparer.OrdinalIgnoreCase);

        foreach (var attribute in attributes)
        {
            if (string.Equals(attribute.name, "clr", StringComparison.OrdinalIgnoreCase))
            {
                externalImportLinks["clr"] = create_direct_clr_import_link(attribute.arguments, subject);
                continue;
            }

            if (string.Equals(attribute.name, "jvm", StringComparison.OrdinalIgnoreCase))
            {
                externalImportLinks["jvm"] = create_direct_jvm_import_link(attribute.arguments, subject);
                continue;
            }

            if (string.Equals(attribute.name, "wasm", StringComparison.OrdinalIgnoreCase))
                externalImportLinks["wasm"] = create_direct_wasm_import_link(attribute.arguments, subject);
        }

        foreach (var attribute in attributes)
        {
            if (!string.Equals(attribute.name, "import", StringComparison.OrdinalIgnoreCase) ||
                attribute.arguments.Count < 3)
                continue;

            var targetBackend = attribute.arguments[0].Trim();
            if (string.IsNullOrWhiteSpace(targetBackend) || externalImportLinks.ContainsKey(targetBackend)) continue;

            if (string.Equals(targetBackend, "clr", StringComparison.OrdinalIgnoreCase))
            {
                externalImportLinks[targetBackend] = create_import_clr_import_link(attribute.arguments, subject);
                continue;
            }

            if (string.Equals(targetBackend, "jvm", StringComparison.OrdinalIgnoreCase))
            {
                externalImportLinks[targetBackend] = create_import_jvm_import_link(attribute.arguments, subject);
                continue;
            }

            if (string.Equals(targetBackend, "wasm", StringComparison.OrdinalIgnoreCase))
            {
                externalImportLinks[targetBackend] = create_import_wasm_import_link(attribute.arguments, subject);
                continue;
            }

            // 未知后端暂不处理，避免引入不存在的导入链接类型。
        }

        return [.. externalImportLinks.Values];
    }

    public static bool try_get_external_import_link(
        IReadOnlyList<HirAttribute> attributes,
        CallingConvention convention,
        out ExternalImport externalImport)
    {
        foreach (var link in collect_callable_external_import_links(attributes))
            if (link.convention == convention)
            {
                externalImport = link;
                return true;
            }

        externalImport = null!;
        return false;
    }

    public static bool try_get_intrinsic_name(IReadOnlyList<HirAttribute> attributes, out string intrinsicName)
    {
        foreach (var attribute in attributes)
        {
            // 同时识别 [intrinsic("xxx")] 和 [vm("xxx")] 属性
            var isIntrinsicAttr = string.Equals(attribute.name, "intrinsic", StringComparison.Ordinal) ||
                                  string.Equals(attribute.name, "vm", StringComparison.Ordinal);
            if (!isIntrinsicAttr || attribute.arguments.Count == 0)
                continue;

            intrinsicName = normalize_intrinsic_name(attribute.arguments[0]);
            if (!string.IsNullOrWhiteSpace(intrinsicName)) return true;
        }

        intrinsicName = string.Empty;
        return false;
    }

    private static string normalize_intrinsic_name(string intrinsicName)
    {
        return intrinsicName.Trim();
    }

    private static ExternalImport create_direct_clr_import_link(
        IReadOnlyList<string> arguments,
        HirExternalImportSubject subject)
    {
        return subject switch
        {
            HirExternalImportSubject.type when arguments.Count == 2 =>
                new ExternalClrTypeImport(arguments[0], arguments[1]),
            HirExternalImportSubject.callable when arguments.Count == 3 =>
                new ExternalClrMethodImport(arguments[0], arguments[1], arguments[2]),
            HirExternalImportSubject.type =>
                throw new InvalidOperationException("类型上的 `[clr]` 需要 2 个参数：程序集名、类型全名。"),
            HirExternalImportSubject.callable =>
                throw new InvalidOperationException("函数上的 `[clr]` 需要 3 个参数：程序集名、类型全名、方法名。"),
            _ => throw new InvalidOperationException("未知的 CLR 外部导入目标。")
        };
    }

    private static ExternalImport create_import_clr_import_link(
        IReadOnlyList<string> arguments,
        HirExternalImportSubject subject)
    {
        return subject switch
        {
            HirExternalImportSubject.type when arguments.Count == 3 =>
                new ExternalClrTypeImport(arguments[1], arguments[2]),
            HirExternalImportSubject.callable when arguments.Count == 4 =>
                new ExternalClrMethodImport(arguments[1], arguments[2], arguments[3]),
            HirExternalImportSubject.type =>
                throw new InvalidOperationException("类型上的 `[import(\"clr\", ...)]` 需要 3 个参数：目标后端、程序集名、类型全名。"),
            HirExternalImportSubject.callable =>
                throw new InvalidOperationException("函数上的 `[import(\"clr\", ...)]` 需要 4 个参数：目标后端、程序集名、类型全名、方法名。"),
            _ => throw new InvalidOperationException("未知的 CLR 外部导入目标。")
        };
    }

    private static ExternalImport create_direct_jvm_import_link(
        IReadOnlyList<string> arguments,
        HirExternalImportSubject subject)
    {
        return subject switch
        {
            HirExternalImportSubject.type when arguments.Count == 1 =>
                new ExternalJvmClassImport(arguments[0]),
            HirExternalImportSubject.callable when arguments.Count >= 2 =>
                new ExternalJvmMethodImport(
                    arguments[0],
                    arguments[1],
                    arguments.Count >= 3 ? arguments[2] : null),
            HirExternalImportSubject.type =>
                throw new InvalidOperationException("类型上的 `[jvm]` 需要 1 个参数：JVM 类型全名。"),
            HirExternalImportSubject.callable =>
                throw new InvalidOperationException("函数上的 `[jvm]` 至少需要 2 个参数：类名、方法名。"),
            _ => throw new InvalidOperationException("未知的 JVM 外部导入目标。")
        };
    }

    private static ExternalImport create_import_jvm_import_link(
        IReadOnlyList<string> arguments,
        HirExternalImportSubject subject)
    {
        return subject switch
        {
            HirExternalImportSubject.type when arguments.Count == 2 =>
                new ExternalJvmClassImport(arguments[1]),
            HirExternalImportSubject.callable when arguments.Count >= 3 =>
                new ExternalJvmMethodImport(
                    arguments[1],
                    arguments[2],
                    arguments.Count >= 4 ? arguments[3] : null),
            HirExternalImportSubject.type =>
                throw new InvalidOperationException("类型上的 `[import(\"jvm\", ...)]` 需要 2 个参数：目标后端、JVM 类型全名。"),
            HirExternalImportSubject.callable =>
                throw new InvalidOperationException("函数上的 `[import(\"jvm\", ...)]` 至少需要 3 个参数：目标后端、类名、方法名。"),
            _ => throw new InvalidOperationException("未知的 JVM 外部导入目标。")
        };
    }

    private static ExternalImport create_direct_wasm_import_link(
        IReadOnlyList<string> arguments,
        HirExternalImportSubject subject)
    {
        if (subject != HirExternalImportSubject.callable)
            throw new InvalidOperationException("类型声明当前不支持 `[wasm]` 外部导入。");

        if (arguments.Count < 2)
            throw new InvalidOperationException("函数上的 `[wasm]` 需要 2 个参数：模块名、字段名。");

        return new ExternalWasmFunctionImport(arguments[0], arguments[1]);
    }

    private static ExternalImport create_import_wasm_import_link(
        IReadOnlyList<string> arguments,
        HirExternalImportSubject subject)
    {
        if (subject != HirExternalImportSubject.callable)
            throw new InvalidOperationException("类型声明当前不支持 `[import(\"wasm\", ...)]` 外部导入。");

        if (arguments.Count < 3)
            throw new InvalidOperationException("函数上的 `[import(\"wasm\", ...)]` 需要 3 个参数：目标后端、模块名、字段名。");

        return new ExternalWasmFunctionImport(arguments[1], arguments[2]);
    }

    private enum HirExternalImportSubject
    {
        callable,
        type
    }
}