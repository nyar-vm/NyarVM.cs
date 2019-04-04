using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator.Command;

/// <summary>
///     为标记了 <c>[Sonic.Standard.Command.Command]</c> 特性的 partial 类自动生成命令行解析器、
///     帮助文本、JSON Schema 和 Shell 补全脚本。
///     命令名称从类名自动推断（去掉 Command 后缀后转为 kebab-case），
///     选项名称从属性名自动推断，描述优先读取 <c>&lt;summary&gt;</c> XML 注释。
/// </summary>
[Generator]
public sealed class CommandGenerator : IIncrementalGenerator
{
    private const string _command_attribute_full_name = "Sonic.Standard.Widget.Terminal.CommandAttribute";
    private const string _argument_attribute_full_name = "Sonic.Standard.Widget.Terminal.ArgumentAttribute";
    private const string _option_attribute_full_name = "Sonic.Standard.Widget.Terminal.OptionAttribute";
    private const string _commands_attribute_full_name = "Sonic.Standard.Widget.Terminal.CommandsAttribute";
    private const string _after_parse_attribute_full_name = "Sonic.Standard.Widget.Terminal.AfterParseAttribute";

    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targetTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _command_attribute_full_name,
                static (node, _) => node is ClassDeclarationSyntax classDecl &&
                                    classDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, ct) => transform_command(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(targetTypes, generate_source);
    }

    /// <summary>
    ///     将 PascalCase 标识符转换为 kebab-case。
    ///     例如：GreetCommand → greet，OutputPath → output-path。
    /// </summary>
    private static string to_kebab_case(string name)
    {
        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('-');

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     从类名推断命令名称：去掉 Command 后缀后转为 kebab-case。
    ///     例如：GreetCommand → greet，BuildCommand → build。
    /// </summary>
    private static string infer_command_name(string typeName)
    {
        var name = typeName;
        if (name.EndsWith("Command", StringComparison.Ordinal))
            name = name.Substring(0, name.Length - "Command".Length);

        return to_kebab_case(name);
    }

    /// <summary>
    ///     从属性名推断选项长名称：转为 kebab-case。
    ///     例如：OutputPath → output-path，Verbose → verbose。
    /// </summary>
    private static string infer_option_name(string propertyName)
    {
        return to_kebab_case(propertyName);
    }

    /// <summary>
    ///     从符号的 <c>&lt;summary&gt;</c> XML 注释中提取描述文本。
    /// </summary>
    private static string? get_summary_comment(ISymbol symbol)
    {
        var xml = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrEmpty(xml)) return null;

        var startTag = "<summary>";
        var endTag = "</summary>";
        var startIdx = xml.IndexOf(startTag, StringComparison.Ordinal);
        if (startIdx < 0) return null;

        startIdx += startTag.Length;
        var endIdx = xml.IndexOf(endTag, startIdx, StringComparison.Ordinal);
        if (endIdx < 0) return null;

        var text = xml.Substring(startIdx, endIdx - startIdx).Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    /// <summary>
    ///     从语法上下文中提取命令信息。
    /// </summary>
    private static CommandInfo? transform_command(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var commandAttr = context.Attributes.First();
        string? description = null;
        string? long_description = null;
        string[]? aliases = null;
        var hide = false;
        string? version = null;
        string? env_prefix = null;
        string? resource_key = null;

        foreach (var named in commandAttr.NamedArguments)
            switch (named.Key)
            {
                case "description" when named.Value.Value is string d:
                    description = d;
                    break;
                case "long_description" when named.Value.Value is string ld:
                    long_description = ld;
                    break;
                case "aliases" when named.Value.Values.Length > 0:
                    aliases = [
                        .. named.Value.Values
                            .Select(v => (string?)v.Value)
                            .Where(v => v is not null)
                    ]!;
                    break;
                case "hide" when named.Value.Value is bool h:
                    hide = h;
                    break;
                case "version" when named.Value.Value is string v:
                    version = v;
                    break;
                case "env_prefix" when named.Value.Value is string ep:
                    env_prefix = ep;
                    break;
                case "resource_key" when named.Value.Value is string rk:
                    resource_key = rk;
                    break;
            }

        var name = infer_command_name(typeSymbol.Name);

        if (description is null) description = get_summary_comment(typeSymbol);

        var arguments = new List<ArgumentInfo>();
        var options = new List<OptionInfo>();
        SubCommandInfo? sub_command = null;
        var after_parse_methods = new List<string>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member.IsStatic) continue;

            if (member is IPropertySymbol prop)
            {
                var argAttr = prop.GetAttributes().FirstOrDefault(a =>
                    a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                    $"global::{_argument_attribute_full_name}");

                if (argAttr is not null)
                {
                    arguments.Add(extract_argument(prop, argAttr));
                    continue;
                }

                var optAttr = prop.GetAttributes().FirstOrDefault(a =>
                    a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                    $"global::{_option_attribute_full_name}");

                if (optAttr is not null)
                {
                    options.Add(extract_option(prop, optAttr));
                    continue;
                }

                var cmdsAttr = prop.GetAttributes().FirstOrDefault(a =>
                    a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                    $"global::{_commands_attribute_full_name}");

                if (cmdsAttr is not null && prop.Type.TypeKind == TypeKind.Enum)
                    sub_command = extract_sub_commands(prop, cmdsAttr);
            }
            else if (member is IMethodSymbol method)
            {
                var afterAttr = method.GetAttributes().FirstOrDefault(a =>
                    a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                    $"global::{_after_parse_attribute_full_name}");

                if (afterAttr is not null && method.Parameters.Length == 0) after_parse_methods.Add(method.Name);
            }
        }

        return new CommandInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            name,
            description,
            long_description,
            aliases,
            hide,
            version,
            env_prefix,
            resource_key,
            arguments,
            options,
            sub_command,
            after_parse_methods
        );
    }

    /// <summary>
    ///     从属性和特性中提取位置参数信息。
    /// </summary>
    private static ArgumentInfo extract_argument(IPropertySymbol prop, AttributeData attr)
    {
        var position = -1;
        string? description = null;
        object? default_value = null;
        var required = false;
        string? value_name = null;
        string? resource_key = null;

        foreach (var named in attr.NamedArguments)
            switch (named.Key)
            {
                case "Position" when named.Value.Value is int p:
                    position = p;
                    break;
                case "Description" when named.Value.Value is string d:
                    description = d;
                    break;
                case "DefaultValue":
                    default_value = named.Value.Value;
                    break;
                case "Required" when named.Value.Value is bool r:
                    required = r;
                    break;
                case "ValueName" when named.Value.Value is string vn:
                    value_name = vn;
                    break;
                case "ResourceKey" when named.Value.Value is string rk:
                    resource_key = rk;
                    break;
            }

        if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int pos) position = pos;

        if (description is null) description = get_summary_comment(prop);

        return new ArgumentInfo(
            prop.Name,
            prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            position,
            description,
            default_value,
            required,
            value_name ?? prop.Name,
            resource_key);
    }

    /// <summary>
    ///     从属性和特性中提取命名选项信息。
    ///     选项长名称优先使用显式指定值，否则从属性名自动推断。
    ///     短名称从构造函数参数提取。
    /// </summary>
    private static OptionInfo extract_option(IPropertySymbol prop, AttributeData attr)
    {
        string? long_name = null;
        var short_name = '\0';
        string? description = null;
        object? default_value = null;
        var required = false;
        string? value_name = null;
        var takes_value = true;
        string? env = null;
        var hide = false;
        string? resource_key = null;

        if (attr.ConstructorArguments.Length >= 2)
        {
            if (attr.ConstructorArguments[0].Value is char sn) short_name = sn;

            if (attr.ConstructorArguments[1].Value is string ln) long_name = ln;
        }
        else if (attr.ConstructorArguments.Length == 1)
        {
            var arg = attr.ConstructorArguments[0];
            if (arg.Type?.SpecialType == SpecialType.System_Char && arg.Value is char c)
                short_name = c;
            else if (arg.Value is string s) long_name = s;
        }

        foreach (var named in attr.NamedArguments)
            switch (named.Key)
            {
                case "Description" when named.Value.Value is string d:
                    description = d;
                    break;
                case "DefaultValue":
                    default_value = named.Value.Value;
                    break;
                case "Required" when named.Value.Value is bool r:
                    required = r;
                    break;
                case "ValueName" when named.Value.Value is string vn:
                    value_name = vn;
                    break;
                case "TakesValue" when named.Value.Value is bool tv:
                    takes_value = tv;
                    break;
                case "Env" when named.Value.Value is string e:
                    env = e;
                    break;
                case "Hide" when named.Value.Value is bool h:
                    hide = h;
                    break;
                case "ResourceKey" when named.Value.Value is string rk:
                    resource_key = rk;
                    break;
            }

        long_name ??= infer_option_name(prop.Name);
        description ??= get_summary_comment(prop);

        var prop_type = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (prop.Type.SpecialType == SpecialType.System_Boolean && takes_value) takes_value = false;

        return new OptionInfo(
            prop.Name,
            prop_type,
            long_name,
            short_name,
            description,
            default_value,
            required,
            value_name ?? long_name,
            takes_value,
            env,
            hide,
            resource_key);
    }

    /// <summary>
    ///     从枚举属性和特性中提取子命令信息。
    ///     子命令名称从枚举成员名自动推断（转为 kebab-case）。
    /// </summary>
    private static SubCommandInfo extract_sub_commands(IPropertySymbol prop, AttributeData attr)
    {
        string? default_command = null;
        foreach (var named in attr.NamedArguments)
            if (named is { Key: "DefaultCommand", Value.Value: string dc })
                default_command = dc;

        var enumType = (INamedTypeSymbol)prop.Type;
        var sub_commands = new List<SubCommandEntry>();

        foreach (var member in enumType.GetMembers())
        {
            if (member is not IFieldSymbol field || field.IsImplicitlyDeclared) continue;

            var sub_name = to_kebab_case(member.Name);
            var sub_description = get_summary_comment(member);

            sub_commands.Add(new SubCommandEntry(
                member.Name,
                sub_name,
                sub_description,
                null,
                false,
                sub_name));
        }

        return new SubCommandInfo(
            prop.Name,
            enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            default_command,
            sub_commands);
    }

    /// <summary>
    ///     生成所有命令类型的源代码。
    /// </summary>
    private static void generate_source(SourceProductionContext context, ImmutableArray<CommandInfo> commands)
    {
        foreach (var cmd in commands)
        {
            var source = generate_command_source(cmd);
            var hintName = $"{cmd.type_name}.Command.g.cs";
            context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个命令类型的完整源代码。
    /// </summary>
    private static string generate_command_source(CommandInfo cmd)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();

        if (!string.IsNullOrEmpty(cmd.namespace_name))
        {
            sb.append_line($"namespace {cmd.namespace_name};");
            sb.append_line();
        }

        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{cmd.type_name}</c> 的命令行解析器生成代码。");
        sb.append_line("/// </summary>");
        sb.append_line($"public partial class {cmd.type_name}");
        using (sb.block())
        {
            generate_parse_methods(sb, cmd);
            sb.append_line();
            generate_try_parse_method(sb, cmd);
            sb.append_line();
            generate_get_help_text(sb, cmd);
            sb.append_line();
            generate_get_json_schema(sb, cmd);
            sb.append_line();
            generate_get_completion_script(sb, cmd);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成 <c>Parse</c> 方法。
    /// </summary>
    private static void generate_parse_methods(SourceTextBuilder sb, CommandInfo cmd)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 解析命令行参数并返回命令实例。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"args\">命令行参数数组。</param>");
        sb.append_line("/// <returns>填充好的命令实例。</returns>");
        sb.append_line($"public static {cmd.fully_qualified_name} Parse(string[] args)");
        using (sb.block())
        {
            sb.append_line("if (TryParse(args, out var command, out var error))");
            using (sb.block())
            {
                sb.append_line("return command;");
            }

            sb.append_line("throw new global::Sonic.Standard.Command.CommandParseException(error!);");
        }
    }

    /// <summary>
    ///     生成 <c>TryParse</c> 方法。
    /// </summary>
    private static void generate_try_parse_method(SourceTextBuilder sb, CommandInfo cmd)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 尝试解析命令行参数，不抛出异常。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"args\">命令行参数数组。</param>");
        sb.append_line("/// <param name=\"command\">解析成功的命令实例。</param>");
        sb.append_line("/// <param name=\"error\">解析失败的错误信息。</param>");
        sb.append_line("/// <returns>是否解析成功。</returns>");
        sb.append_line(
            $"public static bool TryParse(string[] args, out {cmd.fully_qualified_name}? command, out string? error)");
        using (sb.block())
        {
            sb.append_line("command = null;");
            sb.append_line("error = null;");
            sb.append_line($"var result = new {cmd.fully_qualified_name}();");
            sb.append_line("var errors = new global::System.Collections.Generic.List<string>();");
            sb.append_line("var positionalIndex = 0;");
            sb.append_line("var i = 0;");
            sb.append_line();
            sb.append_line("while (i < args.Length)");
            using (sb.block())
            {
                sb.append_line("var arg = args[i];");
                sb.append_line();
                sb.append_line("if (arg == \"--help\" || arg == \"-h\")");
                using (sb.block())
                {
                    sb.append_line("error = result.GetHelpText();");
                    sb.append_line("return false;");
                }

                sb.append_line();

                if (cmd.version is not null)
                {
                    sb.append_line("if (arg == \"--version\")");
                    using (sb.block())
                    {
                        sb.append_line($"error = \"{cmd.version}\";");
                        sb.append_line("return false;");
                    }

                    sb.append_line();
                }

                if (cmd.sub_command is not null)
                {
                    sb.append_line("if (!arg.StartsWith(\"-\"))");
                    using (sb.block())
                    {
                        generate_sub_command_dispatch(sb, cmd);
                        sb.append_line("continue;");
                    }

                    sb.append_line();
                }

                sb.append_line("if (arg.StartsWith(\"--\"))");
                using (sb.block())
                {
                    generate_long_option_parsing(sb, cmd);
                }

                sb.append_line("else if (arg.StartsWith(\"-\") && arg.Length == 2)");
                using (sb.block())
                {
                    generate_short_option_parsing(sb, cmd);
                }

                sb.append_line("else");
                using (sb.block())
                {
                    generate_positional_argument_parsing(sb, cmd);
                }
            }

            sb.append_line();

            generate_environment_variable_overrides(sb, cmd);
            sb.append_line();

            generate_required_validation(sb, cmd);
            sb.append_line();

            generate_after_parse_calls(sb, cmd);
            sb.append_line();

            sb.append_line("if (errors.Count > 0)");
            using (sb.block())
            {
                sb.append_line("error = string.Join(\"; \", errors);");
                sb.append_line("return false;");
            }

            sb.append_line();
            sb.append_line("command = result;");
            sb.append_line("return true;");
        }
    }

    /// <summary>
    ///     生成子命令分发逻辑。
    /// </summary>
    private static void generate_sub_command_dispatch(SourceTextBuilder sb, CommandInfo cmd)
    {
        var sub = cmd.sub_command!.Value;
        sb.append_line("switch (arg)");
        using (sb.block())
        {
            foreach (var entry in sub.entries)
            {
                if (entry.hide) continue;

                sb.append_line($"case \"{entry.name}\":");
                using (sb.block())
                {
                    sb.append_line(
                        $"result.{sub.property_name} = ({sub.enum_full_name})global::System.Enum.Parse(typeof({sub.enum_full_name}), \"{entry.enum_member_name}\");");
                    sb.append_line("i = args.Length;");
                    sb.append_line("break;");
                }
            }

            sb.append_line("default:");
            using (sb.block())
            {
                sb.append_line("errors.Add($\"未知子命令: {arg}\");");
                sb.append_line("break;");
            }
        }
    }

    /// <summary>
    ///     生成长选项解析逻辑。
    /// </summary>
    private static void generate_long_option_parsing(SourceTextBuilder sb, CommandInfo cmd)
    {
        sb.append_line("var longName = arg.Substring(2);");
        sb.append_line("var equalPos = longName.IndexOf('=');");
        sb.append_line("string? value = null;");
        sb.append_line();
        sb.append_line("if (equalPos >= 0)");
        using (sb.block())
        {
            sb.append_line("value = longName.Substring(equalPos + 1);");
            sb.append_line("longName = longName.Substring(0, equalPos);");
        }

        sb.append_line();

        foreach (var opt in cmd.options)
        {
            if (opt.hide) continue;

            sb.append_line($"if (longName == \"{opt.long_name}\")");
            using (sb.block())
            {
                if (opt.takes_value)
                {
                    sb.append_line("if (value is null && i + 1 < args.Length)");
                    using (sb.block())
                    {
                        sb.append_line("i++;");
                        sb.append_line("value = args[i];");
                    }

                    sb.append_line("if (value is null)");
                    using (sb.block())
                    {
                        sb.append_line($"errors.Add(\"选项 --{opt.long_name} 需要一个值\");");
                    }

                    sb.append_line("else");
                    using (sb.block())
                    {
                        sb.append_line(
                            $"result.{opt.property_name} = global::Sonic.Standard.Command.CommandParser.convert_value<{opt.property_type}>(value);");
                    }
                }
                else
                {
                    sb.append_line($"result.{opt.property_name} = true;");
                }

                sb.append_line("continue;");
            }
        }

        sb.append_line("errors.Add($\"未知选项: --{longName}\");");
    }

    /// <summary>
    ///     生成短选项解析逻辑。
    /// </summary>
    private static void generate_short_option_parsing(SourceTextBuilder sb, CommandInfo cmd)
    {
        foreach (var opt in cmd.options)
        {
            if (opt.hide || opt.short_name == '\0') continue;

            sb.append_line($"if (arg[1] == '{opt.short_name}')");
            using (sb.block())
            {
                if (opt.takes_value)
                {
                    sb.append_line("if (i + 1 < args.Length)");
                    using (sb.block())
                    {
                        sb.append_line("i++;");
                        sb.append_line(
                            $"result.{opt.property_name} = global::Sonic.Standard.Command.CommandParser.convert_value<{opt.property_type}>(args[i]);");
                    }

                    sb.append_line("else");
                    using (sb.block())
                    {
                        sb.append_line($"errors.Add(\"选项 -{opt.short_name} 需要一个值\");");
                    }
                }
                else
                {
                    sb.append_line($"result.{opt.property_name} = true;");
                }

                sb.append_line("continue;");
            }
        }

        sb.append_line("errors.Add($\"未知选项: -{arg[1]}\");");
    }

    /// <summary>
    ///     生成位置参数解析逻辑。
    /// </summary>
    private static void generate_positional_argument_parsing(SourceTextBuilder sb, CommandInfo cmd)
    {
        var sorted_args = cmd.arguments.OrderBy(a => a.position).ToList();
        sb.append_line("switch (positionalIndex)");
        using (sb.block())
        {
            for (var idx = 0; idx < sorted_args.Count; idx++)
            {
                var arg = sorted_args[idx];
                sb.append_line($"case {idx}:");
                using (sb.block())
                {
                    sb.append_line(
                        $"result.{arg.property_name} = global::Sonic.Standard.Command.CommandParser.convert_value<{arg.property_type}>(arg);");
                    sb.append_line("positionalIndex++;");
                    sb.append_line("break;");
                }
            }

            sb.append_line("default:");
            using (sb.block())
            {
                sb.append_line("errors.Add($\"多余的位置参数: {arg}\");");
                sb.append_line("break;");
            }
        }
    }

    /// <summary>
    ///     生成环境变量覆盖逻辑。
    /// </summary>
    private static void generate_environment_variable_overrides(SourceTextBuilder sb, CommandInfo cmd)
    {
        foreach (var opt in cmd.options)
        {
            if (opt.env is null) continue;

            var env_name = cmd.env_prefix is not null ? $"{cmd.env_prefix}{opt.env}" : opt.env;
            sb.append_line(
                $"var {opt.property_name}_env = global::System.Environment.GetEnvironmentVariable(\"{env_name}\");");

            var short_check = opt.short_name != '\0'
                ? $" || a == \"-{opt.short_name}\""
                : "";
            sb.append_line(
                $"if ({opt.property_name}_env is not null && !global::System.Linq.Enumerable.Any(args, a => a == \"--{opt.long_name}\"{short_check}))");
            using (sb.block())
            {
                sb.append_line(
                    $"result.{opt.property_name} = global::Sonic.Standard.Command.CommandParser.convert_value<{opt.property_type}>({opt.property_name}_env);");
            }
        }
    }

    /// <summary>
    ///     生成必填参数验证逻辑。
    /// </summary>
    private static void generate_required_validation(SourceTextBuilder sb, CommandInfo cmd)
    {
        foreach (var arg in cmd.arguments)
        {
            if (!arg.required) continue;

            var check = arg.property_type.Contains("string")
                ? $"string.IsNullOrEmpty(result.{arg.property_name})"
                : $"result.{arg.property_name} == default";
            sb.append_line($"if ({check})");
            using (sb.block())
            {
                sb.append_line($"errors.Add(\"缺少必需参数: {arg.value_name}\");");
            }
        }

        foreach (var opt in cmd.options)
        {
            if (!opt.required) continue;

            var check = opt.property_type.Contains("string")
                ? $"string.IsNullOrEmpty(result.{opt.property_name})"
                : $"result.{opt.property_name} == default";
            sb.append_line($"if ({check})");
            using (sb.block())
            {
                sb.append_line($"errors.Add(\"缺少必需选项: --{opt.long_name}\");");
            }
        }
    }

    /// <summary>
    ///     生成 AfterParse 方法调用逻辑。
    /// </summary>
    private static void generate_after_parse_calls(SourceTextBuilder sb, CommandInfo cmd)
    {
        foreach (var method in cmd.after_parse_methods) sb.append_line($"result.{method}();");
    }

    /// <summary>
    ///     生成 <c>GetHelpText</c> 方法。
    /// </summary>
    private static void generate_get_help_text(SourceTextBuilder sb, CommandInfo cmd)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 生成格式化的帮助文本。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <returns>帮助文本字符串。</returns>");
        sb.append_line("public string GetHelpText()");
        using (sb.block())
        {
            sb.append_line("var builder = new global::Sonic.Standard.Command.HelpBuilder();");
            sb.append_line($"builder.append_header(\"{cmd.name}\", \"{cmd.version ?? "1.0.0"}\");");

            if (cmd.description is not null)
                sb.append_line(
                    $"builder.append_description(\"{StringEscapeHelper.escape_for_string(cmd.description)}\");");

            if (cmd.long_description is not null)
                sb.append_line(
                    $"builder.AppendDescription(\"{StringEscapeHelper.escape_for_string(cmd.long_description)}\");");

            var usage_parts = new List<string> { "[OPTIONS]" };
            if (cmd.sub_command is not null) usage_parts.Add("<COMMAND>");

            foreach (var arg in cmd.arguments.OrderBy(a => a.position))
                usage_parts.Add(arg.required ? $"<{arg.value_name}>" : $"[{arg.value_name}]");

            sb.append_line($"builder.append_usage(\"{cmd.name}\", null, \"{string.Join(" ", usage_parts)}\");");

            if (cmd.arguments.Count > 0)
            {
                sb.append_line(
                    "builder.append_arguments(new global::System.Collections.Generic.List<(string, string?, bool)>");
                using (sb.block())
                {
                    foreach (var arg in cmd.arguments.OrderBy(a => a.position))
                    {
                        var req_str = arg.required ? "true" : "false";
                        sb.append_line(
                            $"(\"{arg.value_name}\", \"{StringEscapeHelper.escape_for_string(arg.description ?? "")}\", {req_str}),");
                    }
                }

                sb.append_line(");");
            }

            if (cmd.options.Count > 0)
            {
                sb.append_line(
                    "builder.append_options(new global::System.Collections.Generic.List<(char, string, string?)>");
                using (sb.block())
                {
                    foreach (var opt in cmd.options)
                    {
                        if (opt.hide) continue;

                        var short_char = opt.short_name != '\0' ? $"'{opt.short_name}'" : "'\\0'";
                        sb.append_line(
                            $"({short_char}, \"{opt.long_name}\", \"{StringEscapeHelper.escape_for_string(opt.description ?? "")}\"),");
                    }
                }

                sb.append_line(");");
            }

            if (cmd.sub_command is not null && cmd.sub_command.Value.entries.Count > 0)
            {
                sb.append_line(
                    "builder.append_sub_commands(new global::System.Collections.Generic.List<(string, string?, string[]?)>");
                using (sb.block())
                {
                    foreach (var entry in cmd.sub_command.Value.entries)
                    {
                        if (entry.hide) continue;

                        var aliases_expr = entry.aliases is not null
                            ? $"new string[] {{ {string.Join(", ", entry.aliases.Select(a => $"\"{a}\""))} }}"
                            : "null";
                        sb.append_line(
                            $"(\"{entry.name}\", \"{StringEscapeHelper.escape_for_string(entry.description ?? "")}\", {aliases_expr}),");
                    }
                }

                sb.append_line(");");
            }

            var env_options = cmd.options.Where(o => o.env is not null).ToList();
            if (env_options.Count > 0)
            {
                sb.append_line(
                    "builder.append_environment_variables(new global::System.Collections.Generic.List<(string, string?)>");
                using (sb.block())
                {
                    foreach (var opt in env_options)
                    {
                        var env_name = cmd.env_prefix is not null ? $"{cmd.env_prefix}{opt.env}" : opt.env;
                        sb.append_line($"(\"--{opt.long_name}\", \"{env_name}\"),");
                    }
                }

                sb.append_line(");");
            }

            sb.append_line("return builder.ToString();");
        }
    }

    /// <summary>
    ///     生成 <c>GetJsonSchema</c> 方法。
    /// </summary>
    private static void generate_get_json_schema(SourceTextBuilder sb, CommandInfo cmd)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 生成符合 JSON Schema Draft 2020-12 的命令参数描述。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <returns>JSON Schema 字符串。</returns>");
        sb.append_line("public static string GetJsonSchema()");
        using (sb.block())
        {
            sb.append_line("var sb = new global::System.Text.StringBuilder();");
            sb.append_line(
                "sb.Append(\"{\\\"$schema\\\":\\\"https://json-schema.org/draft/2020-12/schema\\\",\\\"type\\\":\\\"object\\\",\\\"properties\\\":{\");");

            var all_props =
                new List<(string key, string Type, object? default_value, bool required, string? description)>();
            foreach (var arg in cmd.arguments)
                all_props.Add((arg.value_name, map_type_to_json_schema(arg.property_type), arg.default_value,
                    arg.required, arg.description));

            foreach (var opt in cmd.options)
                all_props.Add((opt.long_name, map_type_to_json_schema(opt.property_type), opt.default_value,
                    opt.required, opt.description));

            for (var idx = 0; idx < all_props.Count; idx++)
            {
                var prop = all_props[idx];
                if (idx > 0) sb.append_line("sb.Append(\",\");");

                sb.append_line($"sb.Append(\"\\\"{prop.key}\\\":{{\\\"type\\\":\\\"{prop.Type}\\\"\");");

                if (prop.default_value is not null)
                {
                    var default_str = prop.default_value is string s
                        ? $"\\\"{StringEscapeHelper.escape_for_string(s)}\\\""
                        : prop.default_value.ToString()?.ToLowerInvariant();
                    sb.append_line($"sb.Append(\",\\\"default\\\":{default_str}\");");
                }

                if (prop.description is not null)
                    sb.append_line(
                        $"sb.Append(\",\\\"description\\\":\\\"{StringEscapeHelper.escape_for_string(prop.description)}\\\"\");");

                sb.append_line("sb.Append(\"}\");");
            }

            sb.append_line("sb.Append(\"}\");");

            var required_props = all_props
                .Where(p => p.required)
                .Select(p => $"\\\"{p.key}\\\"")
                .ToList();
            if (required_props.Count > 0)
                sb.append_line($"sb.Append(\",\\\"required\\\":[{string.Join(",", required_props)}]\");");

            sb.append_line("sb.Append(\"}\");");
            sb.append_line("return sb.ToString();");
        }
    }

    /// <summary>
    ///     生成 <c>GetCompletionScript</c> 方法及各 Shell 的补全脚本生成器。
    /// </summary>
    private static void generate_get_completion_script(SourceTextBuilder sb, CommandInfo cmd)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 生成 Shell 补全脚本。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"shell\">目标 Shell 类型。</param>");
        sb.append_line("/// <returns>补全脚本字符串。</returns>");
        sb.append_line("public static string GetCompletionScript(global::Sonic.Standard.Command.ShellType shell)");
        using (sb.block())
        {
            sb.append_line("return shell switch");
            using (sb.block())
            {
                sb.append_line(
                    $"global::Sonic.Standard.Command.ShellType.bash => GenerateBashCompletion(\"{cmd.name}\"),");
                sb.append_line(
                    $"global::Sonic.Standard.Command.ShellType.zsh => GenerateZshCompletion(\"{cmd.name}\"),");
                sb.append_line(
                    $"global::Sonic.Standard.Command.ShellType.fish => GenerateFishCompletion(\"{cmd.name}\"),");
                sb.append_line(
                    $"global::Sonic.Standard.Command.ShellType.power_shell => GeneratePowerShellCompletion(\"{cmd.name}\"),");
                sb.append_line("_ => \"\"");
            }

            sb.append_line(";");
        }

        sb.append_line();
        generate_bash_completion(sb, cmd);
        sb.append_line();
        generate_zsh_completion(sb, cmd);
        sb.append_line();
        generate_fish_completion(sb, cmd);
        sb.append_line();
        generate_power_shell_completion(sb, cmd);
    }

    /// <summary>
    ///     生成 Bash 补全脚本生成方法。
    /// </summary>
    private static void generate_bash_completion(SourceTextBuilder sb, CommandInfo cmd)
    {
        sb.append_line("private static string GenerateBashCompletion(string appName)");
        using (sb.block())
        {
            sb.append_line("var sb = new global::System.Text.StringBuilder();");
            sb.append_line("sb.AppendLine(\"_\" + appName + \"()\");");
            sb.append_line("sb.AppendLine(\"{\");");
            sb.append_line("sb.AppendLine(\"    local cur=${COMP_WORDS[COMP_CWORD]}\");");
            sb.append_line("sb.AppendLine(\"    local opts=\\\"\\\"\");");

            foreach (var opt in cmd.options.Where(o => !o.hide))
                sb.append_line("sb.AppendLine(\"    opts=\\\"\" + \"$opts --" + opt.long_name + "\\\"\");");

            if (cmd.sub_command is not null)
                foreach (var entry in cmd.sub_command.Value.entries.Where(e => !e.hide))
                    sb.append_line("sb.AppendLine(\"    opts=\\\"\" + \"$opts " + entry.name + "\\\"\");");

            sb.append_line(
                "sb.AppendLine(\"    COMPREPLY=($(compgen -W \\\"\" + \"$opts\" + \"\\\" -- \" + \"$cur))\");");
            sb.append_line("sb.AppendLine(\"}\");");
            sb.append_line("sb.AppendLine(\"complete -F _\" + appName + \" \" + appName);");
            sb.append_line("return sb.ToString();");
        }
    }

    /// <summary>
    ///     生成 Zsh 补全脚本生成方法。
    /// </summary>
    private static void generate_zsh_completion(SourceTextBuilder sb, CommandInfo cmd)
    {
        sb.append_line("private static string GenerateZshCompletion(string appName)");
        using (sb.block())
        {
            sb.append_line("var sb = new global::System.Text.StringBuilder();");
            sb.append_line("sb.AppendLine(\"#compdef \" + appName);");
            sb.append_line("sb.AppendLine(\"_\" + appName + \"()\");");
            sb.append_line("sb.AppendLine(\"{\");");
            sb.append_line("sb.AppendLine(\"    _arguments \\\\\");");

            foreach (var opt in cmd.options.Where(o => !o.hide))
            {
                var parts = new List<string>();
                if (opt.short_name != '\0') parts.Add($"-'{opt.short_name}'");

                parts.Add(
                    $"'--{opt.long_name}={StringEscapeHelper.escape_for_string(opt.description ?? opt.long_name)}'");
                var line = string.Join(" ", parts);
                sb.append_line($"sb.AppendLine(\"        {line} \\\\\");");
            }

            sb.append_line("sb.AppendLine(\"    && return 0\");");
            sb.append_line("sb.AppendLine(\"}\");");
            sb.append_line("sb.AppendLine(\"_\" + appName + \" \\\"$@\\\"\");");
            sb.append_line("return sb.ToString();");
        }
    }

    /// <summary>
    ///     生成 Fish 补全脚本生成方法。
    /// </summary>
    private static void generate_fish_completion(SourceTextBuilder sb, CommandInfo cmd)
    {
        sb.append_line("private static string GenerateFishCompletion(string appName)");
        using (sb.block())
        {
            sb.append_line("var sb = new global::System.Text.StringBuilder();");

            foreach (var opt in cmd.options.Where(o => !o.hide))
            {
                var shortOpt = opt.short_name != '\0' ? $" -s {opt.short_name}" : "";
                var longOpt = $" -l {opt.long_name}";
                var desc = StringEscapeHelper.escape_for_string(opt.description ?? opt.long_name);
                sb.append_line(
                    $"sb.AppendLine($\"complete -c {{appName}}{shortOpt}{longOpt} -d '{desc}'\");");
            }

            if (cmd.sub_command is not null)
                foreach (var entry in cmd.sub_command.Value.entries.Where(e => !e.hide))
                {
                    var entryDesc = StringEscapeHelper.escape_for_string(entry.description ?? "");
                    sb.append_line(
                        $"sb.AppendLine($\"complete -c {{appName}} -n '__fish_use_subcommand' -a '{entry.name}' -d '{entryDesc}'\");");
                }

            sb.append_line("return sb.ToString();");
        }
    }

    /// <summary>
    ///     生成 PowerShell 补全脚本生成方法。
    /// </summary>
    private static void generate_power_shell_completion(SourceTextBuilder sb, CommandInfo cmd)
    {
        sb.append_line("private static string GeneratePowerShellCompletion(string appName)");
        using (sb.block())
        {
            sb.append_line("var sb = new global::System.Text.StringBuilder();");
            sb.append_line(
                "sb.AppendLine(\"Register-ArgumentCompleter -CommandName \" + appName + \" -ScriptBlock {\");");
            sb.append_line("sb.AppendLine(\"    param($commandName, $wordToComplete, $cursorPosition)\");");
            sb.append_line("sb.AppendLine(\"    $completions = @(\");");

            foreach (var opt in cmd.options.Where(o => !o.hide))
                sb.append_line($"sb.AppendLine(\"        '--{opt.long_name}'\");");

            if (cmd.sub_command is not null)
                foreach (var entry in cmd.sub_command.Value.entries.Where(e => !e.hide))
                    sb.append_line($"sb.AppendLine(\"        '{entry.name}'\");");

            sb.append_line("sb.AppendLine(\"    )\");");
            sb.append_line(
                "sb.AppendLine(\"    $completions | Where-Object { $_ -like \\\"$wordToComplete*\\\" } | ForEach-Object { [System.Management.Automation.CompletionResult]::new($_) }\");");
            sb.append_line("sb.AppendLine(\"}\");");
            sb.append_line("return sb.ToString();");
        }
    }

    /// <summary>
    ///     将 C# 类型映射到 JSON Schema 类型字符串。
    /// </summary>
    private static string map_type_to_json_schema(string type)
    {
        if (type.Contains("Int32") || type.Contains("Int64") || type.Contains("int") || type.Contains("long"))
            return "integer";

        if (type.Contains("Double") || type.Contains("Single") || type.Contains("Float") || type.Contains("double") ||
            type.Contains("float"))
            return "number";

        if (type.Contains("Boolean") || type.Contains("bool")) return "boolean";

        return "string";
    }
}
