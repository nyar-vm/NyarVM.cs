using System.Text;

namespace Nyar.Language.Awsl.Asgard.Compiler;

/// <summary>
///     JS 桥接代码生成器，按 Valkyrie 函数签名和标注自动生成 JS 胶水代码
/// </summary>
public static class JsBridgeGenerator
{
    /// <summary>
    ///     为导入列表生成 JS 桥接胶水代码
    /// </summary>
    /// <param name="jsImports">JS 导入信息列表</param>
    /// <returns>JS 桥接代码</returns>
    public static string generate_bridge_code(List<JsImportInfo> jsImports)
    {
        var sb = new StringBuilder();
        sb.AppendLine("  const bridge = {};");
        sb.AppendLine();

        foreach (var import in jsImports)
        {
            if (!string.IsNullOrWhiteSpace(import.js_function))
            {
                sb.AppendLine($"  bridge.{import.func_name} = (function() {{");
                sb.AppendLine($"    return {import.js_function};");
                sb.AppendLine("  })();");
            }
            else if (!string.IsNullOrWhiteSpace(import.module_name))
            {
                sb.AppendLine($"  bridge.{import.func_name} = function() {{");
                sb.AppendLine($"    throw new Error('外部模块 \"{import.module_name}.{import.func_name}\" 需在运行时注入');");
                sb.AppendLine("  };");
            }
            else
            {
                sb.AppendLine($"  bridge.{import.func_name} = {import.func_name};");
            }

            sb.AppendLine();
        }

        sb.AppendLine("  return bridge;");
        return sb.ToString();
    }
}
