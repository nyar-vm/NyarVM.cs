using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[Cache]</c> 或 <c>[EvictCache]</c> 特性的方法自动生成缓存包装代码，
///     实现方法级缓存和缓存失效逻辑。
/// </summary>
[Generator]
public sealed class CacheGenerator : IIncrementalGenerator
{
    private const string _cache_attribute_full_name = "Sonic.Standard.Data.Cache.CacheAttribute";
    private const string _cache_key_attribute_full_name = "Sonic.Standard.Data.Cache.CacheKeyAttribute";
    private const string _evict_cache_attribute_full_name = "Sonic.Standard.Data.Cache.EvictCacheAttribute";

    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var cacheMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _cache_attribute_full_name,
                static (node, _) => node is MethodDeclarationSyntax,
                static (ctx, ct) => transform_cache_method(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        var evictMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _evict_cache_attribute_full_name,
                static (node, _) => node is MethodDeclarationSyntax,
                static (ctx, ct) => transform_evict_method(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(cacheMethods, generate_cache_source);
        context.RegisterSourceOutput(evictMethods, generate_evict_source);
    }

    /// <summary>
    ///     提取标记了 <c>[Cache]</c> 特性的方法信息。
    /// </summary>
    private static CacheMethodInfo? transform_cache_method(GeneratorAttributeSyntaxContext context,
        CancellationToken ct)
    {
        var methodSymbol = context.TargetSymbol as IMethodSymbol;

        var containingType = methodSymbol?.ContainingType;
        if (containingType is null) return null;

        var cacheAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_cache_attribute_full_name}");

        if (cacheAttr is null) return null;

        var duration = 300;
        string? key = null;

        foreach (var named in cacheAttr.NamedArguments)
        {
            if (named is { Key: "duration", Value.Value: int d }) duration = d;

            if (named is { Key: "key", Value.Value: string k }) key = k;
        }

        var keyParameters = new List<CacheKeyParameterInfo>();

        foreach (var param in methodSymbol.Parameters)
        {
            var hasCacheKey = param.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_cache_key_attribute_full_name}");

            if (hasCacheKey)
                keyParameters.Add(new CacheKeyParameterInfo(
                    param.Name,
                    param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        if (keyParameters.Count == 0)
            foreach (var param in methodSymbol.Parameters)
                keyParameters.Add(new CacheKeyParameterInfo(
                    param.Name,
                    param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

        var returnType = methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var isAsync = methodSymbol.ReturnType is INamedTypeSymbol { Name: "Task" or "ValueTask" };

        return new CacheMethodInfo(
            containingType.Name,
            containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            containingType.ContainingNamespace?.ToDisplayString(),
            methodSymbol.Name,
            returnType,
            isAsync,
            duration,
            key,
            keyParameters,
            extract_parameters(methodSymbol));
    }

    /// <summary>
    ///     提取标记了 <c>[EvictCache]</c> 特性的方法信息。
    /// </summary>
    private static EvictMethodInfo? transform_evict_method(GeneratorAttributeSyntaxContext context,
        CancellationToken ct)
    {
        var methodSymbol = context.TargetSymbol as IMethodSymbol;

        var containingType = methodSymbol?.ContainingType;
        if (containingType is null) return null;

        var evictAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_evict_cache_attribute_full_name}");

        if (evictAttr is null) return null;

        string? key = null;

        foreach (var named in evictAttr.NamedArguments)
            if (named is { Key: "key", Value.Value: string k })
                key = k;

        return new EvictMethodInfo(
            containingType.Name,
            containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            containingType.ContainingNamespace?.ToDisplayString(),
            methodSymbol.Name,
            methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            key,
            extract_parameters(methodSymbol));
    }

    /// <summary>
    ///     提取方法的参数信息列表。
    /// </summary>
    private static List<ParameterInfo> extract_parameters(IMethodSymbol methodSymbol)
    {
        var parameters = new List<ParameterInfo>();

        foreach (var param in methodSymbol.Parameters)
            parameters.Add(new ParameterInfo(
                param.Name,
                param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                param.RefKind));

        return parameters;
    }

    /// <summary>
    ///     生成缓存包装方法的源代码。
    /// </summary>
    private static void generate_cache_source(SourceProductionContext context,
        ImmutableArray<CacheMethodInfo> methodInfos)
    {
        var grouped = methodInfos.GroupBy(m => (m.type_name, m.fully_qualified_name, m.namespace_name));

        foreach (var group in grouped)
        {
            var sourceText = generate_cache_class_source([.. group]);
            var hintName = $"{group.Key.type_name}.Cache.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成缓存失效方法的源代码。
    /// </summary>
    private static void generate_evict_source(SourceProductionContext context,
        ImmutableArray<EvictMethodInfo> methodInfos)
    {
        var grouped = methodInfos.GroupBy(m => (m.type_name, m.fully_qualified_name, m.namespace_name));

        foreach (var group in grouped)
        {
            var sourceText = generate_evict_class_source([.. group]);
            var hintName = $"{group.Key.type_name}.EvictCache.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成包含缓存包装方法的扩展类源代码。
    /// </summary>
    private static string generate_cache_class_source(List<CacheMethodInfo> methods)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();

        var ns = methods[0].namespace_name;
        if (!string.IsNullOrEmpty(ns))
        {
            sb.append_line($"namespace {ns};");
            sb.append_line();
        }

        var typeName = methods[0].type_name;
        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{typeName}</c> 的缓存包装扩展。");
        sb.append_line("/// </summary>");
        sb.append_line($"public static class {typeName}CacheExtensions");
        using (sb.block())
        {
            for (var i = 0; i < methods.Count; i++)
            {
                if (i > 0) sb.append_line();

                generate_cached_method(sb, methods[i]);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成单个缓存包装方法。
    /// </summary>
    private static void generate_cached_method(SourceTextBuilder sb, CacheMethodInfo method)
    {
        var paramList = build_parameter_list(method.parameters);
        var argList = build_argument_list(method.parameters);

        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{method.method_name}</c> 的缓存包装方法。");
        sb.append_line("/// </summary>");

        foreach (var param in method.parameters)
            sb.append_line($"/// <param name=\"{param.name}\">{param.name} 参数。</param>");

        sb.append_line(
            $"public static {method.return_type} {method.method_name}_cached(this {method.fully_qualified_name} target, {paramList})");
        using (sb.block())
        {
            sb.append_line("var __cache = global::Sonic.Standard.Data.Cache.CacheContext.memory_cache;");
            sb.append_line();

            generate_cache_key_building(sb, method);
            sb.append_line();

            if (method.is_async)
            {
                sb.append_line("var __cached = __cache.get<string>(__cacheKey);");
                sb.append_line("if (__cached is not null)");
                using (sb.block())
                {
                    sb.append_line(
                        $"return global::System.Text.Json.JsonSerializer.Deserialize<{method.return_type}>(__cached);");
                }

                sb.append_line();

                sb.append_line($"var __result = target.{method.method_name}({argList});");
                sb.append_line("var __json = global::System.Text.Json.JsonSerializer.Serialize(__result);");
                sb.append_line(
                    $"__cache.set(__cacheKey, __json, global::System.TimeSpan.FromSeconds({method.duration}));");
                sb.append_line("return __result;");
            }
            else
            {
                sb.append_line("var __cached = __cache.get<string>(__cacheKey);");
                sb.append_line("if (__cached is not null)");
                using (sb.block())
                {
                    sb.append_line(
                        $"return global::System.Text.Json.JsonSerializer.Deserialize<{method.return_type}>(__cached)!;");
                }

                sb.append_line();

                sb.append_line($"var __result = target.{method.method_name}({argList});");
                sb.append_line("var __json = global::System.Text.Json.JsonSerializer.Serialize(__result);");
                sb.append_line(
                    $"__cache.set(__cacheKey, __json, global::System.TimeSpan.FromSeconds({method.duration}));");
                sb.append_line("return __result;");
            }
        }
    }

    /// <summary>
    ///     生成缓存键构建代码。
    /// </summary>
    private static void generate_cache_key_building(SourceTextBuilder sb, CacheMethodInfo method)
    {
        if (method.key is not null)
        {
            sb.append_line($"var __cacheKey = \"{StringEscapeHelper.escape_for_string(method.key)}\";");
            return;
        }

        sb.append_line("var __keyBuilder = new global::System.Text.StringBuilder();");
        sb.append_line($"__keyBuilder.Append(\"{method.type_name}.{method.method_name}\");");

        foreach (var kp in method.key_parameters)
            sb.append_line($"__keyBuilder.Append(':').Append({kp.name}?.ToString() ?? \"null\");");

        sb.append_line("var __cacheKey = __keyBuilder.ToString();");
    }

    /// <summary>
    ///     生成包含缓存失效方法的扩展类源代码。
    /// </summary>
    private static string generate_evict_class_source(List<EvictMethodInfo> methods)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();

        var ns = methods[0].namespace_name;
        if (!string.IsNullOrEmpty(ns))
        {
            sb.append_line($"namespace {ns};");
            sb.append_line();
        }

        var typeName = methods[0].type_name;
        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{typeName}</c> 的缓存失效扩展。");
        sb.append_line("/// </summary>");
        sb.append_line($"public static class {typeName}EvictCacheExtensions");
        using (sb.block())
        {
            for (var i = 0; i < methods.Count; i++)
            {
                if (i > 0) sb.append_line();

                generate_evict_method(sb, methods[i]);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成单个缓存失效包装方法。
    /// </summary>
    private static void generate_evict_method(SourceTextBuilder sb, EvictMethodInfo method)
    {
        var paramList = build_parameter_list(method.parameters);
        var argList = build_argument_list(method.parameters);

        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{method.method_name}</c> 的缓存失效包装方法。");
        sb.append_line("/// </summary>");

        foreach (var param in method.parameters)
            sb.append_line($"/// <param name=\"{param.name}\">{param.name} 参数。</param>");

        sb.append_line(
            $"public static {method.return_type} {method.method_name}_with_evict(this {method.fully_qualified_name} target, {paramList})");
        using (sb.block())
        {
            sb.append_line("var __cache = global::Sonic.Standard.Data.Cache.CacheContext.memory_cache;");
            sb.append_line();

            if (method.key is not null)
            {
                sb.append_line($"__cache.remove(\"{StringEscapeHelper.escape_for_string(method.key)}\");");
            }
            else
            {
                sb.append_line("// 清除所有缓存条目");
                sb.append_line("global::Sonic.Standard.Data.Cache.CacheContext.clear_all();");
            }

            sb.append_line();
            sb.append_line($"return target.{method.method_name}({argList});");
        }
    }

    /// <summary>
    ///     构建方法参数列表字符串。
    /// </summary>
    private static string build_parameter_list(List<ParameterInfo> parameters)
    {
        var parts = new List<string>();

        foreach (var param in parameters)
        {
            var prefix = param.ref_kind switch
            {
                RefKind.Ref => "ref ",
                RefKind.Out => "out ",
                RefKind.In => "in ",
                _ => ""
            };

            parts.Add($"{prefix}{param.Type} {param.name}");
        }

        return string.Join(", ", parts);
    }

    /// <summary>
    ///     构建方法调用参数列表字符串。
    /// </summary>
    private static string build_argument_list(List<ParameterInfo> parameters)
    {
        var parts = new List<string>();

        foreach (var param in parameters)
        {
            var prefix = param.ref_kind switch
            {
                RefKind.Ref => "ref ",
                RefKind.Out => "out ",
                RefKind.In => "in ",
                _ => ""
            };

            parts.Add($"{prefix}{param.name}");
        }

        return string.Join(", ", parts);
    }

    #region 数据模型

    /// <summary>
    ///     需要生成缓存包装的方法信息。
    /// </summary>
    internal readonly struct CacheMethodInfo
    {
        /// <summary>
        ///     包含类型名称。
        /// </summary>
        public readonly string type_name;

        /// <summary>
        ///     包含类型完全限定名称。
        /// </summary>
        public readonly string fully_qualified_name;

        /// <summary>
        ///     命名空间名称。
        /// </summary>
        public readonly string? namespace_name;

        /// <summary>
        ///     方法名称。
        /// </summary>
        public readonly string method_name;

        /// <summary>
        ///     返回类型完全限定名称。
        /// </summary>
        public readonly string return_type;

        /// <summary>
        ///     是否为异步方法。
        /// </summary>
        public readonly bool is_async;

        /// <summary>
        ///     缓存持续时间（秒）。
        /// </summary>
        public readonly int duration;

        /// <summary>
        ///     显式指定的缓存键，为 null 时自动生成。
        /// </summary>
        public readonly string? key;

        /// <summary>
        ///     参与缓存键构建的参数列表。
        /// </summary>
        public readonly List<CacheKeyParameterInfo> key_parameters;

        /// <summary>
        ///     方法参数列表。
        /// </summary>
        public readonly List<ParameterInfo> parameters;

        /// <summary>
        ///     初始化 <see cref="CacheMethodInfo" /> 的新实例。
        /// </summary>
        public CacheMethodInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            string methodName,
            string returnType,
            bool isAsync,
            int duration,
            string? key,
            List<CacheKeyParameterInfo> keyParameters,
            List<ParameterInfo> parameters)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            method_name = methodName;
            return_type = returnType;
            is_async = isAsync;
            this.duration = duration;
            this.key = key;
            key_parameters = keyParameters;
            this.parameters = parameters;
        }
    }

    /// <summary>
    ///     缓存键参数信息。
    /// </summary>
    internal readonly struct CacheKeyParameterInfo
    {
        /// <summary>
        ///     参数名称。
        /// </summary>
        public readonly string name;

        /// <summary>
        ///     参数类型完全限定名称。
        /// </summary>
        public readonly string Type;

        /// <summary>
        ///     初始化 <see cref="CacheKeyParameterInfo" /> 的新实例。
        /// </summary>
        public CacheKeyParameterInfo(string name, string type)
        {
            this.name = name;
            Type = type;
        }
    }

    /// <summary>
    ///     需要生成缓存失效包装的方法信息。
    /// </summary>
    internal readonly struct EvictMethodInfo
    {
        /// <summary>
        ///     包含类型名称。
        /// </summary>
        public readonly string type_name;

        /// <summary>
        ///     包含类型完全限定名称。
        /// </summary>
        public readonly string fully_qualified_name;

        /// <summary>
        ///     命名空间名称。
        /// </summary>
        public readonly string? namespace_name;

        /// <summary>
        ///     方法名称。
        /// </summary>
        public readonly string method_name;

        /// <summary>
        ///     返回类型完全限定名称。
        /// </summary>
        public readonly string return_type;

        /// <summary>
        ///     要清除的缓存键，为 null 时清除所有缓存。
        /// </summary>
        public readonly string? key;

        /// <summary>
        ///     方法参数列表。
        /// </summary>
        public readonly List<ParameterInfo> parameters;

        /// <summary>
        ///     初始化 <see cref="EvictMethodInfo" /> 的新实例。
        /// </summary>
        public EvictMethodInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            string methodName,
            string returnType,
            string? key,
            List<ParameterInfo> parameters)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            method_name = methodName;
            return_type = returnType;
            this.key = key;
            this.parameters = parameters;
        }
    }

    /// <summary>
    ///     方法参数信息。
    /// </summary>
    internal readonly struct ParameterInfo
    {
        /// <summary>
        ///     参数名称。
        /// </summary>
        public readonly string name;

        /// <summary>
        ///     参数类型完全限定名称。
        /// </summary>
        public readonly string Type;

        /// <summary>
        ///     参数传递方式。
        /// </summary>
        public readonly RefKind ref_kind;

        /// <summary>
        ///     初始化 <see cref="ParameterInfo" /> 的新实例。
        /// </summary>
        public ParameterInfo(string name, string type, RefKind refKind)
        {
            this.name = name;
            Type = type;
            ref_kind = refKind;
        }
    }

    #endregion
}