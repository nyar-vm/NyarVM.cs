using System.Collections;
using System.Globalization;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Pattern;
using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Nyar.Language.Valkyrie.Formatter;

/// <summary>
///     Valkyrie 代码格式化器，将 AST 还原为格式化后的源代码文本
/// </summary>
public static class ValkyrieFormatter
{
    private const string IndentString = "    ";

    /// <summary>
    ///     将编译单元 AST 格式化为源代码字符串
    /// </summary>
    /// <param name="unit">编译单元根节点</param>
    /// <returns>格式化后的源代码</returns>
    public static string Format(CompilationUnit unit)
    {
        var writer = new StringWriter(CultureInfo.InvariantCulture);
        var indent = 0;

        WriteDeclarations(writer, unit.declarations, ref indent);
        return writer.ToString();
    }

    #region 类型格式化

    private static void WriteType(TextWriter writer, TypeNode node)
    {
        switch (node)
        {
            case TypeLiteralNamePathNode namePath:
                WriteQualifiedPath(writer, namePath.path);
                WriteTypeArguments(writer, namePath.type_arguments);
                return;
            case TypeExpressionBinaryNode binary:
                WriteType(writer, binary.lhs);
                writer.Write(" ");
                writer.Write(GetTypeBinaryOpString(binary.@operator));
                writer.Write(" ");
                WriteType(writer, binary.rhs);
                return;
            case TypeExpressionUnaryNode unary:
                if (unary.is_prefix) writer.Write(GetTypeUnaryOpString(unary.@operator));

                WriteType(writer, unary.operand);

                if (!unary.is_prefix) writer.Write(GetTypeUnaryOpString(unary.@operator));

                return;
            case TypeMicroNode micro:
                writer.Write("micro(");
                WriteType(writer, micro.parameter_type);
                writer.Write(") -> ");
                WriteType(writer, micro.return_type);
                return;
            case TypeLiteralArrayNode:
                writer.Write("[]");
                return;
            case TypeLiteralTupleNode tuple:
                writer.Write("(");
                for (var i = 0; i < tuple.elements.Count; i++)
                {
                    if (i > 0)
                    {
                        writer.Write(", ");
                    }

                    var element = tuple.elements[i];
                    if (element.label is not null)
                    {
                        writer.Write(element.label.name);
                        writer.Write(": ");
                    }

                    WriteType(writer, element.type);
                }

                if (tuple.elements.Count == 1)
                {
                    writer.Write(",");
                }

                writer.Write(")");
                return;
            case TypeLiteralNullNode:
                writer.Write("null");
                return;
            case TypeLiteralNumberNode literalNumber:
                writer.Write(literalNumber.value.ToString(CultureInfo.InvariantCulture));
                return;
            case TypeLiteralTextNode:
                writer.Write("utf8");
                return;
            case TypeLiteralBooleanNode:
                writer.Write("bool");
                return;
            default:
                writer.Write(node.GetType().Name);
                return;
        }
    }

    #endregion

    #region 声明格式化

    private static void WriteDeclarations(TextWriter writer, IReadOnlyList<AstNode> declarations, ref int indent)
    {
        for (var i = 0; i < declarations.Count; i++)
        {
            if (i > 0)
            {
                writer.WriteLine();
                writer.WriteLine();
            }

            WriteDeclaration(writer, declarations[i], ref indent);
        }
    }

    private static void WriteDeclaration(TextWriter writer, AstNode node, ref int indent)
    {
        switch (node)
        {
            case FunctionDecl declareMicro:
                WriteMicro(writer, declareMicro, ref indent);
                return;
            case LetDeclaration declareLet:
                WriteLet(writer, declareLet, ref indent);
                writer.WriteLine(";");
                return;
            case StructureDecl declareStructure:
                WriteStructure(writer, declareStructure, ref indent);
                return;
            case ClassDecl declareClass:
                WriteClass(writer, declareClass, ref indent);
                return;
            case TraitDecl declareTrait:
                WriteTrait(writer, declareTrait, ref indent);
                return;
            case NamespaceDecl declareNamespace:
                WriteNamespace(writer, declareNamespace, ref indent);
                return;
            case EnumDecl declareEnums:
                WriteEnums(writer, declareEnums, ref indent);
                return;
            case FlagsDecl declareFlags:
                WriteFlags(writer, declareFlags, ref indent);
                return;
            case UniteDecl declareUnite:
                WriteUnite(writer, declareUnite, ref indent);
                return;
            case ImportDecl declareUsing:
                WriteUsing(writer, declareUsing, ref indent);
                writer.WriteLine(";");
                return;
            case ComponentDeclaration declareComponent:
                WriteComponent(writer, declareComponent, ref indent);
                return;
            case SystemDeclaration declareSystem:
                WriteSystem(writer, declareSystem, ref indent);
                return;
            case WidgetDecl declareWidget:
                WriteWidget(writer, declareWidget, ref indent);
                return;
            case DeclareMacro declareMacro:
                WriteIndent(writer, indent);
                writer.Write($"macro {GetName(declareMacro.name)};");
                return;
            case DeclareMezzo declareMezzo:
                WriteIndent(writer, indent);
                writer.Write($"mezzo {GetName(declareMezzo.name)};");
                return;
            case DeclareImply declareImply:
                WriteImply(writer, declareImply, ref indent);
                return;
            case DeclareTraitAlias declareTraitAlias:
                WriteIndent(writer, indent);
                writer.Write("trait ");
                writer.Write(GetName(declareTraitAlias.name));
                writer.Write(" = ");
                WriteType(writer, declareTraitAlias.target_type);
                writer.Write(";");
                return;
            case PluginDecl pluginDecl:
                WritePlugin(writer, pluginDecl, ref indent);
                return;
            case UnionDecl unionDecl:
                WriteUnion(writer, unionDecl, ref indent);
                return;
            case TypeAliasDecl typeAliasDecl:
                WriteIndent(writer, indent);
                writer.Write("typealias ");
                writer.Write(GetName(typeAliasDecl.name));
                writer.Write(" = ");
                WriteType(writer, typeAliasDecl.target_type);
                writer.Write(";");
                return;
            default:
                WriteFallback(writer, node, ref indent);
                return;
        }
    }

    #endregion

    #region 具体声明类型

    private static void WriteMicro(TextWriter writer, FunctionDecl micro, ref int indent)
    {
        WriteAnnotations(writer, micro.annotations, ref indent);

        WriteIndent(writer, indent);
        writer.Write("micro ");
        writer.Write(GetName(micro.name));

        WriteTypeParameters(writer, micro.type_parameters);

        writer.Write("(");
        WriteParameterLists(writer, micro.parameters);
        writer.Write(")");

        if (micro.return_type is not null)
        {
            writer.Write(" -> ");
            WriteType(writer, micro.return_type);
        }

        WriteGenericConstraints(writer, micro.generic_constraints);

        if (micro.body is not null)
        {
            writer.WriteLine();
            WriteBlock(writer, micro.body, ref indent);
        }
        else
        {
            writer.Write(";");
        }
    }

    private static void WriteLet(TextWriter writer, LetDeclaration declareLet, ref int indent)
    {
        WriteAnnotations(writer, declareLet.annotations, ref indent);

        WriteIndent(writer, indent);

        if (declareLet.is_mutable)
            writer.Write("var ");
        else
            writer.Write("let ");

        writer.Write(GetName(declareLet.name));

        if (declareLet.var_type is not null)
        {
            writer.Write(": ");
            WriteType(writer, declareLet.var_type);
        }

        if (declareLet.initializer is not null)
        {
            writer.Write(" = ");
            WriteExpression(writer, declareLet.initializer);
        }
    }

    private static void WriteStructure(TextWriter writer, StructureDecl structure, ref int indent)
    {
        WriteAnnotations(writer, structure.annotations, ref indent);

        WriteIndent(writer, indent);
        writer.Write("structure ");
        writer.Write(GetName(structure.name));

        WriteTypeParameters(writer, structure.type_parameters);
        WriteGenericConstraints(writer, structure.generic_constraints);

        if (structure.body is not null)
        {
            writer.WriteLine();
            WriteObjectBody(writer, structure.body, ref indent);
        }
        else
        {
            writer.Write(";");
        }
    }

    private static void WriteClass(TextWriter writer, ClassDecl declareClass, ref int indent)
    {
        WriteAnnotations(writer, declareClass.annotations, ref indent);

        WriteIndent(writer, indent);
        writer.Write("class ");
        writer.Write(GetName(declareClass.name));

        WriteTypeParameters(writer, declareClass.type_parameters);

        if (declareClass.inheritance is not null && declareClass.inheritance.bases.Count > 0)
        {
            writer.Write("(");
            for (var i = 0; i < declareClass.inheritance.bases.Count; i++)
            {
                if (i > 0) writer.Write(", ");

                var item = declareClass.inheritance.bases[i];
                if (item.name is not null)
                {
                    writer.Write(GetName(item.name));
                    writer.Write(": ");
                }

                WriteType(writer, item.base_type);
            }

            writer.Write(")");
        }

        WriteGenericConstraints(writer, declareClass.generic_constraints);

        if (declareClass.body is not null)
        {
            writer.WriteLine();
            WriteObjectBody(writer, declareClass.body, ref indent);
        }
        else
        {
            writer.Write(";");
        }
    }

    private static void WriteTrait(TextWriter writer, TraitDecl declareTrait, ref int indent)
    {
        WriteAnnotations(writer, declareTrait.annotations, ref indent);

        WriteIndent(writer, indent);
        writer.Write("trait ");
        writer.Write(GetName(declareTrait.name));

        WriteTypeParameters(writer, declareTrait.type_parameters);

        if (declareTrait.inheritance is not null && declareTrait.inheritance.bases.Count > 0)
        {
            writer.Write(": ");
            for (var i = 0; i < declareTrait.inheritance.bases.Count; i++)
            {
                if (i > 0) writer.Write(" + ");

                WriteType(writer, declareTrait.inheritance.bases[i].base_type);
            }
        }

        WriteGenericConstraints(writer, declareTrait.generic_constraints);

        if (declareTrait.body is not null)
        {
            writer.WriteLine();
            WriteObjectBody(writer, declareTrait.body, ref indent);
        }
        else
        {
            writer.Write(";");
        }
    }

    private static void WriteNamespace(TextWriter writer, NamespaceDecl declareNamespace, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.Write("namespace ");
        writer.Write(declareNamespace.name.name);

        if (declareNamespace.is_primary) writer.Write(" primary");

        if (declareNamespace.is_test) writer.Write(" test");

        if (declareNamespace.declarations.Count > 0)
        {
            writer.WriteLine();
            WriteIndent(writer, indent);
            writer.WriteLine("{");
            indent++;
            WriteDeclarations(writer, declareNamespace.declarations, ref indent);
            indent--;
            writer.WriteLine();
            WriteIndent(writer, indent);
            writer.WriteLine("}");
        }
        else
        {
            writer.Write(";");
        }
    }

    private static void WriteEnums(TextWriter writer, EnumDecl declareEnums, ref int indent)
    {
        WriteAnnotations(writer, declareEnums.annotations, ref indent);

        WriteIndent(writer, indent);
        writer.Write("enums ");
        writer.Write(declareEnums.name.name);

        if (declareEnums.members.Count > 0)
        {
            writer.WriteLine();
            WriteIndent(writer, indent);
            writer.WriteLine("{");
            indent++;
            for (var i = 0; i < declareEnums.members.Count; i++)
            {
                WriteIndent(writer, indent);
                writer.Write(GetName(declareEnums.members[i].name));
                if (declareEnums.members[i].value is not null)
                {
                    writer.Write(" = ");
                    WriteExpression(writer, declareEnums.members[i].value!);
                }

                if (i < declareEnums.members.Count - 1) writer.Write(",");

                writer.WriteLine();
            }

            indent--;
            WriteIndent(writer, indent);
            writer.Write("}");
        }
        else
        {
            writer.Write(" {}");
        }
    }

    private static void WriteFlags(TextWriter writer, FlagsDecl declareFlags, ref int indent)
    {
        WriteAnnotations(writer, declareFlags.annotations, ref indent);

        WriteIndent(writer, indent);
        writer.Write("flags ");
        writer.Write(declareFlags.name.name);

        if (declareFlags.members.Count > 0)
        {
            writer.WriteLine();
            WriteIndent(writer, indent);
            writer.WriteLine("{");
            indent++;
            for (var i = 0; i < declareFlags.members.Count; i++)
            {
                WriteIndent(writer, indent);
                writer.Write(GetName(declareFlags.members[i].name));
                if (declareFlags.members[i].value is not null)
                {
                    writer.Write(" = ");
                    WriteExpression(writer, declareFlags.members[i].value!);
                }

                if (i < declareFlags.members.Count - 1) writer.Write(",");

                writer.WriteLine();
            }

            indent--;
            WriteIndent(writer, indent);
            writer.Write("}");
        }
        else
        {
            writer.Write(" {}");
        }
    }

    private static void WriteUnite(TextWriter writer, UniteDecl declareUnite, ref int indent)
    {
        WriteAnnotations(writer, declareUnite.annotations, ref indent);

        WriteIndent(writer, indent);
        writer.Write("unite ");
        writer.Write(GetName(declareUnite.name));

        WriteTypeParameters(writer, declareUnite.type_parameters);

        writer.WriteLine();
        WriteIndent(writer, indent);
        writer.WriteLine("{");
        indent++;

        for (var i = 0; i < declareUnite.variants.Count; i++)
        {
            WriteUniteVariant(writer, declareUnite.variants[i], ref indent);
            if (i < declareUnite.variants.Count - 1) writer.Write(",");

            writer.WriteLine();
        }

        if (declareUnite.methods.Count > 0)
        {
            writer.WriteLine();
            for (var i = 0; i < declareUnite.methods.Count; i++)
                WriteObjectMethod(writer, declareUnite.methods[i], ref indent);
        }

        indent--;
        WriteIndent(writer, indent);
        writer.Write("}");
    }

    private static void WriteUniteVariant(TextWriter writer, DeclareUniteVariant variant, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.Write(GetName(variant.name));

        if (variant.body is not null) WriteObjectBodyInline(writer, variant.body, ref indent);
    }

    private static void WriteUsing(TextWriter writer, DeclareUsing declareUsing, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.Write("using ");

        if (!string.IsNullOrEmpty(declareUsing.module_path)) writer.Write(declareUsing.module_path);

        if (declareUsing.@namespace is not null && declareUsing.@namespace.segments.Count > 0)
        {
            if (!string.IsNullOrEmpty(declareUsing.module_path)) writer.Write(".");

            writer.Write(declareUsing.@namespace.full_name);
        }

        if (declareUsing.selections.Count > 0)
        {
            writer.Write(".{");
            for (var i = 0; i < declareUsing.selections.Count; i++)
            {
                if (i > 0) writer.Write(", ");

                writer.Write(declareUsing.selections[i].name);
            }

            writer.Write("}");
        }

        if (declareUsing.alias is not null)
        {
            writer.Write(" as ");
            writer.Write(declareUsing.alias.name);
        }
    }

    private static void WriteComponent(TextWriter writer, ComponentDeclaration component, ref int indent)
    {
        WriteAnnotations(writer, component.annotations, ref indent);

        WriteIndent(writer, indent);
        writer.Write("component ");
        writer.Write(GetName(component.name));

        if (component.body is not null)
        {
            writer.WriteLine();
            WriteObjectBody(writer, component.body, ref indent);
        }
        else
        {
            writer.Write(";");
        }
    }

    private static void WriteSystem(TextWriter writer, SystemDeclaration system, ref int indent)
    {
        WriteAnnotations(writer, system.annotations, ref indent);

        WriteIndent(writer, indent);
        writer.Write("system ");
        writer.Write(GetName(system.name));

        if (system.body is not null)
        {
            writer.WriteLine();
            WriteObjectBody(writer, system.body, ref indent);
        }
        else
        {
            writer.Write(";");
        }
    }

    private static void WriteWidget(TextWriter writer, WidgetDecl widget, ref int indent)
    {
        WriteAnnotations(writer, widget.annotations, ref indent);

        WriteIndent(writer, indent);
        writer.Write("widget ");
        writer.Write(GetName(widget.name));

        writer.WriteLine();
        WriteIndent(writer, indent);
        writer.WriteLine("{");
        indent++;

        foreach (var property in widget.properties)
        {
            WriteIndent(writer, indent);
            writer.Write("var ");
            writer.Write(property.name);
            writer.Write(": ");
            WriteType(writer, property.field_type);
            if (property.default_value is not null)
            {
                writer.Write(" = ");
                WriteExpression(writer, property.default_value);
            }

            writer.WriteLine(";");
        }

        if (widget.render_method is not null)
        {
            writer.WriteLine();
            WriteMicro(writer, widget.render_method, ref indent);
        }

        indent--;
        WriteIndent(writer, indent);
        writer.Write("}");
    }

    private static void WriteImply(TextWriter writer, DeclareImply imply, ref int indent)
    {
        WriteAnnotations(writer, imply.annotations, ref indent);

        WriteIndent(writer, indent);
        writer.Write("imply ");
        WriteType(writer, imply.target_type);

        if (imply.contract_type is not null)
        {
            writer.Write(": ");
            WriteType(writer, imply.contract_type);
        }

        writer.WriteLine();
        WriteIndent(writer, indent);
        writer.WriteLine("{");
        indent++;

        foreach (var method in imply.methods) WriteObjectMethod(writer, method, ref indent);

        indent--;
        WriteIndent(writer, indent);
        writer.Write("}");
    }

    private static void WritePlugin(TextWriter writer, PluginDecl plugin, ref int indent)
    {
        WriteAnnotations(writer, plugin.annotations, ref indent);

        WriteIndent(writer, indent);
        writer.Write("plugin ");
        writer.Write(GetName(plugin.name));

        if (plugin.functions.Count > 0)
        {
            writer.WriteLine();
            WriteIndent(writer, indent);
            writer.WriteLine("{");
            indent++;

            foreach (var function in plugin.functions)
            {
                WriteMicro(writer, function, ref indent);
                writer.WriteLine();
            }

            indent--;
            WriteIndent(writer, indent);
            writer.Write("}");
        }
        else
        {
            writer.Write(";");
        }
    }

    private static void WriteUnion(TextWriter writer, UnionDecl union, ref int indent)
    {
        WriteAnnotations(writer, union.annotations, ref indent);

        WriteIndent(writer, indent);
        writer.Write("union ");
        writer.Write(GetName(union.name));

        if (union.variants.Count > 0)
        {
            writer.WriteLine();
            WriteIndent(writer, indent);
            writer.WriteLine("{");
            indent++;

            for (var i = 0; i < union.variants.Count; i++)
            {
                var variant = union.variants[i];
                WriteIndent(writer, indent);
                writer.Write(variant.name);
                if (variant.fields.Count > 0)
                {
                    writer.Write(" { ");
                    for (var j = 0; j < variant.fields.Count; j++)
                    {
                        if (j > 0) writer.Write(", ");

                        writer.Write(variant.fields[j].name);
                        writer.Write(": ");
                        WriteType(writer, variant.fields[j].field_type);
                    }

                    writer.Write(" }");
                }

                if (i < union.variants.Count - 1) writer.Write(",");

                writer.WriteLine();
            }

            indent--;
            WriteIndent(writer, indent);
            writer.Write("}");
        }
        else
        {
            writer.Write(" {}");
        }
    }

    #endregion

    #region 对象体

    private static void WriteObjectBody(TextWriter writer, ObjectBody body, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.WriteLine("{");
        indent++;

        foreach (var associatedType in body.associated_types)
        {
            WriteIndent(writer, indent);
            writer.Write("type ");
            writer.Write(GetName(associatedType.name));
            if (associatedType.default_type is not null)
            {
                writer.Write(" = ");
                WriteType(writer, associatedType.default_type);
            }

            writer.WriteLine(";");
        }

        foreach (var field in body.fields)
        {
            WriteIndent(writer, indent);
            writer.Write(field.name);
            writer.Write(": ");
            WriteType(writer, field.field_type);
            if (field.default_value is not null)
            {
                writer.Write(" = ");
                WriteExpression(writer, field.default_value);
            }

            writer.WriteLine(";");
        }

        if (body.fields.Count > 0 && body.methods.Count > 0) writer.WriteLine();

        foreach (var method in body.methods) WriteObjectMethod(writer, method, ref indent);

        if (body.methods.Count > 0 && body.domains.Count > 0) writer.WriteLine();

        foreach (var domain in body.domains) WriteObjectDomain(writer, domain, ref indent);

        indent--;
        writer.WriteLine();
        WriteIndent(writer, indent);
        writer.Write("}");
    }

    private static void WriteObjectBodyInline(TextWriter writer, ObjectBody body, ref int indent)
    {
        writer.Write(" {");

        if (body.fields.Count > 0 || body.methods.Count > 0)
        {
            writer.WriteLine();
            indent++;

            foreach (var field in body.fields)
            {
                WriteIndent(writer, indent);
                writer.Write(field.name);
                writer.Write(": ");
                WriteType(writer, field.field_type);
                if (field.default_value is not null)
                {
                    writer.Write(" = ");
                    WriteExpression(writer, field.default_value);
                }

                writer.WriteLine(";");
            }

            foreach (var method in body.methods) WriteObjectMethod(writer, method, ref indent);

            indent--;
            WriteIndent(writer, indent);
        }

        writer.Write("}");
    }

    private static void WriteObjectMethod(TextWriter writer, DeclareObjectMethod method, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.Write(GetName(method.name));
        writer.Write("(");
        WriteParameterLists(writer, method.parameters);
        writer.Write(")");

        if (method.return_type is not null)
        {
            writer.Write(" -> ");
            WriteType(writer, method.return_type);
        }

        if (method.body is not null)
        {
            writer.WriteLine();
            WriteBlock(writer, method.body, ref indent);
        }
        else
        {
            writer.WriteLine(";");
        }
    }

    private static void WriteObjectDomain(TextWriter writer, DeclareObjectDomain domain, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.Write(GetName(domain.name));

        writer.WriteLine();
        WriteObjectBody(writer, domain.body, ref indent);
    }

    #endregion

    #region 语句格式化

    private static void WriteStatement(TextWriter writer, AstNode node, ref int indent)
    {
        switch (node)
        {
            case IfStatement ifStatement:
                WriteIfStatement(writer, ifStatement, ref indent);
                return;
            case WhileStatement whileStatement:
                WriteWhileStatement(writer, whileStatement, ref indent);
                return;
            case UntilStatement untilStatement:
                WriteUntilStatement(writer, untilStatement, ref indent);
                return;
            case LoopStatement loopStatement:
                WriteLoopStatement(writer, loopStatement, ref indent);
                return;
            case LoopInStatement loopInStatement:
                WriteLoopInStatement(writer, loopInStatement, ref indent);
                return;
            case ReturnStatement returnStatement:
                WriteReturn(writer, returnStatement, ref indent);
                return;
            case YieldStatement yieldStatement:
                WriteYield(writer, yieldStatement, ref indent);
                return;
            case BreakStatement:
                WriteKeywordStatement(writer, "break", ref indent);
                return;
            case ContinueStatement:
                WriteKeywordStatement(writer, "continue", ref indent);
                return;
            case ResumeStatement resumeStatement:
                WriteResume(writer, resumeStatement, ref indent);
                return;
            case MatchStatementNode matchStatement:
                WriteMatch(writer, matchStatement, ref indent);
                return;
            case CatchStatementNode catchStatement:
                WriteCatch(writer, catchStatement, ref indent);
                return;
            case FunctionBody functionBody:
                WriteBlock(writer, functionBody, ref indent);
                return;
            case DeclareLet declareLet:
                WriteLet(writer, declareLet, ref indent);
                writer.WriteLine(";");
                return;
            case TermNode termNode:
                WriteIndent(writer, indent);
                WriteExpression(writer, termNode);
                writer.WriteLine(";");
                return;
            default:
                WriteIndent(writer, indent);
                WriteFallbackInline(writer, node);
                writer.WriteLine(";");
                return;
        }
    }

    private static void WriteIfStatement(TextWriter writer, IfStatement ifStatement, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.Write("if ");
        WriteExpression(writer, ifStatement.condition);
        writer.WriteLine();
        WriteBlock(writer, ifStatement.then_block, ref indent);

        if (ifStatement.else_block is not null)
        {
            WriteIndent(writer, indent);
            writer.Write("else ");

            if (ifStatement.else_block is IfStatement elseIf)
            {
                writer.Write("if ");
                WriteExpression(writer, elseIf.condition);
                writer.WriteLine();
                WriteBlock(writer, elseIf.then_block, ref indent);

                if (elseIf.else_block is not null)
                {
                    WriteIndent(writer, indent);
                    writer.Write("else ");
                    WriteStatement(writer, elseIf.else_block, ref indent);
                }
            }
            else if (ifStatement.else_block is FunctionBody elseBody)
            {
                writer.WriteLine();
                WriteBlock(writer, elseBody, ref indent);
            }
            else
            {
                WriteStatement(writer, ifStatement.else_block, ref indent);
            }
        }
    }

    private static void WriteWhileStatement(TextWriter writer, WhileStatement whileStatement, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.Write("while ");
        WriteExpression(writer, whileStatement.condition);
        writer.WriteLine();
        WriteBlock(writer, whileStatement.body, ref indent);
    }

    private static void WriteUntilStatement(TextWriter writer, UntilStatement untilStatement, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.Write("until ");
        WriteExpression(writer, untilStatement.condition);
        writer.WriteLine();
        WriteBlock(writer, untilStatement.body, ref indent);
    }

    private static void WriteLoopStatement(TextWriter writer, LoopStatement loopStatement, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.Write("loop ");

        if (loopStatement.initializer is not null) WriteExpressionInline(writer, loopStatement.initializer);

        writer.Write("; ");

        if (loopStatement.condition is not null) WriteExpressionInline(writer, loopStatement.condition);

        writer.Write("; ");

        if (loopStatement.update is not null) WriteExpressionInline(writer, loopStatement.update);

        writer.WriteLine();
        WriteBlock(writer, loopStatement.body, ref indent);
    }

    private static void WriteLoopInStatement(TextWriter writer, LoopInStatement loopInStatement, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.Write("for ");

        if (loopInStatement.iterator_name is not null) writer.Write(loopInStatement.iterator_name);

        if (loopInStatement.iterable is not null)
        {
            writer.Write(" in ");
            WriteExpression(writer, loopInStatement.iterable);
        }

        writer.WriteLine();
        WriteBlock(writer, loopInStatement.body, ref indent);
    }

    private static void WriteReturn(TextWriter writer, ReturnStatement returnStatement, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.Write("return");

        if (returnStatement.value is not null)
        {
            writer.Write(" ");
            WriteExpression(writer, returnStatement.value);
        }

        writer.WriteLine(";");
    }

    private static void WriteKeywordStatement(TextWriter writer, string keyword, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.Write(keyword);
        writer.WriteLine(";");
    }

    private static void WriteYield(TextWriter writer, YieldStatement yieldStatement, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.Write("yield");

        switch (yieldStatement.keyword)
        {
            case YieldKeyword.YieldBreak:
                writer.WriteLine(" break;");
                return;
            case YieldKeyword.YieldReturn:
                writer.Write(" return");
                break;
        }

        if (yieldStatement.value is not null)
        {
            writer.Write(" ");
            WriteExpression(writer, yieldStatement.value);
        }

        writer.WriteLine(";");
    }

    private static void WriteResume(TextWriter writer, ResumeStatement resumeStatement, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.Write("resume");

        if (resumeStatement.value is not null)
        {
            writer.Write(" ");
            WriteExpression(writer, resumeStatement.value);
        }

        writer.WriteLine(";");
    }

    private static void WriteMatch(TextWriter writer, MatchStatementNode matchStatement, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.Write("match ");
        WriteExpression(writer, matchStatement.expression);
        writer.WriteLine();
        WriteIndent(writer, indent);
        writer.WriteLine("{");
        indent++;

        foreach (var arm in matchStatement.arms) WriteArm(writer, arm, ref indent);

        indent--;
        WriteIndent(writer, indent);
        writer.Write("}");
    }

    private static void WriteCatch(TextWriter writer, CatchStatementNode catchStatement, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.Write("catch ");
        WriteExpression(writer, catchStatement.expression);
        writer.WriteLine();
        WriteIndent(writer, indent);
        writer.WriteLine("{");
        indent++;

        foreach (var arm in catchStatement.arms) WriteArm(writer, arm, ref indent);

        indent--;
        WriteIndent(writer, indent);
        writer.Write("}");
    }

    private static void WriteArm(TextWriter writer, ArmNode arm, ref int indent)
    {
        switch (arm)
        {
            case ArmCaseNode caseArm:
                WriteIndent(writer, indent);
                writer.Write("case ");
                WritePattern(writer, caseArm.pattern);

                if (caseArm.guard is not null)
                {
                    writer.Write(" if ");
                    WriteExpression(writer, caseArm.guard);
                }

                writer.Write(":");
                writer.WriteLine();

                if (caseArm.body is not null)
                {
                    indent++;
                    WriteBlockBody(writer, caseArm.body, ref indent);
                    indent--;
                }

                return;
            case ArmElseNode:
                WriteIndent(writer, indent);
                writer.Write("else:");
                writer.WriteLine();

                if (arm.body is not null)
                {
                    indent++;
                    WriteBlockBody(writer, arm.body, ref indent);
                    indent--;
                }

                return;
            case ArmWhenNode whenArm:
                WriteIndent(writer, indent);
                writer.Write("when ");
                WriteExpression(writer, whenArm.term);

                if (whenArm.guard is not null)
                {
                    writer.Write(" if ");
                    WriteExpression(writer, whenArm.guard);
                }

                writer.Write(":");
                writer.WriteLine();

                if (arm.body is not null)
                {
                    indent++;
                    WriteBlockBody(writer, arm.body, ref indent);
                    indent--;
                }

                return;
            default:
                WriteIndent(writer, indent);
                writer.WriteLine("_:");
                return;
        }
    }

    private static void WriteBlock(TextWriter writer, FunctionBody body, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.WriteLine("{");
        indent++;
        WriteBlockBody(writer, body, ref indent);
        indent--;
        WriteIndent(writer, indent);
        writer.Write("}");
    }

    private static void WriteBlockBody(TextWriter writer, FunctionBody body, ref int indent)
    {
        foreach (var statement in body.statements) WriteStatement(writer, statement, ref indent);
    }

    #endregion

    #region 表达式格式化

    private static void WriteExpression(TextWriter writer, AstNode node)
    {
        switch (node)
        {
            case TermBinaryExpression binary:
                WriteBinary(writer, binary);
                return;
            case TermUnaryExpression unary:
                WriteUnary(writer, unary);
                return;
            case TermCallExpression call:
                WriteCall(writer, call);
                return;
            case TermDotExpression dot:
                WriteDot(writer, dot);
                return;
            case TermIndexExpression index:
                WriteIndex(writer, index);
                return;
            case TermOrdinalExpression ordinal:
                WriteOrdinal(writer, ordinal);
                return;
            case TermLiteralNumberNode num:
                writer.Write(num.value);
                return;
            case TermLiteralTextNode text:
                if (!string.IsNullOrWhiteSpace(text.prefix))
                {
                    writer.Write(text.prefix);
                }

                writer.Write('"');
                writer.Write(text.value);
                writer.Write('"');
                return;
            case TermLiteralBooleanNode boolean:
                writer.Write(boolean.value ? "true" : "false");
                return;
            case LiteralNullNode:
                writer.Write("null");
                return;
            case TermLiteralObjectNode objectNode:
                WriteObjectLiteral(writer, objectNode);
                return;
            case TermLiteralNamePathNode namePath:
                WriteQualifiedPath(writer, namePath.path);
                return;
            case TypeLiteralNamePathNode typeNamePath:
                WriteType(writer, typeNamePath);
                return;
            case TermLiteralArrayNode array:
                WriteArrayLiteral(writer, array);
                return;
            case TermLiteralTupleNode tuple:
                WriteTupleLiteral(writer, tuple);
                return;
            case TermAsExpression asExpression:
                WriteExpression(writer, asExpression.operand);
                writer.Write(" as ");
                WriteType(writer, asExpression.target_type);
                return;
            case TermIsExpression isExpression:
                WriteExpression(writer, isExpression.operand);
                writer.Write(" is ");
                WritePattern(writer, isExpression.target_pattern_node);
                return;
            case IdentifierNode identifier:
                writer.Write(identifier.name);
                return;
            case QualifiedPathNode qualifiedPath:
                WriteQualifiedPath(writer, qualifiedPath);
                return;
            case DeclareLet declareLet:
                var noIndent = 0;
                WriteLet(writer, declareLet, ref noIndent);
                return;
            case TermObjectField objectField:
                WriteObjectFieldExpr(writer, objectField);
                return;
            default:
                WriteFallbackInline(writer, node);
                return;
        }
    }

    private static void WriteExpressionInline(TextWriter writer, AstNode node)
    {
        WriteExpression(writer, node);
    }

    private static void WriteBinary(TextWriter writer, BinaryExpr binary)
    {
        WriteExpression(writer, binary.left);
        writer.Write(" ");
        writer.Write(GetBinaryOpString(binary.@operator));
        writer.Write(" ");
        WriteExpression(writer, binary.right);
    }

    private static void WriteUnary(TextWriter writer, TermUnaryExpression unary)
    {
        if (unary.is_prefix)
        {
            writer.Write(GetUnaryOpString(unary.@operator));
            WriteExpression(writer, unary.operand);
        }
        else
        {
            WriteExpression(writer, unary.operand);
            writer.Write(GetUnaryOpString(unary.@operator));
        }
    }

    private static void WriteCall(TextWriter writer, TermCallExpression call)
    {
        WriteExpression(writer, call.caller);

        if (call.call_body is not null) WriteCallBody(writer, call.call_body);
    }

    private static void WriteDot(TextWriter writer, TermDotExpression dot)
    {
        WriteExpression(writer, dot.caller);
        writer.Write(".");
        WriteQualifiedPath(writer, dot.callee);

        if (dot.call_body is not null) WriteCallBody(writer, dot.call_body);
    }

    private static void WriteCallBody(TextWriter writer, CallBody callBody)
    {
        if (callBody.type_arguments is not null && callBody.type_arguments.items.Count > 0)
        {
            writer.Write("::<");
            for (var i = 0; i < callBody.type_arguments.items.Count; i++)
            {
                if (i > 0) writer.Write(", ");

                var arg = callBody.type_arguments.items[i];
                if (arg.slot is not null)
                {
                    writer.Write(GetName(arg.slot));
                    writer.Write(" = ");
                }

                WriteType(writer, arg.argument);
            }

            writer.Write(">");
        }

        if (callBody.term_arguments is not null && callBody.term_arguments.items.Count > 0)
        {
            writer.Write("(");
            for (var i = 0; i < callBody.term_arguments.items.Count; i++)
            {
                if (i > 0) writer.Write(", ");

                var arg = callBody.term_arguments.items[i];
                if (arg.key is not null)
                {
                    writer.Write(GetName(arg.key.name));
                    writer.Write(": ");
                }

                WriteExpression(writer, arg.value);
            }

            writer.Write(")");
        }

        if (callBody.function_body is not null)
        {
            writer.Write(" ");
            var tmp = 0;
            WriteBlock(writer, callBody.function_body, ref tmp);
        }
    }

    private static void WriteIndex(TextWriter writer, TermIndexExpression index)
    {
        WriteExpression(writer, index.target);
        writer.Write("[");
        WriteExpression(writer, index.index);
        writer.Write("]");
    }

    private static void WriteOrdinal(TextWriter writer, TermOrdinalExpression ordinal)
    {
        WriteExpression(writer, ordinal.target);
        writer.Write("[");
        for (var i = 0; i < ordinal.indices.Count; i++)
        {
            if (i > 0) writer.Write(", ");

            WriteExpression(writer, ordinal.indices[i]);
        }

        writer.Write("]");
    }

    private static void WriteObjectLiteral(TextWriter writer, TermLiteralObjectNode objectNode)
    {
        WriteExpression(writer, objectNode.constructor);
        writer.Write(" {");

        if (objectNode.fields.Count > 0)
        {
            writer.Write(" ");
            for (var i = 0; i < objectNode.fields.Count; i++)
            {
                if (i > 0) writer.Write(", ");

                var field = objectNode.fields[i];
                writer.Write(field.name);
                if (field.value is not null)
                {
                    writer.Write(": ");
                    WriteExpression(writer, field.value);
                }
            }

            writer.Write(" ");
        }

        if (objectNode.has_spread) writer.Write("..");

        writer.Write("}");
    }

    private static void WriteObjectFieldExpr(TextWriter writer, TermObjectField field)
    {
        writer.Write(field.name);
        if (field.value is not null)
        {
            writer.Write(": ");
            WriteExpression(writer, field.value);
        }
    }

    private static void WriteArrayLiteral(TextWriter writer, TermLiteralArrayNode array)
    {
        writer.Write("[");
        for (var i = 0; i < array.elements.Count; i++)
        {
            if (i > 0) writer.Write(", ");

            WriteExpression(writer, array.elements[i]);
        }

        writer.Write("]");
    }

    private static void WriteTupleLiteral(TextWriter writer, TermLiteralTupleNode tuple)
    {
        writer.Write("(");
        for (var i = 0; i < tuple.elements.Count; i++)
        {
            if (i > 0) writer.Write(", ");

            WriteExpression(writer, tuple.elements[i]);
        }

        writer.Write(")");
    }

    #endregion

    #region 辅助方法

    private static void WriteAnnotations(TextWriter writer, Annotations annotations, ref int indent)
    {
        foreach (var doc in annotations.documents)
        {
            WriteIndent(writer, indent);
            writer.Write("/// ");
            writer.WriteLine(doc.content);
        }

        foreach (var attrList in annotations.attribute_lists)
        {
            WriteIndent(writer, indent);
            writer.Write("[");
            for (var i = 0; i < attrList.items.Count; i++)
            {
                if (i > 0) writer.Write(", ");

                WriteAttribute(writer, attrList.items[i]);
            }

            writer.WriteLine("]");
        }

        foreach (var modifier in annotations.modifiers)
        {
            WriteIndent(writer, indent);
            writer.Write(modifier.name);
            writer.Write(" ");
        }
    }

    private static void WriteAttribute(TextWriter writer, AttributeItem attribute)
    {
        writer.Write(attribute.name);

        if (attribute.arguments is not null && attribute.arguments.items.Count > 0)
        {
            writer.Write("(");
            for (var i = 0; i < attribute.arguments.items.Count; i++)
            {
                if (i > 0) writer.Write(", ");

                var arg = attribute.arguments.items[i];
                if (arg.key is not null)
                {
                    writer.Write(GetName(arg.key.name));
                    writer.Write(" = ");
                }

                WriteExpression(writer, arg.value);
            }

            writer.Write(")");
        }
    }

    private static void WriteTypeParameters(TextWriter writer, IReadOnlyList<TypeParameterList> typeParameters)
    {
        if (typeParameters.Count == 0) return;

        writer.Write("<");
        for (var i = 0; i < typeParameters.Count; i++)
        {
            if (i > 0) writer.Write(", ");

            var paramList = typeParameters[i];
            for (var j = 0; j < paramList.items.Count; j++)
            {
                if (j > 0) writer.Write(", ");

                var param = paramList.items[j];
                if (param.name is not null) writer.Write(GetName(param.name));

                if (param.bound_type is not null)
                {
                    writer.Write(": ");
                    WriteType(writer, param.bound_type);
                }

                if (param.default_type is not null)
                {
                    writer.Write(" = ");
                    WriteType(writer, param.default_type);
                }
            }
        }

        writer.Write(">");
    }

    private static void WriteTypeArguments(TextWriter writer, TypeArgumentList? typeArguments)
    {
        if (typeArguments is null || typeArguments.items.Count == 0)
        {
            return;
        }

        writer.Write("<");
        for (var i = 0; i < typeArguments.items.Count; i++)
        {
            if (i > 0) writer.Write(", ");

            var argument = typeArguments.items[i];
            if (argument.slot is not null)
            {
                writer.Write(GetName(argument.slot));
                writer.Write(" = ");
            }

            WriteType(writer, argument.argument);
        }

        writer.Write(">");
    }

    private static void WriteGenericConstraints(TextWriter writer, IReadOnlyList<GenericConstraint> constraints)
    {
        if (constraints.Count == 0) return;

        writer.Write(" where ");
        for (var i = 0; i < constraints.Count; i++)
        {
            if (i > 0) writer.Write(", ");

            writer.Write(constraints[i].parameter_name);
            writer.Write(": ");
            for (var j = 0; j < constraints[i].constraint_types.Count; j++)
            {
                if (j > 0) writer.Write(" + ");

                WriteType(writer, constraints[i].constraint_types[j]);
            }
        }
    }

    private static void WriteParameterLists(TextWriter writer, IReadOnlyList<TermParameterList> parameterLists)
    {
        var first = true;
        foreach (var paramList in parameterLists)
            for (var i = 0; i < paramList.items.Count; i++)
            {
                if (!first) writer.Write(", ");

                first = false;

                var param = paramList.items[i];
                if (param.name is not null) writer.Write(GetName(param.name));

                if (param.bound_type is not null)
                {
                    writer.Write(": ");
                    WriteType(writer, param.bound_type);
                }

                if (param.default_term is not null)
                {
                    writer.Write(" = ");
                    WriteExpression(writer, param.default_term);
                }
            }
    }

    private static void WriteQualifiedPath(TextWriter writer, QualifiedPathNode path)
    {
        if (path.is_global) writer.Write("::");

        for (var i = 0; i < path.segments.Count; i++)
        {
            if (i > 0) writer.Write("::");

            writer.Write(path.segments[i].name);
        }
    }

    private static void WritePattern(TextWriter writer, PatternNode pattern)
    {
        if (pattern is null)
        {
            writer.Write("_");
            return;
        }

        var typeName = pattern.GetType().Name;
        switch (typeName)
        {
            case "PatternLiteralWildcardNode":
                writer.Write("_");
                return;
            case "PatternLiteralVariableNode":
                var varName = pattern.GetType().GetProperty("Name")?.GetValue(pattern) as string;
                writer.Write(varName ?? "_");
                return;
            case "PatternLiteralNumberNode":
                var numVal = pattern.GetType().GetProperty("Value")?.GetValue(pattern);
                writer.Write(numVal?.ToString() ?? "0");
                return;
            case "PatternLiteralTextNode":
                var textVal = pattern.GetType().GetProperty("Value")?.GetValue(pattern) as string;
                writer.Write('"');
                writer.Write(textVal ?? "");
                writer.Write('"');
                return;
            case "PatternLiteralBooleanNode":
                var boolVal = pattern.GetType().GetProperty("Value")?.GetValue(pattern);
                writer.Write(boolVal is true ? "true" : "false");
                return;
            case "PatternLiteralNullNode":
                writer.Write("null");
                return;
            case "PatternLiteralTupleNode":
                writer.Write("(");
                var elements = pattern.GetType().GetProperty("elements")?.GetValue(pattern) as IList;
                if (elements is not null)
                    for (var i = 0; i < elements.Count; i++)
                    {
                        if (i > 0) writer.Write(", ");

                        if (elements[i] is PatternNode elementPattern)
                            WritePattern(writer, elementPattern);
                        else
                            writer.Write("_");
                    }

                writer.Write(")");
                return;
            case "PatternLiteralObjectNode":
                var pathProp = pattern.GetType().GetProperty("Path")?.GetValue(pattern) as QualifiedPathNode;
                if (pathProp is not null) WriteQualifiedPath(writer, pathProp);

                writer.Write(" { ... }");
                return;
            case "PatternExpressionUnaryNode":
                var unaryOp = pattern.GetType().GetProperty("Operator")?.GetValue(pattern);
                var unaryOperand = pattern.GetType().GetProperty("Operand")?.GetValue(pattern) as PatternNode;
                writer.Write(unaryOp?.ToString() ?? "!");
                if (unaryOperand is not null) WritePattern(writer, unaryOperand);

                return;
            case "PatternExpressionBinaryNode":
                var binOp = pattern.GetType().GetProperty("Operator")?.GetValue(pattern);
                var lhs = pattern.GetType().GetProperty("Lhs")?.GetValue(pattern) as PatternNode;
                var rhs = pattern.GetType().GetProperty("Rhs")?.GetValue(pattern) as PatternNode;
                if (lhs is not null) WritePattern(writer, lhs);

                writer.Write(" ");
                writer.Write(binOp?.ToString() ?? "|");
                writer.Write(" ");
                if (rhs is not null) WritePattern(writer, rhs);

                return;
            default:
                writer.Write(pattern.GetType().Name);
                return;
        }
    }

    private static void WriteIndent(TextWriter writer, int indent)
    {
        for (var i = 0; i < indent; i++) writer.Write(IndentString);
    }

    private static string GetName(IdentifierNode? node)
    {
        return node?.name ?? string.Empty;
    }

    private static string GetBinaryOpString(TermBinaryOperator op)
    {
        return op switch
        {
            TermBinaryOperator.equal => "==",
            TermBinaryOperator.not_equal => "!=",
            TermBinaryOperator.less_than => "<",
            TermBinaryOperator.greater_than => ">",
            TermBinaryOperator.less_than_or_equal => "<=",
            TermBinaryOperator.greater_than_or_equal => ">=",
            TermBinaryOperator.logical_and => "&&",
            TermBinaryOperator.logical_or => "||",
            TermBinaryOperator.power => "^",
            TermBinaryOperator.addition => "+",
            TermBinaryOperator.subtraction => "-",
            TermBinaryOperator.multiplication => "*",
            TermBinaryOperator.division => "/",
            TermBinaryOperator.modulus => "%",
            TermBinaryOperator.bitwise_and => "&",
            TermBinaryOperator.bitwise_or => "|",
            TermBinaryOperator.bitwise_xor => "^",
            TermBinaryOperator.left_shift => "<<",
            TermBinaryOperator.right_shift => ">>",
            TermBinaryOperator.assign => "=",
            TermBinaryOperator.plus_assign => "+=",
            TermBinaryOperator.minus_assign => "-=",
            TermBinaryOperator.multiply_assign => "*=",
            TermBinaryOperator.divide_assign => "/=",
            TermBinaryOperator.modulus_assign => "%=",
            TermBinaryOperator.and_assign => "&=",
            TermBinaryOperator.or_assign => "|=",
            TermBinaryOperator.xor_assign => "^=",
            TermBinaryOperator.left_shift_assign => "<<=",
            TermBinaryOperator.right_shift_assign => ">>=",
            _ => op.ToString()
        };
    }

    private static string GetUnaryOpString(TermUnaryOperator op)
    {
        return op switch
        {
            TermUnaryOperator.logical_not => "!",
            TermUnaryOperator.bitwise_not => "~",
            TermUnaryOperator.negate => "-",
            TermUnaryOperator.increment => "++",
            TermUnaryOperator.decrement => "--",
            _ => op.ToString()
        };
    }

    private static string GetTypeBinaryOpString(TypeBinaryOperator op)
    {
        return op switch
        {
            TypeBinaryOperator.or => "|",
            TypeBinaryOperator.and => "&",
            TypeBinaryOperator.difference => "-",
            TypeBinaryOperator.product => "+",
            TypeBinaryOperator.nullable => "?",
            _ => op.ToString()
        };
    }

    private static string GetTypeUnaryOpString(TypeUnaryOperator op)
    {
        return op switch
        {
            TypeUnaryOperator.not => "!",
            TypeUnaryOperator.contravariance => "-",
            TypeUnaryOperator.covariance => "+",
            TypeUnaryOperator.nullable => "?",
            _ => op.ToString()
        };
    }

    private static void WriteFallback(TextWriter writer, ValkyrieNode node, ref int indent)
    {
        WriteIndent(writer, indent);
        writer.Write("// [unformatted: ");
        writer.Write(node.GetType().Name);
        var props = node.GetType().GetProperties();
        foreach (var prop in props)
        {
            if (prop.Name is "Span" or "Type") continue;

            try
            {
                var value = prop.GetValue(node);
                writer.Write($" {prop.Name}={value}");
            }
            catch
            {
                // 忽略无法读取的属性
            }
        }

        writer.WriteLine("]");
    }

    private static void WriteFallbackInline(TextWriter writer, ValkyrieNode node)
    {
        writer.Write("/* ");
        writer.Write(node.GetType().Name);
        writer.Write(" */");
    }

    #endregion
}
