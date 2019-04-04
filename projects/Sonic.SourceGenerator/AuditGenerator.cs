using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[Auditable]</c> 特性的方法自动生成审计日志包装。
/// </summary>
[Generator]
public sealed class AuditGenerator : IIncrementalGenerator
{
    private const string _auditable_attribute_full_name = "Sonic.Standard.Security.Audit.AuditableAttribute";

    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targetTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _auditable_attribute_full_name,
                static (node, _) => node is TypeDeclarationSyntax typeDecl &&
                                    typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, ct) => transform_auditable(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(targetTypes, generate_source);
    }

    /// <summary>
    ///     提取标记了 <c>[Auditable]</c> 的类型和方法信息。
    /// </summary>
    private static AuditTypeInfo? transform_auditable(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var classCategory = extract_class_category(typeSymbol);

        var methods = extract_auditable_methods(typeSymbol, classCategory);

        if (methods.Count == 0 && classCategory is null) return null;

        return new AuditTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            typeSymbol.TypeKind == TypeKind.Struct,
            methods);
    }

    /// <summary>
    ///     提取类级别 <c>[Auditable]</c> 特性的审计分类。
    /// </summary>
    private static string? extract_class_category(INamedTypeSymbol typeSymbol)
    {
        var classAttr = typeSymbol.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_auditable_attribute_full_name}");

        if (classAttr is null) return null;

        foreach (var named in classAttr.NamedArguments)
            if (named is { Key: "category", Value.Value: string c })
                return c;

        return typeSymbol.Name;
    }

    /// <summary>
    ///     提取标记了 <c>[Auditable]</c> 的方法信息。
    /// </summary>
    private static List<AuditMethodInfo> extract_auditable_methods(INamedTypeSymbol typeSymbol, string? classCategory)
    {
        var methods = new List<AuditMethodInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;

            if (method.IsStatic) continue;

            if (method.DeclaredAccessibility != Accessibility.Public) continue;

            var methodAttr = method.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_auditable_attribute_full_name}");

            if (methodAttr is null && classCategory is null) continue;

            var category = classCategory;

            if (methodAttr is not null)
                foreach (var named in methodAttr.NamedArguments)
                    if (named is { Key: "category", Value.Value: string c })
                        category = c;

            var parameters = method.Parameters
                .Select(p => new AuditParameterInfo(
                    p.Name,
                    p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    p.RefKind))
                .ToList();

            methods.Add(new AuditMethodInfo(
                method.Name,
                method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                method.ReturnsVoid,
                is_task(method.ReturnType),
                parameters,
                category));
        }

        return methods;
    }

    /// <summary>
    ///     判断类型是否为 <c>System.Threading.Tasks.Task</c>。
    /// </summary>
    private static bool is_task(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
               "global::System.Threading.Tasks.Task" ||
               type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).StartsWith(
                   "global::System.Threading.Tasks.Task<");
    }

    /// <summary>
    ///     生成所有类型的审计日志包装源代码。
    /// </summary>
    private static void generate_source(SourceProductionContext context, ImmutableArray<AuditTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_audit_source(info);
            var hintName = $"{info.type_name}.Audit.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个类型的审计日志包装源代码。
    /// </summary>
    private static string generate_audit_source(AuditTypeInfo info)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();

        if (!string.IsNullOrEmpty(info.namespace_name))
        {
            sb.append_line($"namespace {info.namespace_name};");
            sb.append_line();
        }

        var typeKeyword = info.is_struct ? "partial struct" : "partial class";
        sb.append_line($"{typeKeyword} {info.type_name}");
        using (sb.block())
        {
            foreach (var method in info.methods)
            {
                generate_audit_wrapper(sb, info, method);
                sb.append_line();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     为单个方法生成审计日志包装方法。
    /// </summary>
    private static void generate_audit_wrapper(SourceTextBuilder sb, AuditTypeInfo info, AuditMethodInfo method)
    {
        var paramList = string.Join(", ", method.parameters.Select(p => format_parameter(p)));
        var argList = string.Join(", ", method.parameters.Select(p => format_argument(p)));

        sb.append_line("/// <summary>");
        sb.append_line($"/// 带审计日志的 <c>{method.method_name}</c> 包装方法。");
        sb.append_line("/// </summary>");

        foreach (var p in method.parameters) sb.append_line($"/// <param name=\"{p.name}\">{p.name} 参数。</param>");

        if (method.is_async)
        {
            sb.append_line(
                $"public async global::System.Threading.Tasks.Task {method.method_name}_audited({paramList})");
            using (sb.block())
            {
                emit_audit_before(sb, method);
                sb.append_line($"await {method.method_name}({argList});");
                sb.append_line();
                emit_audit_after(sb, method);
            }
        }
        else
        {
            var returnType = method.is_void ? "void" : method.return_type;
            sb.append_line($"public {returnType} {method.method_name}_audited({paramList})");
            using (sb.block())
            {
                emit_audit_before(sb, method);

                if (method.is_void)
                    sb.append_line($"{method.method_name}({argList});");
                else
                    sb.append_line($"var __result = {method.method_name}({argList});");

                sb.append_line();
                emit_audit_after(sb, method);

                if (!method.is_void) sb.append_line("return __result;");
            }
        }
    }

    /// <summary>
    ///     生成审计日志的前置记录代码。
    /// </summary>
    private static void emit_audit_before(SourceTextBuilder sb, AuditMethodInfo method)
    {
        var categoryExpr = method.category is not null ? $"\"{method.category}\"" : "null";
        sb.append_line($"var __auditAction = $\"{method.method_name}\";");
        sb.append_line($"var __auditCategory = {categoryExpr};");
        sb.append_line("var __auditTimestamp = global::System.DateTimeOffset.UtcNow;");
        sb.append_line(
            "var __auditUserId = global::Sonic.Standard.Security.Authentication.AuthContext.current_user_id() ?? \"anonymous\";");
    }

    /// <summary>
    ///     生成审计日志的后置写入代码。
    /// </summary>
    private static void emit_audit_after(SourceTextBuilder sb, AuditMethodInfo method)
    {
        sb.append_line(
            "var __entry = new global::Sonic.Standard.Security.Audit.AuditEntry(__auditAction, __auditTimestamp, __auditUserId, __auditCategory);");
        sb.append_line("global::Sonic.Standard.Security.Audit.AuditContext.write(__entry);");
    }

    /// <summary>
    ///     格式化参数声明。
    /// </summary>
    private static string format_parameter(AuditParameterInfo param)
    {
        var prefix = param.ref_kind switch
        {
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            RefKind.In => "in ",
            _ => ""
        };

        return $"{prefix}{param.Type} {param.name}";
    }

    /// <summary>
    ///     格式化参数调用。
    /// </summary>
    private static string format_argument(AuditParameterInfo param)
    {
        var prefix = param.ref_kind switch
        {
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            RefKind.In => "in ",
            _ => ""
        };

        return $"{prefix}{param.name}";
    }

    #region 数据模型

    /// <summary>
    ///     包含审计方法的类型信息。
    /// </summary>
    internal readonly struct AuditTypeInfo
    {
        /// <summary>
        ///     类型名称。
        /// </summary>
        public readonly string type_name;

        /// <summary>
        ///     完全限定类型名称。
        /// </summary>
        public readonly string fully_qualified_name;

        /// <summary>
        ///     命名空间名称。
        /// </summary>
        public readonly string? namespace_name;

        /// <summary>
        ///     是否为结构体。
        /// </summary>
        public readonly bool is_struct;

        /// <summary>
        ///     需要审计日志的方法列表。
        /// </summary>
        public readonly List<AuditMethodInfo> methods;

        /// <summary>
        ///     初始化 <see cref="AuditTypeInfo" /> 的新实例。
        /// </summary>
        public AuditTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            bool isStruct,
            List<AuditMethodInfo> methods)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            is_struct = isStruct;
            this.methods = methods;
        }
    }

    /// <summary>
    ///     需要审计日志的方法信息。
    /// </summary>
    internal readonly struct AuditMethodInfo
    {
        /// <summary>
        ///     方法名称。
        /// </summary>
        public readonly string method_name;

        /// <summary>
        ///     返回类型的完全限定名称。
        /// </summary>
        public readonly string return_type;

        /// <summary>
        ///     是否为 void 返回类型。
        /// </summary>
        public readonly bool is_void;

        /// <summary>
        ///     是否为异步方法（返回 Task）。
        /// </summary>
        public readonly bool is_async;

        /// <summary>
        ///     参数列表。
        /// </summary>
        public readonly List<AuditParameterInfo> parameters;

        /// <summary>
        ///     审计分类。
        /// </summary>
        public readonly string? category;

        /// <summary>
        ///     初始化 <see cref="AuditMethodInfo" /> 的新实例。
        /// </summary>
        public AuditMethodInfo(
            string methodName,
            string returnType,
            bool isVoid,
            bool isAsync,
            List<AuditParameterInfo> parameters,
            string? category)
        {
            method_name = methodName;
            return_type = returnType;
            is_void = isVoid;
            is_async = isAsync;
            this.parameters = parameters;
            this.category = category;
        }
    }

    /// <summary>
    ///     方法参数信息。
    /// </summary>
    internal readonly struct AuditParameterInfo
    {
        /// <summary>
        ///     参数名称。
        /// </summary>
        public readonly string name;

        /// <summary>
        ///     参数类型的完全限定名称。
        /// </summary>
        public readonly string Type;

        /// <summary>
        ///     参数传递方式。
        /// </summary>
        public readonly RefKind ref_kind;

        /// <summary>
        ///     初始化 <see cref="AuditParameterInfo" /> 的新实例。
        /// </summary>
        public AuditParameterInfo(string name, string type, RefKind refKind)
        {
            this.name = name;
            Type = type;
            ref_kind = refKind;
        }
    }

    #endregion
}