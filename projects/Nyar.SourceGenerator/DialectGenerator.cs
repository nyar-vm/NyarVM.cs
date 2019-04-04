using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Nyar.Generator;

[Generator]
public sealed class DialectGenerator : ISourceGenerator
{
    private const string DialectAttributeFullName = "Nyar.ObjectAlgebra.DialectAttribute";
    private const string OpAttributeFullName = "Nyar.ObjectAlgebra.OperatorAttribute";
    private const string TermTypeFullName = "Nyar.ObjectAlgebra.Term";
    private const string IAlgebraFullName = "Nyar.ObjectAlgebra.IAlgebra";
    private const string PENamedTuple = "(bool IsStatic, object? Value, object? Residual)";

    private static readonly Dictionary<string, string> BinaryOperators = new()
    {
        { "add", "+" }, { "sub", "-" }, { "mul", "*" }, { "div", "/" }, { "mod", "%" },
        { "eq", "==" }, { "ne", "!=" }, { "lt", "<" }, { "le", "<=" }, { "gt", ">" }, { "ge", ">=" },
        { "and", "&&" }, { "or", "||" }, { "xor", "^" }, { "shl", "<<" }, { "shr", ">>" },
        { "band", "&" }, { "bor", "|" }
    };

    private static readonly Dictionary<string, string> UnaryOperators = new()
    {
        { "neg", "-" }, { "not", "!" }, { "bitnot", "~" }
    };

    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var dialects = new List<DialectInfo>();
        var syntaxTreeCount = context.Compilation.SyntaxTrees.Count();
        var interfaceCount = 0;
        var dialectAttrCount = 0;

        foreach (var tree in context.Compilation.SyntaxTrees)
        {
            var semanticModel = context.Compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();

            foreach (var ifaceDecl in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
            {
                interfaceCount++;
                var ifaceSymbol = semanticModel.GetDeclaredSymbol(ifaceDecl) as INamedTypeSymbol;
                if (ifaceSymbol is null) continue;

                var attrs = ifaceSymbol.GetAttributes();
                var dialectAttr = attrs
                    .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == DialectAttributeFullName);

                if (dialectAttr is null) continue;

                dialectAttrCount++;

                var dialectName = dialectAttr.ConstructorArguments.FirstOrDefault().Value as string
                                  ?? ifaceSymbol.Name.TrimStart('I').ToLowerInvariant();
                var ops = new List<OpInfo>();

                foreach (var member in ifaceSymbol.GetMembers())
                {
                    if (member is not IMethodSymbol method) continue;

                    if (method.IsStatic) continue;

                    var opAttr = method.GetAttributes()
                        .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == OpAttributeFullName);

                    if (opAttr is null) continue;

                    var opName = opAttr.ConstructorArguments.FirstOrDefault().Value as string ?? method.Name;
                    ops.Add(AnalyzeOp(method, opName, context));
                }

                dialects.Add(new DialectInfo(ifaceSymbol, dialectName, ops));
            }
        }

        if (dialects.Count == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("DG000", "DialectGenerator",
                    $"鏈壘鍒颁换浣曟柟瑷€鎺ュ彛 (璇硶鏍? {syntaxTreeCount}, 鎺ュ彛: {interfaceCount}, 甯ialect鐗规€? {dialectAttrCount})",
                    "Nyar.SourceGenerator", DiagnosticSeverity.Warning, true),
                Location.None));
            return;
        }

        foreach (var dialect in dialects)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("DG001", "DialectGenerator", $"处理方言: {dialect.AlgInterfaceName}",
                    "Nyar.SourceGenerator", DiagnosticSeverity.Warning, true),
                Location.None));

            // OA 核心产物：algebra 接口
            GenerateAlgInterface(context, dialect);

            // OA 核心产物：operator descriptor / symbols
            GenerateOperatorDescriptors(context, dialect);

            // OA 核心产物：ENode 级别 Reifier
            GenerateReifier(context, dialect);

            // OA 核心产物：模式代数接口
            GeneratePatternAlg(context, dialect);

            // OA 核心产物：规则 DSL
            GenerateRuleDSL(context, dialect);

            // OA 核心产物：聚合 algebra 接口（如有多个方言）
            // （由方言项目手动组合，此处暂不自动生成）

            // ⛔ 遗留产物：节点类生成（兼容期保留，OA 重构完成后删除）
            // 新的方言操作符应通过 OA 核心产物（algebra + descriptor + reifier + pattern）表达
            // 注意：跳过 Literal 节点生成，因为 Nyar.IR.Intent.Literal<T> 已手动定义泛型版本
            GenerateNodes(context, dialect);
            // GenerateEgraphBuilder(context, dialect);
            // GeneratePEBuilder(context, dialect);
            // GenerateIkunBridge(context, dialect);
            // GenerateMatcher(context, dialect);
            // GenerateCostHook(context, dialect);

            // 如果方言已有手动编写的 Builtin，则跳过自动生成
            if (!HasExistingType(context.Compilation, $"Nyar.Dialect.{dialect.PascalDialectName}.Rules.{dialect.PascalDialectName}Builtin"))
            {
                GenerateBuiltin(context, dialect);
            }
        }
    }

    #region 鍒嗘瀽鏂规硶

    private OpInfo AnalyzeOp(IMethodSymbol method, string opName, GeneratorExecutionContext context)
    {
        var returnExprTypeArg = GetExprTypeArg(method.ReturnType);
        var parameters = new List<OpParamInfo>();
        var exprParamCount = 0;
        var hasFuncExpr = false;
        var hasExprArray = false;
        var hasExprCollection = false;
        var hasExprDictionary = false;
        var hasNullableExpr = false;
        var hasPlainArray = false;

        foreach (var param in method.Parameters)
        {
            var paramInfo = AnalyzeParam(param, context);
            parameters.Add(paramInfo);

            if (paramInfo.IsExprType && !paramInfo.IsNullableExprType) exprParamCount++;

            if (paramInfo.IsFuncExprType) hasFuncExpr = true;

            if (paramInfo.IsExprArrayType) hasExprArray = true;

            if (paramInfo.IsExprCollectionType) hasExprCollection = true;

            if (paramInfo.IsExprDictionaryType) hasExprDictionary = true;

            if (paramInfo.IsNullableExprType) hasNullableExpr = true;

            if (paramInfo.IsPlainArrayType) hasPlainArray = true;
        }

        var category = OpCategory.Extension;
        if (!hasFuncExpr && !hasExprArray && !hasExprCollection && !hasExprDictionary && !hasNullableExpr &&
            !hasPlainArray)
        {
            if (exprParamCount == 0)
                category = OpCategory.Literal;
            else if (exprParamCount == 1)
                category = OpCategory.Unary;
            else if (exprParamCount == 2) category = OpCategory.Binary;
        }

        var typeParams = method.TypeParameters.Length > 0
            ? $"<{string.Join(", ", method.TypeParameters.Select(tp => tp.Name))}>"
            : "";

        return new OpInfo(method.Name, opName, category, returnExprTypeArg, parameters, typeParams);
    }

    private OpParamInfo AnalyzeParam(IParameterSymbol param, GeneratorExecutionContext context)
    {
        var type = param.Type;
        var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var isExpr = IsExprType(type);
        var isNullableExpr = IsNullableExprType(type);
        var exprTypeArg = isExpr ? GetExprTypeArg(type) : null;
        var isFuncExpr = IsFuncExprType(type);

        var isExprArray = false;
        var exprArrayElementType = (string)null;
        var isExprCollection = false;
        var exprCollectionKind = ExprCollectionKind.None;
        var exprCollectionElementType = (string)null;
        var isExprDictionary = false;
        var exprDictionaryKind = ExprDictionaryKind.None;
        var exprDictionaryKeyType = (string)null;
        var isPlainArray = false;

        if (!isExpr && !isFuncExpr && type is IArrayTypeSymbol arrayType)
        {
            var elementType = arrayType.ElementType;
            var elementTypeDisplay = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            // 直接检查元素类型是否为 Term<T>，通过显示名称判断
            var isTermType = elementTypeDisplay == "Nyar.ObjectAlgebra.Term<T>" ||
                             elementTypeDisplay == "global::Nyar.ObjectAlgebra.Term<T>" ||
                             elementTypeDisplay.StartsWith("Nyar.ObjectAlgebra.Term<", StringComparison.Ordinal) ||
                             elementTypeDisplay.StartsWith("global::Nyar.ObjectAlgebra.Term<",
                                 StringComparison.Ordinal);

            if (isTermType)
            {
                isExprArray = true;
                // Term<T>[] 数组，表达式类型参数为 Id
                exprArrayElementType = "Id";
            }
            else
            {
                isPlainArray = true;
            }
        }

        if (!isExpr && !isFuncExpr && !isExprArray && type is INamedTypeSymbol namedType)
        {
            if (TryGetExprCollectionInfo(namedType, out exprCollectionKind, out exprCollectionElementType))
                isExprCollection = true;
            else if (TryGetExprDictionaryInfo(namedType, out exprDictionaryKind, out exprDictionaryKeyType))
                isExprDictionary = true;
        }

        return new OpParamInfo(
            EscapeKeyword(param.Name),
            typeName,
            isExpr,
            isNullableExpr,
            exprTypeArg,
            isFuncExpr,
            GetFuncExprArgCount(type),
            isExprArray,
            exprArrayElementType,
            isExprCollection,
            exprCollectionKind,
            exprCollectionElementType,
            isExprDictionary,
            exprDictionaryKind,
            exprDictionaryKeyType,
            isPlainArray
        );
    }

    private static bool IsExprType(ITypeSymbol type)
    {
        // 数组类型不是表达式类型（表达式类型是 Term<T>，不是 Term<T>[]）
        if (type is IArrayTypeSymbol) return false;

        // 检查元数据名称
        if (type is INamedTypeSymbol named)
            // Term<T> 的元数据名称是 "Term`1"（带反引号）
            // 也检查没有反引号的情况（某些 Roslyn 上下文）
            if (named.MetadataName == "Term`1" || named.MetadataName == "Term")
                return true;

        // 检查显示名称
        var display = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).TrimEnd('?');
        if (display == "Nyar.ObjectAlgebra.Term<T>" ||
            display == "Term<T>" ||
            display.StartsWith("Nyar.ObjectAlgebra.Term<", StringComparison.Ordinal) ||
            display.StartsWith("global::Nyar.ObjectAlgebra.Term<", StringComparison.Ordinal))
            return true;

        return false;
    }

    private static bool IsNullableExprType(ITypeSymbol type)
    {
        if (!IsExprType(type)) return false;

        if (type.NullableAnnotation == NullableAnnotation.Annotated) return true;

        var display = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return display.EndsWith("?", StringComparison.Ordinal);
    }

    private static string GetExprTypeArg(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named) return null;

        if (!IsExprType(type)) return null;

        return named.TypeArguments.FirstOrDefault()
            ?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
    }

    private static bool IsFuncExprType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named) return false;

        if (named.ConstructedFrom == null) return false;

        if (!named.TypeArguments.All(t => IsExprType(t))) return false;

        var constructedFromStr = named.ConstructedFrom.ToDisplayString();
        return constructedFromStr.StartsWith("System.Func<");
    }

    private static int GetFuncExprArgCount(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named) return 0;

        if (!IsFuncExprType(type)) return 0;

        return named.TypeArguments.Length;
    }

    private static bool TryGetExprCollectionInfo(INamedTypeSymbol type, out ExprCollectionKind kind,
        out string exprElementType)
    {
        kind = ExprCollectionKind.None;
        exprElementType = null;

        if (!type.IsGenericType || type.TypeArguments.Length != 1) return false;

        if (!IsExprType(type.TypeArguments[0])) return false;

        var constructedFrom = type.ConstructedFrom?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        switch (constructedFrom)
        {
            case "global::System.Collections.Generic.IReadOnlyList<T>":
                kind = ExprCollectionKind.ReadOnlyList;
                break;
            case "global::System.Collections.Immutable.ImmutableArray<T>":
                kind = ExprCollectionKind.ImmutableArray;
                break;
            default:
                return false;
        }

        exprElementType = GetExprTypeArg(type.TypeArguments[0]);
        return true;
    }

    private static bool TryGetExprDictionaryInfo(INamedTypeSymbol type, out ExprDictionaryKind kind,
        out string keyType)
    {
        kind = ExprDictionaryKind.None;
        keyType = null;

        if (!type.IsGenericType || type.TypeArguments.Length != 2) return false;

        if (!IsExprType(type.TypeArguments[1])) return false;

        var constructedFrom = type.ConstructedFrom?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        switch (constructedFrom)
        {
            case "global::System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>":
                kind = ExprDictionaryKind.ReadOnlyDictionary;
                break;
            default:
                return false;
        }

        keyType = SimplifyType(type.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        return true;
    }

    #endregion

    #region Alg 鎺ュ彛鐢熸垚

    private void GenerateAlgInterface(GeneratorExecutionContext context, DialectInfo dialect)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using Nyar.ObjectAlgebra;");
        sb.AppendLine();
        sb.AppendLine($"namespace Nyar.ObjectAlgebra.Generated.{dialect.PascalDialectName};");
        sb.AppendLine();

        var algName = dialect.AlgInterfaceName;
        var docName = dialect.PascalDialectName;

        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// {docName} 鏂硅█鐨?OA 宸ュ巶鎺ュ彛");
        sb.AppendLine("        /// </summary>");

        sb.AppendLine($"public interface {algName}<E> : global::Nyar.ObjectAlgebra.IAlgebra<E>");
        sb.AppendLine("{");

        for (var i = 0; i < dialect.Ops.Count; i++)
        {
            var op = dialect.Ops[i];
            GenerateAlgMethod(sb, op, "    ");
            if (i < dialect.Ops.Count - 1) sb.AppendLine();
        }

        sb.AppendLine("}");

        context.AddSource($"{dialect.PascalDialectName}.Alg.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GenerateAlgMethod(StringBuilder sb, OpInfo op, string indent)
    {
        var paramList = new List<string>();

        foreach (var p in op.Parameters)
        {
            var algType = GetAlgType(p);
            paramList.Add($"{algType} {p.Name}");
        }

        var paramStr = paramList.Count > 0 ? string.Join(", ", paramList) : "";
        var escapedMethodName = EscapeKeyword(op.MethodName);
        sb.AppendLine($"{indent}E {escapedMethodName}{op.TypeParams}({paramStr});");
    }

    private static string GetAlgType(OpParamInfo param)
    {
        if (param.IsNullableExprType) return "E";

        if (param.IsExprType) return "E";

        if (param.IsExprArrayType) return "E[]";

        if (param.IsExprCollectionType) return $"{GetExprCollectionTypeName(param.ExprCollectionKind)}<E>";

        if (param.IsExprDictionaryType)
            return $"{GetExprDictionaryTypeName(param.ExprDictionaryKind)}<{param.ExprDictionaryKeyType}, E>";

        if (param.IsFuncExprType)
        {
            var eArgs = Enumerable.Range(0, param.FuncExprArgCount).Select(_ => "E");
            return $"Func<{string.Join(", ", eArgs)}>";
        }

        return SimplifyType(param.OriginalType);
    }

    #endregion

    #region EGraph Builder 鐢熸垚

    private void GenerateEgraphBuilder(GeneratorExecutionContext context, DialectInfo dialect)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using System.Collections.Immutable;");
        sb.AppendLine("using Nyar.EGraph;");
        sb.AppendLine("using Nyar.IR.Intent;");
        sb.AppendLine("using Nyar.ObjectAlgebra;");
        sb.AppendLine($"using Nyar.Dialect.{dialect.PascalDialectName}.Nodes;");
        sb.AppendLine();
        sb.AppendLine($"namespace Nyar.ObjectAlgebra.Generated.{dialect.PascalDialectName};");
        sb.AppendLine();

        var className = $"{dialect.PascalDialectName}EgraphBuilder";
        var algName = dialect.AlgInterfaceName;
        const string sealedKeyword = "sealed ";

        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// {dialect.PascalDialectName} 鏂硅█鐨?EGraph Builder锛氬皢 OA 鏂规硶璋冪敤鏄犲皠涓?EGraph 鑺傜偣");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"public {sealedKeyword}class {className} : {algName}<Id>");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly EGraph<AlgebraNode> _egraph;");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        ///     初始化 EGraph Builder");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <param name=\"egraph\">EGraph 实例。</param>");
        sb.AppendLine($"    public {className}(EGraph<AlgebraNode> egraph)");
        sb.AppendLine("    {");
        sb.AppendLine("        _egraph = egraph;");
        sb.AppendLine("    }");

        for (var i = 0; i < dialect.Ops.Count; i++)
        {
            var op = dialect.Ops[i];
            sb.AppendLine();
            GenerateEgraphBuilderMethod(sb, op, "    ");
        }

        sb.AppendLine("}");

        context.AddSource($"{dialect.PascalDialectName}.EgraphBuilder.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GenerateEgraphBuilderMethod(StringBuilder sb, OpInfo op, string indent)
    {
        var paramList = new List<string>();
        var exprParams = new List<OpParamInfo>();
        var nonExprParams = new List<OpParamInfo>();

        foreach (var p in op.Parameters)
        {
            var egraphType = GetEgraphType(p);
            paramList.Add($"{egraphType} {p.Name}");

            if (p.IsExprType || p.IsExprArrayType || p.IsExprCollectionType || p.IsExprDictionaryType ||
                p.IsFuncExprType)
                exprParams.Add(p);
            else
                nonExprParams.Add(p);
        }

        var paramStr = paramList.Count > 0 ? string.Join(", ", paramList) : "";

        switch (op.Category)
        {
            case OpCategory.Unary:
                GenerateUnaryEgraphMethod(sb, op, indent, paramStr, exprParams);
                break;
            case OpCategory.Binary:
                GenerateBinaryEgraphMethod(sb, op, indent, paramStr, exprParams);
                break;
            default:
                GenerateExtensionEgraphMethod(sb, op, indent, paramStr, exprParams, nonExprParams);
                break;
        }
    }

    private static void GenerateUnaryEgraphMethod(StringBuilder sb, OpInfo op, string indent,
        string paramStr, List<OpParamInfo> exprParams)
    {
        var className = ToPascalCase(op.MethodName) + op.TypeParams;
        var escapedMethodName = EscapeKeyword(op.MethodName);
        var args = string.Join(", ", op.Parameters.Select(p => p.Name));
        sb.AppendLine(
            $"{indent}public Id {escapedMethodName}{op.TypeParams}({paramStr}) => _egraph.add(new global::Nyar.Dialect.{op.DialectPascalName}.Nodes.{className}({args}));");
    }

    private static void GenerateBinaryEgraphMethod(StringBuilder sb, OpInfo op, string indent,
        string paramStr, List<OpParamInfo> exprParams)
    {
        var className = ToPascalCase(op.MethodName) + op.TypeParams;
        var escapedMethodName = EscapeKeyword(op.MethodName);
        var args = string.Join(", ", op.Parameters.Select(p => p.Name));
        sb.AppendLine(
            $"{indent}public Id {escapedMethodName}{op.TypeParams}({paramStr}) => _egraph.add(new global::Nyar.Dialect.{op.DialectPascalName}.Nodes.{className}({args}));");
    }

    private static void GenerateExtensionEgraphMethod(StringBuilder sb, OpInfo op, string indent,
        string paramStr, List<OpParamInfo> exprParams, List<OpParamInfo> nonExprParams)
    {
        var className = ToPascalCase(op.MethodName) + op.TypeParams;
        var escapedMethodName = EscapeKeyword(op.MethodName);
        var args = string.Join(", ", op.Parameters.Select(p => p.Name));
        sb.AppendLine(
            $"{indent}public Id {escapedMethodName}{op.TypeParams}({paramStr}) => _egraph.add(new global::Nyar.Dialect.{op.DialectPascalName}.Nodes.{className}({args}));");
    }

    private static string GetEgraphType(OpParamInfo param)
    {
        if (param.IsNullableExprType) return "Id";

        if (param.IsExprType) return "Id";

        if (param.IsExprArrayType) return "Id[]";

        if (param.IsExprCollectionType) return $"{GetExprCollectionTypeName(param.ExprCollectionKind)}<Id>";

        if (param.IsExprDictionaryType)
            return $"{GetExprDictionaryTypeName(param.ExprDictionaryKind)}<{param.ExprDictionaryKeyType}, Id>";

        if (param.IsFuncExprType)
        {
            var args = Enumerable.Range(0, param.FuncExprArgCount).Select(_ => "Id");
            return $"Func<{string.Join(", ", args)}>";
        }

        return SimplifyType(param.OriginalType);
    }

    #endregion

    #region PE Builder 鐢熸垚

    private void GeneratePEBuilder(GeneratorExecutionContext context, DialectInfo dialect)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using Nyar.ObjectAlgebra;");
        sb.AppendLine();
        sb.AppendLine($"namespace Nyar.ObjectAlgebra.Generated.{dialect.PascalDialectName};");
        sb.AppendLine();

        var className = $"{dialect.PascalDialectName}PEBuilder";
        var algName = dialect.AlgInterfaceName;
        const string sealedKeyword = "sealed ";

        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// {dialect.PascalDialectName} 鏂硅█鐨勯儴鍒嗘眰鍊?Builder");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"public {sealedKeyword}class {className} : {algName}<{PENamedTuple}>");
        sb.AppendLine("{");

        for (var i = 0; i < dialect.Ops.Count; i++)
        {
            var op = dialect.Ops[i];
            sb.AppendLine();
            GeneratePEBuilderMethod(sb, op, "    ");
        }

        sb.AppendLine("}");

        GeneratePEResidualClasses(sb, dialect);

        context.AddSource($"{dialect.PascalDialectName}.PEBuilder.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GeneratePEBuilderMethod(StringBuilder sb, OpInfo op, string indent)
    {
        var paramList = new List<string>();
        var exprParams = new List<OpParamInfo>();

        foreach (var p in op.Parameters)
        {
            var peType = GetPEType(p);
            paramList.Add($"{peType} {p.Name}");

            if (p.IsExprType || p.IsExprArrayType || p.IsExprCollectionType || p.IsExprDictionaryType)
                exprParams.Add(p);
        }

        var paramStr = paramList.Count > 0 ? string.Join(", ", paramList) : "";

        switch (op.Category)
        {
            case OpCategory.Literal:
                GenerateLiteralPEMethod(sb, op, indent, paramStr);
                break;
            case OpCategory.Unary:
                GenerateUnaryPEMethod(sb, op, indent, paramStr, exprParams);
                break;
            case OpCategory.Binary:
                GenerateBinaryPEMethod(sb, op, indent, paramStr, exprParams);
                break;
            default:
                GenerateExtensionPEMethod(sb, op, indent, paramStr, exprParams);
                break;
        }
    }

    private static void GenerateLiteralPEMethod(StringBuilder sb, OpInfo op, string indent, string paramStr)
    {
        var escapedMethodName = EscapeKeyword(op.MethodName);
        if (op.Parameters.Count == 0)
        {
            sb.AppendLine(
                $"{indent}public {PENamedTuple} {escapedMethodName}{op.TypeParams}({paramStr}) => (true, null, null);");
            return;
        }

        if (op.Parameters.Count == 1)
        {
            var p = op.Parameters[0];
            sb.AppendLine(
                $"{indent}public {PENamedTuple} {escapedMethodName}{op.TypeParams}({paramStr}) => (true, {p.Name}, null);");
            return;
        }

        var values = op.Parameters.Select(p => p.Name).ToList();
        sb.AppendLine(
            $"{indent}public {PENamedTuple} {escapedMethodName}{op.TypeParams}({paramStr}) => (true, ({string.Join(", ", values)}), null);");
    }

    private static void GenerateUnaryPEMethod(StringBuilder sb, OpInfo op, string indent,
        string paramStr, List<OpParamInfo> exprParams)
    {
        var operand = exprParams[0];
        var castType = "dynamic";
        var opSymbol = UnaryOperators.TryGetValue(op.OpName, out var unaryVal) ? unaryVal : null;
        var escapedMethodName = EscapeKeyword(op.MethodName);

        sb.AppendLine($"{indent}public {PENamedTuple} {escapedMethodName}{op.TypeParams}({paramStr})");
        sb.AppendLine($"{indent}{{");

        if (opSymbol != null)
        {
            sb.AppendLine($"{indent}    if ({operand.Name}.IsStatic)");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        return (true, {opSymbol}({castType}){operand.Name}.Value!, null);");
            sb.AppendLine($"{indent}    }}");
        }

        sb.AppendLine(
            $"{indent}    return (false, null, new PE{op.MethodName}Residual{op.TypeParams}({string.Join(", ", op.Parameters.Select(p => p.Name))}));");
        sb.AppendLine($"{indent}}}");
    }

    private static void GenerateBinaryPEMethod(StringBuilder sb, OpInfo op, string indent,
        string paramStr, List<OpParamInfo> exprParams)
    {
        var left = exprParams[0];
        var right = exprParams[1];
        var leftCast = "dynamic";
        var rightCast = "dynamic";
        var opSymbol = BinaryOperators.TryGetValue(op.OpName, out var binaryVal) ? binaryVal : null;
        var escapedMethodName = EscapeKeyword(op.MethodName);

        sb.AppendLine($"{indent}public {PENamedTuple} {escapedMethodName}{op.TypeParams}({paramStr})");
        sb.AppendLine($"{indent}{{");

        if (opSymbol != null)
        {
            sb.AppendLine($"{indent}    if ({left.Name}.IsStatic && {right.Name}.IsStatic)");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine(
                $"{indent}        return (true, ({leftCast}){left.Name}.Value! {opSymbol} ({rightCast}){right.Name}.Value!, null);");
            sb.AppendLine($"{indent}    }}");
        }

        sb.AppendLine(
            $"{indent}    return (false, null, new PE{op.MethodName}Residual{op.TypeParams}({string.Join(", ", op.Parameters.Select(p => p.Name))}));");
        sb.AppendLine($"{indent}}}");
    }

    private static void GenerateExtensionPEMethod(StringBuilder sb, OpInfo op, string indent,
        string paramStr, List<OpParamInfo> exprParams)
    {
        var escapedMethodName = EscapeKeyword(op.MethodName);
        sb.AppendLine($"{indent}public {PENamedTuple} {escapedMethodName}{op.TypeParams}({paramStr})");
        sb.AppendLine($"{indent}{{");

        if (exprParams.Count > 0)
        {
            var conditions = new List<string>();
            foreach (var p in exprParams)
                if (p.IsNullableExprType)
                    conditions.Add($"{p.Name}.IsStatic");
                else if (p.IsExprType)
                    conditions.Add($"{p.Name}.IsStatic");
                else if (p.IsExprArrayType)
                    conditions.Add($"{p.Name}.All(x => x.IsStatic)");
                else if (p.IsExprCollectionType)
                    conditions.Add($"{p.Name}.All(x => x.IsStatic)");
                else if (p.IsExprDictionaryType) conditions.Add($"{p.Name}.Values.All(x => x.IsStatic)");

            if (conditions.Count > 0)
            {
                sb.AppendLine($"{indent}    if ({string.Join(" && ", conditions)})");
                sb.AppendLine($"{indent}    {{");
                sb.AppendLine($"{indent}        return (true, null, null);");
                sb.AppendLine($"{indent}    }}");
            }
        }

        sb.AppendLine(
            $"{indent}    return (false, null, new PE{op.MethodName}Residual{op.TypeParams}({string.Join(", ", op.Parameters.Select(p => p.Name))}));");
        sb.AppendLine($"{indent}}}");
    }

    private static void GeneratePEResidualClasses(StringBuilder sb, DialectInfo dialect)
    {
        var nonLiteralOps = dialect.Ops.Where(o => o.Category != OpCategory.Literal).ToList();

        foreach (var op in nonLiteralOps)
        {
            sb.AppendLine();
            sb.AppendLine("            /// <summary>");
            sb.AppendLine($"            /// {op.MethodName} 操作的部分求值残差");
            sb.AppendLine("            /// </summary>");
            sb.AppendLine($"public sealed class PE{op.MethodName}Residual{op.TypeParams}");
            sb.AppendLine("{");

            var constructorParams = new List<string>();
            var assignments = new List<string>();

            foreach (var p in op.Parameters)
            {
                var propType = GetPEType(p);
                var propName = char.ToUpperInvariant(p.Name[0]) + p.Name.Substring(1);

                sb.AppendLine("                /// <summary>");
                sb.AppendLine($"                /// {p.Name} 参数的值");
                sb.AppendLine("                /// </summary>");
                sb.AppendLine($"    public {propType} {propName} {{ get; }}");
                constructorParams.Add($"{propType} {p.Name}");
                assignments.Add($"        {propName} = {p.Name};");
            }

            sb.AppendLine();
            sb.AppendLine("            /// <summary>");
            sb.AppendLine($"            /// 初始化 PE{op.MethodName}Residual 实例");
            sb.AppendLine("            /// </summary>");
            sb.AppendLine(
                $"    public PE{op.MethodName}Residual{op.TypeParams}({string.Join(", ", constructorParams)})");
            sb.AppendLine("    {");
            foreach (var assignment in assignments) sb.AppendLine(assignment);

            sb.AppendLine("    }");

            sb.AppendLine("}");
        }
    }

    private static string GetPEType(OpParamInfo param)
    {
        if (param.IsNullableExprType) return PENamedTuple;

        if (param.IsExprType) return PENamedTuple;

        if (param.IsExprArrayType) return PENamedTuple + "[]";

        if (param.IsExprCollectionType) return $"{GetExprCollectionTypeName(param.ExprCollectionKind)}<{PENamedTuple}>";

        if (param.IsExprDictionaryType)
            return
                $"{GetExprDictionaryTypeName(param.ExprDictionaryKind)}<{param.ExprDictionaryKeyType}, {PENamedTuple}>";

        if (param.IsFuncExprType)
        {
            var args = Enumerable.Range(0, param.FuncExprArgCount).Select(_ => PENamedTuple);
            return $"Func<{string.Join(", ", args)}>";
        }

        return SimplifyType(param.OriginalType);
    }

    #endregion

    #region Oa Bridge 鐢熸垚

    private void GenerateIkunBridge(GeneratorExecutionContext context, DialectInfo dialect)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using System.Collections.Immutable;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using Nyar.EGraph;");
        sb.AppendLine("using Nyar.IR.Intent;");
        sb.AppendLine("using Nyar.ObjectAlgebra;");
        sb.AppendLine($"using Nyar.Dialect.{dialect.PascalDialectName}.Nodes;");
        sb.AppendLine();
        sb.AppendLine($"namespace Nyar.ObjectAlgebra.Generated.{dialect.PascalDialectName};");
        sb.AppendLine();

        var className = $"{dialect.PascalDialectName}IkunBridge";
        var algName = dialect.AlgInterfaceName;
        const string sealedKeyword = "sealed ";

        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// {dialect.PascalDialectName} 方言的 AlgebraNode 桥接器：将 OA 构造映射为 AlgebraNode 节点");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"public {sealedKeyword}class {className} : {algName}<AlgebraNode>");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly EGraph<AlgebraNode> _egraph;");
        sb.AppendLine();

        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        ///     初始化 AlgebraNode 桥接器");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <param name=\"egraph\">EGraph 实例，用于将 AlgebraNode 节点注册为 Id 引用。</param>");

        sb.AppendLine($"    public {className}(EGraph<AlgebraNode> egraph)");
        sb.AppendLine("    {");
        sb.AppendLine("        _egraph = egraph;");
        sb.AppendLine("    }");

        for (var i = 0; i < dialect.Ops.Count; i++)
        {
            var op = dialect.Ops[i];
            sb.AppendLine();
            GenerateIkunBridgeMethod(sb, op, "    ");
        }

        sb.AppendLine("}");

        context.AddSource($"{dialect.PascalDialectName}.IkunBridge.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GenerateIkunBridgeMethod(StringBuilder sb, OpInfo op, string indent)
    {
        var paramList = new List<string>();
        var exprParams = new List<OpParamInfo>();
        var nonExprParams = new List<OpParamInfo>();

        foreach (var p in op.Parameters)
        {
            var ikunType = GetIkunType(p);
            paramList.Add($"{ikunType} {p.Name}");

            if (p.IsExprType || p.IsExprArrayType || p.IsExprCollectionType || p.IsExprDictionaryType ||
                p.IsFuncExprType)
                exprParams.Add(p);
            else
                nonExprParams.Add(p);
        }

        var paramStr = paramList.Count > 0 ? string.Join(", ", paramList) : "";

        switch (op.Category)
        {
            case OpCategory.Unary:
                GenerateUnaryIkunBridgeMethod(sb, op, indent, paramStr, exprParams);
                break;
            case OpCategory.Binary:
                GenerateBinaryIkunBridgeMethod(sb, op, indent, paramStr, exprParams);
                break;
            default:
                GenerateExtensionIkunBridgeMethod(sb, op, indent, paramStr, exprParams, nonExprParams);
                break;
        }
    }

    private static void GenerateUnaryIkunBridgeMethod(StringBuilder sb, OpInfo op, string indent,
        string paramStr, List<OpParamInfo> exprParams)
    {
        var className = GetQualifiedNodeClassName(op);
        var escapedMethodName = EscapeKeyword(op.MethodName);
        sb.AppendLine($"{indent}public AlgebraNode {escapedMethodName}{op.TypeParams}({paramStr})");
        sb.AppendLine($"{indent}{{");
        var constructorArgs = EmitIkunNodeConstructorArgs(sb, op, indent);
        sb.AppendLine($"{indent}    return new {className}({string.Join(", ", constructorArgs)});");
        sb.AppendLine($"{indent}}}");
    }

    private static void GenerateBinaryIkunBridgeMethod(StringBuilder sb, OpInfo op, string indent,
        string paramStr, List<OpParamInfo> exprParams)
    {
        var className = GetQualifiedNodeClassName(op);
        var escapedMethodName = EscapeKeyword(op.MethodName);
        sb.AppendLine($"{indent}public AlgebraNode {escapedMethodName}{op.TypeParams}({paramStr})");
        sb.AppendLine($"{indent}{{");
        var constructorArgs = EmitIkunNodeConstructorArgs(sb, op, indent);
        sb.AppendLine($"{indent}    return new {className}({string.Join(", ", constructorArgs)});");
        sb.AppendLine($"{indent}}}");
    }

    private static void GenerateExtensionIkunBridgeMethod(StringBuilder sb, OpInfo op, string indent,
        string paramStr, List<OpParamInfo> exprParams, List<OpParamInfo> nonExprParams)
    {
        var className = GetQualifiedNodeClassName(op);
        var escapedMethodName = EscapeKeyword(op.MethodName);
        sb.AppendLine($"{indent}public AlgebraNode {escapedMethodName}{op.TypeParams}({paramStr})");
        sb.AppendLine($"{indent}{{");
        var constructorArgs = EmitIkunNodeConstructorArgs(sb, op, indent);
        sb.AppendLine($"{indent}    return new {className}({string.Join(", ", constructorArgs)});");
        sb.AppendLine($"{indent}}}");
    }

    private static List<string> EmitIkunNodeConstructorArgs(StringBuilder sb, OpInfo op, string indent)
    {
        var constructorArgs = new List<string>();

        foreach (var p in op.Parameters)
        {
            if (p.IsNullableExprType)
            {
                var idName = $"__{p.Name}Id";
                sb.AppendLine($"{indent}    Id? {idName} = {p.Name} is null ? null : _egraph.add({p.Name});");
                constructorArgs.Add($"{idName} ?? default");
                continue;
            }

            if (p.IsExprType)
            {
                var idName = $"__{p.Name}Id";
                sb.AppendLine($"{indent}    var {idName} = _egraph.add({p.Name});");
                constructorArgs.Add(idName);
                continue;
            }

            if (p.IsExprArrayType)
            {
                var idsName = $"__{p.Name}Ids";
                sb.AppendLine($"{indent}    var {idsName} = new List<Id>();");
                sb.AppendLine($"{indent}    foreach (var __item in {p.Name})");
                sb.AppendLine($"{indent}    {{");
                sb.AppendLine($"{indent}        {idsName}.Add(_egraph.add(__item));");
                sb.AppendLine($"{indent}    }}");
                constructorArgs.Add($"{idsName}.ToArray()");
                continue;
            }

            if (p.IsExprCollectionType)
            {
                var idsName = $"__{p.Name}Ids";
                if (p.ExprCollectionKind == ExprCollectionKind.ImmutableArray)
                {
                    var builderName = $"__{p.Name}Builder";
                    sb.AppendLine(
                        $"{indent}    var {builderName} = global::System.Collections.Immutable.ImmutableArray.CreateBuilder<Id>();");
                    sb.AppendLine($"{indent}    foreach (var __item in {p.Name})");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        {builderName}.Add(_egraph.add(__item));");
                    sb.AppendLine($"{indent}    }}");
                    constructorArgs.Add($"{builderName}.MoveToImmutable()");
                }
                else
                {
                    sb.AppendLine($"{indent}    var {idsName} = new List<Id>();");
                    sb.AppendLine($"{indent}    foreach (var __item in {p.Name})");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        {idsName}.Add(_egraph.add(__item));");
                    sb.AppendLine($"{indent}    }}");
                    constructorArgs.Add(idsName);
                }

                continue;
            }

            if (p.IsExprDictionaryType)
            {
                var idsName = $"__{p.Name}Ids";
                sb.AppendLine($"{indent}    var {idsName} = new Dictionary<{p.ExprDictionaryKeyType}, Id>();");
                sb.AppendLine($"{indent}    foreach (var __entry in {p.Name})");
                sb.AppendLine($"{indent}    {{");
                sb.AppendLine($"{indent}        {idsName}[__entry.Key] = _egraph.add(__entry.Value);");
                sb.AppendLine($"{indent}    }}");
                constructorArgs.Add(idsName);
                continue;
            }

            constructorArgs.Add(p.Name);
        }

        return constructorArgs;
    }

    private static string GetQualifiedNodeClassName(OpInfo op)
    {
        return $"global::Nyar.Dialect.{op.DialectPascalName}.Nodes.{ToPascalCase(op.MethodName)}{op.TypeParams}";
    }

    private static string GetIkunType(OpParamInfo param)
    {
        if (param.IsNullableExprType) return "AlgebraNode";

        if (param.IsExprType) return "AlgebraNode";

        if (param.IsExprArrayType) return "AlgebraNode[]";

        if (param.IsExprCollectionType) return $"{GetExprCollectionTypeName(param.ExprCollectionKind)}<AlgebraNode>";

        if (param.IsExprDictionaryType)
            return $"{GetExprDictionaryTypeName(param.ExprDictionaryKind)}<{param.ExprDictionaryKeyType}, AlgebraNode>";

        if (param.IsFuncExprType)
        {
            var args = Enumerable.Range(0, param.FuncExprArgCount).Select(_ => "AlgebraNode");
            return $"Func<{string.Join(", ", args)}>";
        }

        return SimplifyType(param.OriginalType);
    }

    #endregion

    #region Matcher 鐢熸垚

    private void GenerateMatcher(GeneratorExecutionContext context, DialectInfo dialect)
    {
        var missingNodes = new List<string>();
        foreach (var op in dialect.Ops)
        {
            if (HasExistingGeneratedNamespaceNode(context.Compilation, dialect, op)) continue;
            missingNodes.Add(ToPascalCase(op.MethodName));
        }

        if (missingNodes.Count > 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("DG002", "DialectGenerator",
                    $"跳过 {dialect.PascalDialectName} Matcher 生成：缺少 Nodes 类型 {string.Join(", ", missingNodes)}，" +
                    $"可能存在手动定义的 Nodes（如 Web 方言的 WebElementNode），请检查命名空间 Nyar.Dialect.{dialect.PascalDialectName}.Nodes",
                    "Nyar.SourceGenerator", DiagnosticSeverity.Info, true),
                Location.None));
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using Nyar.IR.Intent;");
        sb.AppendLine("using Nyar.ObjectAlgebra;");
        sb.AppendLine($"using Nyar.Dialect.{dialect.PascalDialectName}.Nodes;");
        sb.AppendLine();
        sb.AppendLine($"namespace Nyar.ObjectAlgebra.Generated.{dialect.PascalDialectName};");
        sb.AppendLine();

        var className = $"{dialect.PascalDialectName}Matcher";
        sb.AppendLine($"        /// {dialect.PascalDialectName} 方言的强类型模式匹配");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"public static class {className}");
        sb.AppendLine("{");

        for (var i = 0; i < dialect.Ops.Count; i++)
        {
            var op = dialect.Ops[i];
            sb.AppendLine();
            GenerateMatcherMethod(sb, op, "    ");
        }

        sb.AppendLine("}");

        context.AddSource($"{dialect.PascalDialectName}.Matcher.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GenerateMatcherMethod(StringBuilder sb, OpInfo op, string indent)
    {
        GenerateTypedNodeMatcherMethod(sb, op, indent);
    }

    private static void GenerateTypedNodeMatcherMethod(StringBuilder sb, OpInfo op, string indent)
    {
        var baseClassName = ToPascalCase(op.MethodName);
        var className = baseClassName + op.TypeParams;

        var complexExprParams = op.Parameters.Where(p =>
            p.IsNullableExprType || p.IsExprArrayType || p.IsExprCollectionType || p.IsExprDictionaryType).ToList();
        if (complexExprParams.Count > 0)
        {
            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// 鍖归厤 {op.MethodName} 鑺傜偣");
            sb.AppendLine($"{indent}/// </summary>");
            sb.AppendLine($"{indent}public static bool Match{op.MethodName}{op.TypeParams}(AlgebraNode node)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    return node is global::Nyar.Dialect.{op.DialectPascalName}.Nodes.{className};");
            sb.AppendLine($"{indent}}}");
            return;
        }

        var exprParams = op.Parameters.Where(p => p.IsExprType && !p.IsNullableExprType).ToList();
        var nonExprParams = op.Parameters.Where(p =>
            !p.IsExprType && !p.IsFuncExprType && !p.IsExprArrayType && !p.IsExprCollectionType &&
            !p.IsExprDictionaryType).ToList();

        var exprOutParams = exprParams
            .Select(p => (Param: p, OutName: GetMatcherOutParameterName(p.Name)))
            .ToList();
        var nonExprOutParams = nonExprParams
            .Select(p => (Param: p, OutName: GetMatcherOutParameterName(p.Name)))
            .ToList();

        var outParams = new List<string>();
        foreach (var item in exprOutParams) outParams.Add($"out Id {item.OutName}");

        foreach (var item in nonExprOutParams)
            outParams.Add($"out {SimplifyType(item.Param.OriginalType)} {item.OutName}");

        if (outParams.Count == 0)
        {
            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// 鍖归厤 {op.MethodName} 鑺傜偣");
            sb.AppendLine($"{indent}/// </summary>");
            sb.AppendLine($"{indent}public static bool Match{op.MethodName}{op.TypeParams}(AlgebraNode node)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    return node is global::Nyar.Dialect.{op.DialectPascalName}.Nodes.{className};");
            sb.AppendLine($"{indent}}}");
            return;
        }

        var outParamsStr = string.Join(", ", outParams);

        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// 鍖归厤 {op.MethodName} 鑺傜偣");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine(
            $"{indent}public static bool Match{op.MethodName}{op.TypeParams}(AlgebraNode node, {outParamsStr})");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    if (node is global::Nyar.Dialect.{op.DialectPascalName}.Nodes.{className} n)");
        sb.AppendLine($"{indent}    {{");

        foreach (var item in exprOutParams)
            sb.AppendLine($"{indent}        {item.OutName} = n.{GetNodePropertyName(baseClassName, item.Param.Name)};");

        foreach (var item in nonExprOutParams)
            sb.AppendLine($"{indent}        {item.OutName} = n.{GetNodePropertyName(baseClassName, item.Param.Name)};");

        sb.AppendLine($"{indent}        return true;");
        sb.AppendLine($"{indent}    }}");

        foreach (var item in exprOutParams) sb.AppendLine($"{indent}    {item.OutName} = default;");

        foreach (var item in nonExprOutParams) sb.AppendLine($"{indent}    {item.OutName} = default;");

        sb.AppendLine($"{indent}    return false;");
        sb.AppendLine($"{indent}}}");
    }

    #endregion

    #region OperatorDescriptors 与 Symbols 生成

    private void GenerateOperatorDescriptors(GeneratorExecutionContext context, DialectInfo dialect)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using Nyar.ObjectAlgebra;");
        sb.AppendLine();
        sb.AppendLine($"namespace Nyar.ObjectAlgebra.Generated.{dialect.PascalDialectName};");
        sb.AppendLine();

        var className = $"{dialect.PascalDialectName}Symbols";

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {dialect.PascalDialectName} 方言的操作符符号表。");
        sb.AppendLine("/// 每个操作符对应一个 IOperatorDescriptor 实例，由 Source Generator 自动生成。");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public static class {className}");
        sb.AppendLine("{");

        var dialectIdVar = $"DialectId";
        sb.AppendLine($"    private static readonly Guid {dialectIdVar} = DialectIdHelper.compute(\"{dialect.DialectName}\");");
        sb.AppendLine();

        // 生成所有 Symbol 字段
        for (var i = 0; i < dialect.Ops.Count; i++)
        {
            var op = dialect.Ops[i];
            var fieldName = ToPascalCase(op.MethodName);
            var arity = CountExprParams(op);

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {op.MethodName} 操作符描述符");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public static readonly IOperatorDescriptor {fieldName} = new OperatorDescriptor(");
            sb.AppendLine($"        new OperatorKey({dialectIdVar}, {i}),");
            sb.AppendLine($"        \"{op.OpName}\",");
            sb.AppendLine($"        {arity},");
            sb.AppendLine($"        \"{dialect.DialectName}\");");
            sb.AppendLine();
        }

        // 生成 ByName 字典
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 按操作符名称查找描述符");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static readonly IReadOnlyDictionary<string, IOperatorDescriptor> ByName =");
        sb.AppendLine("        new Dictionary<string, IOperatorDescriptor>");
        sb.AppendLine("        {");
        for (var i = 0; i < dialect.Ops.Count; i++)
        {
            var op = dialect.Ops[i];
            var fieldName = ToPascalCase(op.MethodName);
            var comma = i < dialect.Ops.Count - 1 ? "," : "";
            sb.AppendLine($"            [\"{op.OpName}\"] = {fieldName}{comma}");
        }

        sb.AppendLine("        };");

        sb.AppendLine("}");

        context.AddSource($"{dialect.PascalDialectName}.Symbols.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static int CountExprParams(OpInfo op)
    {
        var count = 0;
        foreach (var p in op.Parameters)
        {
            if (p.IsExprType || p.IsExprArrayType || p.IsExprCollectionType ||
                p.IsExprDictionaryType || p.IsFuncExprType || p.IsNullableExprType)
            {
                if (p.IsExprArrayType || p.IsExprCollectionType)
                {
                    // 可变参数不计入固定 arity，用 -1 表示
                    return -1;
                }

                count++;
            }
        }

        return count;
    }

    #endregion

    #region ENode Reifier 生成

    private void GenerateReifier(GeneratorExecutionContext context, DialectInfo dialect)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Collections.Immutable;");
        sb.AppendLine("using Nyar.EGraph;");
        sb.AppendLine("using Nyar.IR.Intent;");
        sb.AppendLine("using Nyar.ObjectAlgebra;");
        sb.AppendLine($"using Nyar.ObjectAlgebra.Generated.{dialect.PascalDialectName};");
        sb.AppendLine();
        sb.AppendLine($"namespace Nyar.ObjectAlgebra.Generated.{dialect.PascalDialectName};");
        sb.AppendLine();

        var className = $"{dialect.PascalDialectName}Reifier";
        var algName = dialect.AlgInterfaceName;

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {dialect.PascalDialectName} 方言的 ENode Reifier。");
        sb.AppendLine($"/// 将 OA algebra 调用映射为开放 ENode 节点并写入 EGraph<ENode>。");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public sealed class {className} : {algName}<Id>, IReifier<Id>");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly EGraph<ENode> _egraph;");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 初始化 Reifier 实例");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <param name=\"egraph\">目标 EGraph<ENode> 实例。</param>");
        sb.AppendLine($"    public {className}(EGraph<ENode> egraph)");
        sb.AppendLine("    {");
        sb.AppendLine("        _egraph = egraph;");
        sb.AppendLine("        Symbols = new Dictionary<string, IOperatorDescriptor>(");
        sb.AppendLine($"            {dialect.PascalDialectName}Symbols.ByName);");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public IReadOnlyDictionary<string, IOperatorDescriptor> Symbols { get; }");
        sb.AppendLine();

        // 生成每个操作符的 reify 方法
        for (var i = 0; i < dialect.Ops.Count; i++)
        {
            var op = dialect.Ops[i];
            sb.AppendLine();
            GenerateReifierMethod(sb, op, dialect, "    ");
        }

        sb.AppendLine("}");

        context.AddSource($"{dialect.PascalDialectName}.Reifier.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GenerateReifierMethod(StringBuilder sb, OpInfo op, DialectInfo dialect, string indent)
    {
        var paramList = new List<string>();

        foreach (var p in op.Parameters)
        {
            var reifierType = GetReifierType(p);
            paramList.Add($"{reifierType} {p.Name}");
        }

        var paramStr = paramList.Count > 0 ? string.Join(", ", paramList) : "";

        var exprParams = op.Parameters.Where(p =>
            p.IsExprType || p.IsNullableExprType || p.IsExprArrayType ||
            p.IsExprCollectionType).ToList();

        var dictParams = op.Parameters.Where(p => p.IsExprDictionaryType).ToList();

        var nonExprParams = op.Parameters.Where(p =>
            !p.IsExprType && !p.IsNullableExprType && !p.IsExprArrayType &&
            !p.IsExprCollectionType && !p.IsExprDictionaryType).ToList();

        var fieldName = ToPascalCase(op.MethodName);
        var escapedMethodName = EscapeKeyword(op.MethodName);
        var hasNonExprParams = nonExprParams.Count > 0;

        sb.AppendLine($"{indent}public Id {escapedMethodName}{op.TypeParams}({paramStr})");
        sb.AppendLine($"{indent}{{");

        // 构建 children 列表
        var totalCollectionParams = exprParams.Count + dictParams.Count;

        if (totalCollectionParams == 0)
        {
            sb.AppendLine($"{indent}    var _children = ImmutableArray<Id>.Empty;");
        }
        else if (totalCollectionParams == 1 && exprParams.Count == 1 && !exprParams[0].IsExprArrayType && !exprParams[0].IsExprCollectionType && dictParams.Count == 0)
        {
            sb.AppendLine($"{indent}    var _children = ImmutableArray.Create({exprParams[0].Name});");
        }
        else
        {
            var childListName = "childList";
            sb.AppendLine($"{indent}    var {childListName} = new List<Id>();");
            foreach (var p in exprParams)
            {
                if (p.IsExprType && !p.IsNullableExprType)
                {
                    sb.AppendLine($"{indent}    {childListName}.Add({p.Name});");
                }
                else if (p.IsNullableExprType)
                {
                    sb.AppendLine($"{indent}    {childListName}.Add({p.Name});");
                }
                else if (p.IsExprArrayType)
                {
                    sb.AppendLine($"{indent}    {childListName}.AddRange({p.Name});");
                }
                else if (p.IsExprCollectionType)
                {
                    sb.AppendLine($"{indent}    {childListName}.AddRange({p.Name});");
                }
            }

            foreach (var p in dictParams)
            {
                sb.AppendLine($"{indent}    foreach (var __dictVal in {p.Name}.Values)");
                sb.AppendLine($"{indent}    {{");
                sb.AppendLine($"{indent}        {childListName}.Add(__dictVal);");
                sb.AppendLine($"{indent}    }}");
            }

            sb.AppendLine($"{indent}    var _children = {childListName}.ToImmutableArray();");
        }

        // 构建 payload（非表达式参数，包装为匿名类型）
        if (hasNonExprParams)
        {
            var payloadParts = string.Join(", ", nonExprParams.Select(p => $"{p.Name}"));
            sb.AppendLine($"{indent}    var _payload = new {{ {payloadParts} }};");
        }

        var payloadArg = hasNonExprParams ? ", _payload" : "";
        sb.AppendLine(
            $"{indent}    var enode = new ENode({dialect.PascalDialectName}Symbols.{fieldName}, _children{payloadArg});");
        sb.AppendLine($"{indent}    return _egraph.add(enode);");
        sb.AppendLine($"{indent}}}");
    }

    private static string GetReifierType(OpParamInfo param)
    {
        if (param.IsNullableExprType) return "Id";
        if (param.IsExprType) return "Id";
        if (param.IsExprArrayType) return "Id[]";
        if (param.IsExprCollectionType) return $"{GetExprCollectionTypeName(param.ExprCollectionKind)}<Id>";
        if (param.IsExprDictionaryType)
            return $"{GetExprDictionaryTypeName(param.ExprDictionaryKind)}<{param.ExprDictionaryKeyType}, Id>";
        if (param.IsFuncExprType)
        {
            var args = Enumerable.Range(0, param.FuncExprArgCount).Select(_ => "Id");
            return $"Func<{string.Join(", ", args)}>";
        }

        return SimplifyType(param.OriginalType);
    }

    #endregion

    #region PatternAlg 生成

    private void GeneratePatternAlg(GeneratorExecutionContext context, DialectInfo dialect)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using Nyar.ObjectAlgebra;");
        sb.AppendLine();
        sb.AppendLine($"namespace Nyar.ObjectAlgebra.Generated.{dialect.PascalDialectName};");
        sb.AppendLine();

        var patternAlgName = $"I{dialect.PascalDialectName}PatternAlg";
        var baseAlgName = dialect.AlgInterfaceName;

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {dialect.PascalDialectName} 方言的模式代数接口。");
        sb.AppendLine("/// 用于在规则 DSL 中描述可匹配的模式。");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public interface {patternAlgName}<P> : {baseAlgName}<P>, IPatternAlg<P>");
        sb.AppendLine("{");
        sb.AppendLine("}");

        context.AddSource($"{dialect.PascalDialectName}.PatternAlg.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    #endregion

    #region Rule DSL 生成

    private void GenerateRuleDSL(GeneratorExecutionContext context, DialectInfo dialect)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using Nyar.ObjectAlgebra;");
        sb.AppendLine("using Nyar.IR.Rewrite;");
        sb.AppendLine();
        sb.AppendLine($"namespace Nyar.ObjectAlgebra.Generated.{dialect.PascalDialectName};");
        sb.AppendLine();

        var ruleClassName = $"{dialect.PascalDialectName}Rules";

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {dialect.PascalDialectName} 方言的规则 DSL 入口。");
        sb.AppendLine("/// 提供创建模式的便捷方法和规则构建器。");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public static class {ruleClassName}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 创建一个新的模式构建器");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public static IPatternBuilder<P> Pattern<P>()");
        sb.AppendLine("    {");
        sb.AppendLine($"        return new {dialect.PascalDialectName}PatternBuilder<P>();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 创建匹配同名变量的重写规则");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <param name=\"pattern\">模式表达式。</param>");
        sb.AppendLine("    /// <param name=\"replacement\">替换表达式。</param>");
        sb.AppendLine("    /// <param name=\"name\">规则名称。</param>");
        sb.AppendLine("    public static RewriteRule<ENode> Rule(ENode pattern, ENode replacement, string name)");
        sb.AppendLine("    {");
        sb.AppendLine("        return RewriteRule<ENode>.create(name, pattern, replacement);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();

        // 生成 PatternBuilder 内部类
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {dialect.PascalDialectName} 方言的模式构建器");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public sealed class {dialect.PascalDialectName}PatternBuilder<P> : IPatternBuilder<P>");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly Dictionary<string, P> _bindings = new();");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public P Any(string name)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!_bindings.TryGetValue(name, out var binding))");
        sb.AppendLine("        {");
        sb.AppendLine("            // 返回占位符，实际绑定在匹配时完成");
        sb.AppendLine("            binding = default!;");
        sb.AppendLine("            _bindings[name] = binding;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return binding;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource($"{dialect.PascalDialectName}.RuleDSL.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    #endregion

    #region 寮虹被鍨嬭妭鐐圭敓鎴?

    private void GenerateNodes(GeneratorExecutionContext context, DialectInfo dialect)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using Nyar.IR.Intent;");
        sb.AppendLine();
        sb.AppendLine($"namespace Nyar.Dialect.{dialect.PascalDialectName}.Nodes;");
        sb.AppendLine();

        var hasAnyOp = false;

        foreach (var op in dialect.Ops)
        {
            if (HasExistingGeneratedNamespaceNode(context.Compilation, dialect, op)) continue;

            // 跳过 Literal 节点生成：Nyar.IR.Intent.Literal<T> 已手动定义泛型版本，
            // 生成的非泛型 Literal 记录类会造成类型歧义
            if (op.MethodName == "literal")
            {
                continue;
            }

            hasAnyOp = true;
            GenerateNodeClass(sb, op);
            sb.AppendLine();
        }

        if (!hasAnyOp) return;

        context.AddSource($"{dialect.PascalDialectName}.Nodes.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static bool HasExistingGeneratedNamespaceNode(Compilation compilation, DialectInfo dialect, OpInfo op)
    {
        var className = ToPascalCase(op.MethodName);
        var metadataName = $"Nyar.Dialect.{dialect.PascalDialectName}.Nodes.{className}";
        return compilation.GetTypeByMetadataName(metadataName) is not null;
    }

    /// <summary>
    ///     检查编译中是否已存在指定元数据名称的类型
    /// </summary>
    /// <param name="compilation">编译上下文。</param>
    /// <param name="metadataName">类型的完整元数据名称。</param>
    /// <returns>如果类型已存在则返回 true。</returns>
    private static bool HasExistingType(Compilation compilation, string metadataName)
    {
        return compilation.GetTypeByMetadataName(metadataName) is not null;
    }

    private static void GenerateNodeClass(StringBuilder sb, OpInfo op)
    {
        var className = ToPascalCase(op.MethodName) + op.TypeParams;
        var constructorParams = new List<string>();

        foreach (var p in op.Parameters)
        {
            var nodeType = GetNodeType(p);
            var nodeName = GetNodePropertyName(ToPascalCase(op.MethodName), p.Name);
            constructorParams.Add($"{nodeType} {nodeName}");
        }

        var paramStr = string.Join(", ", constructorParams);

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {op.OpName} 鎿嶄綔鐨勫己绫诲瀷 IR 鑺傜偣锛岀敱 DialectGenerator 浠?OA 鎺ュ彛鑷姩鐢熸垚");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("[AlgebraNode]");
        sb.AppendLine($"public sealed partial record {className}({paramStr}) : AlgebraNode;");
    }

    private static string GetNodeType(OpParamInfo param)
    {
        if (param.IsNullableExprType) return "Id";

        if (param.IsExprType) return "Id";

        if (param.IsExprArrayType) return "Id[]";

        if (param.IsExprCollectionType) return $"{GetExprCollectionTypeName(param.ExprCollectionKind)}<Id>";

        if (param.IsExprDictionaryType)
            return $"{GetExprDictionaryTypeName(param.ExprDictionaryKind)}<{param.ExprDictionaryKeyType}, Id>";

        return SimplifyType(param.OriginalType);
    }

    private static string GetNodePropertyName(string className, string parameterName)
    {
        // Nyar 节点类型使用 snake_case 属性名，直接使用参数名即可
        var propertyName = parameterName;
        if (string.Equals(propertyName, className, StringComparison.Ordinal))
            return EscapeKeyword($"{propertyName}Value");

        return EscapeKeyword(propertyName);
    }

    private static string GetMatcherOutParameterName(string parameterName)
    {
        var name = string.Equals(parameterName, "node", StringComparison.Ordinal)
            ? "nodeValue"
            : parameterName;
        return EscapeKeyword(name);
    }

    /// <summary>
    ///     C# 保留关键字集合，用于在生成代码时对关键字进行转义
    /// </summary>
    private static readonly HashSet<string> CSharpKeywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double",
        "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float",
        "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal",
        "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out",
        "override", "params", "private", "protected", "public", "readonly", "ref", "return",
        "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct",
        "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
        "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
    ];

    /// <summary>
    ///     如果名称为 C# 保留关键字，则添加 @ 前缀进行转义
    /// </summary>
    /// <param name="name">原始名称。</param>
    /// <returns>转义后的名称。</returns>
    private static string EscapeKeyword(string name)
    {
        return CSharpKeywords.Contains(name) ? $"@{name}" : name;
    }

    #endregion

    #region Builtin 生成

    /// <summary>
    ///     从方言 [Operator] 方法生成枚举值。
    ///     使用确定性 ID（基础偏移 + 操作索引）。
    /// </summary>
    private void GenerateBuiltin(GeneratorExecutionContext context, DialectInfo dialect)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine();
        sb.AppendLine("namespace Nyar.Dialect." + dialect.PascalDialectName + ".Rules;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("///     " + dialect.PascalDialectName + " 方言内置函数 ID（映射到 Literal&lt;long&gt; 的 long 值）");
        sb.AppendLine("///     ID 范围: 0x9001 ~ 0x90FF，由 SourceGenerator 自动生成");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public enum " + dialect.PascalDialectName + "Builtin : long");
        sb.AppendLine("{");

        for (var i = 0; i < dialect.Ops.Count; i++)
        {
            var op = dialect.Ops[i];
            var fieldName = ToPascalCase(op.MethodName);
            var id = 0x9001 + i;
            sb.AppendLine("    " + fieldName + " = 0x" + id.ToString("X4") + ",");
        }

        sb.AppendLine("}");

        context.AddSource(dialect.PascalDialectName + ".Builtin.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    #endregion

    #region CostHook 生成

    /// <summary>
    ///     从方言 [Operator] 方法生成 CanHandle 和 Estimate 方法。
    ///     按操作语义分类估算延迟。
    /// </summary>
    private void GenerateCostHook(GeneratorExecutionContext context, DialectInfo dialect)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using Nyar.Dialect." + dialect.PascalDialectName + ".Nodes;");
        sb.AppendLine("using Nyar.IR.Intent;");
        sb.AppendLine("using Nyar.Optimizer.CostModels;");
        sb.AppendLine("using Nyar.Types;");
        sb.AppendLine();
        sb.AppendLine("namespace Nyar.Dialect." + dialect.PascalDialectName + ".Cost;");
        sb.AppendLine();
        sb.AppendLine("public sealed partial class " + dialect.PascalDialectName + "CostHook");
        sb.AppendLine("{");

        // CanHandle 方法
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    ///     判断是否可以处理该节点");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public bool CanHandle(AlgebraNode node)");
        sb.AppendLine("    {");
        sb.Append("        return node is ");
        for (var i = 0; i < dialect.Ops.Count; i++)
        {
            var op = dialect.Ops[i];
            var className = ToPascalCase(op.MethodName) + op.TypeParams;
            if (i > 0)
            {
                sb.Append(" or ");
            }

            sb.Append(className);
        }

        sb.AppendLine(";");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Estimate 方法
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    ///     估算节点成本");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public CostVector Estimate(AlgebraNode node)");
        sb.AppendLine("    {");
        sb.AppendLine("        return node switch");
        sb.AppendLine("        {");

        // 分类估算
        var pureOps = new HashSet<string> { "Cast", "Trunc", "ZExt", "SExt", "BitAnd", "BitOr", "BitXor", "Shl", "LShr", "AShr" };
        var stringOps = new HashSet<string> { "StringConcat", "StringLength", "StringSubstring", "StringCompare", "StringFormat" };
        var cheapOps = new HashSet<string> { "ArrayLength", "StructDeclare" };
        var atomOps = new HashSet<string> { "AtomicCas", "AtomicLoad", "AtomicStore" };

        foreach (var op in dialect.Ops)
        {
            var className = ToPascalCase(op.MethodName) + op.TypeParams;
            var methodName = ToPascalCase(op.MethodName);
            int latency;

            if (pureOps.Contains(methodName))
            {
                latency = 1;
            }
            else if (stringOps.Contains(methodName))
            {
                latency = methodName switch
                {
                    "StringConcat" => 10,
                    "StringSubstring" => 8,
                    _ => 5
                };
            }
            else if (cheapOps.Contains(methodName))
            {
                latency = 1;
            }
            else if (atomOps.Contains(methodName))
            {
                latency = 5;
            }
            else
            {
                latency = 2;
            }

            sb.AppendLine("            " + className + " => CostVector.from_latency(" + latency + "),");
        }

        sb.AppendLine("            _ => CostVector.zero");
        sb.AppendLine("        };");
        sb.AppendLine("    }");

        sb.AppendLine("}");

        context.AddSource(dialect.PascalDialectName + ".CostHook.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    #endregion

    #region 杈呭姪鏂规硶

    private static string SimplifyType(string fullName)
    {
        return fullName
            .Replace("global::System.Int32", "int")
            .Replace("global::System.Int64", "long")
            .Replace("global::System.Boolean", "bool")
            .Replace("global::System.String", "string")
            .Replace("global::System.Double", "double")
            .Replace("global::System.Single", "float")
            .Replace("global::System.Void", "void")
            .Replace("global::", "");
    }

    private static string GetExprCollectionTypeName(ExprCollectionKind kind)
    {
        return kind switch
        {
            ExprCollectionKind.ReadOnlyList => "IReadOnlyList",
            ExprCollectionKind.ImmutableArray => "System.Collections.Immutable.ImmutableArray",
            _ => throw new InvalidOperationException("未知的表达式集合类型。")
        };
    }

    private static string GetExprDictionaryTypeName(ExprDictionaryKind kind)
    {
        return kind switch
        {
            ExprDictionaryKind.ReadOnlyDictionary => "IReadOnlyDictionary",
            _ => throw new InvalidOperationException("未知的表达式字典类型。")
        };
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        if (!name.Any(ch => ch is '_' or '-' or ' ')) return char.ToUpperInvariant(name[0]) + name.Substring(1);

        var sb = new StringBuilder(name.Length);
        var uppercaseNext = true;

        foreach (var ch in name)
        {
            if (ch is '_' or '-' or ' ')
            {
                uppercaseNext = true;
                continue;
            }

            sb.Append(uppercaseNext ? char.ToUpperInvariant(ch) : ch);
            uppercaseNext = false;
        }

        return sb.ToString();
    }

    #endregion

    #region 鏁版嵁绫?

    private enum OpCategory
    {
        Literal,
        Unary,
        Binary,
        Extension
    }

    private enum ExprCollectionKind
    {
        None,
        ReadOnlyList,
        ImmutableArray
    }

    private enum ExprDictionaryKind
    {
        None,
        ReadOnlyDictionary
    }

    private sealed class DialectInfo
    {
        public DialectInfo(INamedTypeSymbol interfaceSymbol, string dialectName, List<OpInfo> ops)
        {
            InterfaceSymbol = interfaceSymbol;
            DialectName = dialectName;
            PascalDialectName = ToPascalCase(dialectName);

            var baseName = interfaceSymbol.Name.StartsWith("I")
                ? interfaceSymbol.Name.Substring(1)
                : interfaceSymbol.Name;
            AlgInterfaceName = $"I{baseName}Alg";

            Ops = ops;
            foreach (var op in Ops) op.DialectPascalName = PascalDialectName;
        }

        public INamedTypeSymbol InterfaceSymbol { get; }
        public string DialectName { get; }
        public string PascalDialectName { get; }
        public string AlgInterfaceName { get; }
        public List<OpInfo> Ops { get; }
    }

    private sealed class OpInfo
    {
        public OpInfo(string methodName, string opName, OpCategory category,
            string returnExprTypeArg, List<OpParamInfo> parameters, string typeParams = "")
        {
            MethodName = methodName;
            OpName = opName;
            Category = category;
            ReturnExprTypeArg = returnExprTypeArg;
            Parameters = parameters;
            TypeParams = typeParams;
        }

        public string MethodName { get; }
        public string OpName { get; }
        public OpCategory Category { get; }
        public string ReturnExprTypeArg { get; }
        public List<OpParamInfo> Parameters { get; }
        public string TypeParams { get; }
        public string DialectPascalName { get; set; } = string.Empty;
    }

    private sealed class OpParamInfo
    {
        public OpParamInfo(string name, string originalType, bool isExprType, bool isNullableExprType,
            string exprTypeArg, bool isFuncExprType, int funcExprArgCount, bool isExprArrayType,
            string exprArrayElementType, bool isExprCollectionType, ExprCollectionKind exprCollectionKind,
            string exprCollectionElementType, bool isExprDictionaryType, ExprDictionaryKind exprDictionaryKind,
            string exprDictionaryKeyType, bool isPlainArrayType)
        {
            Name = name;
            OriginalType = originalType;
            IsExprType = isExprType;
            IsNullableExprType = isNullableExprType;
            ExprTypeArg = exprTypeArg;
            IsFuncExprType = isFuncExprType;
            FuncExprArgCount = funcExprArgCount;
            IsExprArrayType = isExprArrayType;
            IsExprCollectionType = isExprCollectionType;
            ExprCollectionKind = exprCollectionKind;
            ExprCollectionElementType = exprCollectionElementType;
            IsExprDictionaryType = isExprDictionaryType;
            ExprDictionaryKind = exprDictionaryKind;
            ExprDictionaryKeyType = exprDictionaryKeyType;
            IsPlainArrayType = isPlainArrayType;
            ExprArrayElementType = exprArrayElementType;
        }

        public string Name { get; }
        public string OriginalType { get; }
        public bool IsExprType { get; }
        public bool IsNullableExprType { get; }
        public string ExprTypeArg { get; }
        public bool IsFuncExprType { get; }
        public int FuncExprArgCount { get; }
        public bool IsExprArrayType { get; }
        public string ExprArrayElementType { get; }
        public bool IsExprCollectionType { get; }
        public ExprCollectionKind ExprCollectionKind { get; }
        public string ExprCollectionElementType { get; }
        public bool IsExprDictionaryType { get; }
        public ExprDictionaryKind ExprDictionaryKind { get; }
        public string ExprDictionaryKeyType { get; }
        public bool IsPlainArrayType { get; }
    }

    #endregion
}
