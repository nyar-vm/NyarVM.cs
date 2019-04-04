using System.Text;
using Std.Data.Text.Parsing;
using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.ECS;
using Std.Data.Text.Valkyrie.AST.Neural;
using Std.Data.Text.Valkyrie.AST.Pattern;
using Std.Data.Text.Valkyrie.AST.Schema;
using Std.Data.Text.Valkyrie.AST.Shader;
using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;
using Std.Data.Text.Valkyrie.AST.Widget;
using Std.Data.Text.Valkyrie.Lexer;
using DiagnosticTextSpan = Std.Text.TextSpan;

namespace Std.Data.Text.Valkyrie.Parser;

/// <summary>
///     声明解析扩展入口。
internal static class DeclarationExtensions
{
    private static readonly HashSet<string> _s_body_declaration_keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "meta"
    };

    extension(TokenStream tokens)
    {
        internal IReadOnlyList<ValkyrieNode> parse_top_level_nodes(ValkyrieLanguage language)
        {
            var declarations = new List<ValkyrieNode>();

            while (!tokens.is_at_end())
                try
                {
                    var leadingDocs = tokens.parse_doc_comments();
                    var leadingAttrs = tokens.collect_leading_attributes();
                    var leadingMods = tokens.collect_leading_modifiers();

                    if (tokens.is_declaration_start(language))
                        declarations.Add(
                            tokens.parse_declaration_with_modifiers(language, leadingAttrs, leadingMods, leadingDocs));
                    else
                        declarations.Add(tokens.parse_statement_node(language));
                }
                catch (InvalidOperationException)
                {
                    tokens.synchronize();
                }

            return declarations;
        }

        internal bool is_declaration_start_node(ValkyrieLanguage language)
        {
            return tokens.is_declaration_start(language);
        }

        internal ValkyrieNode parse_declaration_node(ValkyrieLanguage language)
        {
            return tokens.parse_declaration(language);
        }

        internal DeclareLet parse_variable_decl_node(ValkyrieLanguage language)
        {
            return tokens.parse_variable_decl(language, []);
        }

        internal FunctionBody parse_block_node(ValkyrieLanguage language)
        {
            return tokens.parse_block(language);
        }

        internal IReadOnlyList<AttributeItem> collect_leading_attributes()
        {
            return tokens.parse_attributes();
        }

        internal IReadOnlyList<string> collect_leading_modifiers()
        {
            var count = 0;

            while (!tokens.is_at_end())
            {
                var kind = tokens.peek_valkyrie_kind(count);
                if (kind != ValkyrieTokenKind.identifier) break;

                count++;
            }

            if (count == 0 || !tokens.peek(count).kind.is_keyword()) return [];

            var mods = new List<string>(count);
            for (var i = 0; i < count; i++) mods.Add(tokens.advance_text());

            return mods;
        }

        internal ValkyrieNode parse_declaration_with_modifiers(ValkyrieLanguage language,
            IReadOnlyList<AttributeItem> leadingAttrs,
            IReadOnlyList<string> leadingMods,
            IReadOnlyList<DocumentComment>? leadingDocs = null)
        {
            var decl = tokens.parse_declaration(language);
            return tokens.attach_leading_metadata(decl, leadingAttrs, leadingMods, leadingDocs);
        }

        private ValkyrieNode attach_leading_metadata(ValkyrieNode node,
            IReadOnlyList<AttributeItem> attrs,
            IReadOnlyList<string> mods,
            IReadOnlyList<DocumentComment>? docs = null)
        {
            if (attrs.Count == 0 && mods.Count == 0 && (docs is null || docs.Count == 0)) return node;

            switch (node)
            {
                case DeclareComponent comp:
                    comp = comp with { annotations = merge_annotations(comp.annotations, attrs, mods, docs) };
                    return comp;
                case DeclareSystem sys:
                    sys = sys with { annotations = merge_annotations(sys.annotations, attrs, mods, docs) };
                    return sys;
                case DeclareWidget w:
                    w = w with { annotations = merge_annotations(w.annotations, attrs, mods, docs) };
                    return w;
                case DeclareMicro fn:
                    fn = fn with { annotations = merge_annotations(fn.annotations, attrs, mods, docs) };
                    return fn;
                case DeclareEnums e:
                    e = e with { annotations = merge_annotations(e.annotations, attrs, mods, docs) };
                    return e;
                case ShaderDecl s:
                    s = s with { annotations = merge_annotations(s.annotations, attrs, mods, docs) };
                    return s;
                case DeclareStructure st:
                    st = st with { annotations = merge_annotations(st.annotations, attrs, mods, docs) };
                    return st;
                case DeclareImply imply:
                    imply = imply with { annotations = merge_annotations(imply.annotations, attrs, mods, docs) };
                    return imply;
                case UniformDecl u:
                    u = u with { annotations = merge_annotations(u.annotations, attrs, mods, docs) };
                    return u;
                case VaryingDecl v:
                    v = v with { annotations = merge_annotations(v.annotations, attrs, mods, docs) };
                    return v;
                case ConstantBufferDecl c:
                    c = c with { annotations = merge_annotations(c.annotations, attrs, mods, docs) };
                    return c;
                case TextureDecl t:
                    t = t with { annotations = merge_annotations(t.annotations, attrs, mods, docs) };
                    return t;
                case SamplerDecl sm:
                    sm = sm with { annotations = merge_annotations(sm.annotations, attrs, mods, docs) };
                    return sm;
            }

            return node;
        }
    }

    private static Annotations build_annotations(
        IReadOnlyList<AttributeItem>? attrs = null,
        IReadOnlyList<string>? mods = null,
        IReadOnlyList<DocumentComment>? docs = null)
    {
        var attributeItems = attrs ?? [];
        IReadOnlyList<AttributeList> attributeLists = attributeItems.Count == 0
            ? []
            : [new AttributeList { items = attributeItems }];

        IReadOnlyList<IdentifierNode> modifierNodes = mods is null || mods.Count == 0
            ? []
            : mods.Select(m => ParserNodeFactory.create_identifier(m)).ToList();

        return new Annotations
        {
            documents = docs ?? [],
            attribute_lists = attributeLists,
            modifiers = modifierNodes
        };
    }

    private static Annotations merge_annotations(
        Annotations existing,
        IReadOnlyList<AttributeItem> attrs,
        IReadOnlyList<string>? mods = null,
        IReadOnlyList<DocumentComment>? docs = null)
    {
        var mergedAttributeLists = attrs.Count == 0
            ? existing.attribute_lists
            : [new AttributeList { items = attrs }, .. existing.attribute_lists];

        IReadOnlyList<IdentifierNode> leadingModifiers = mods is null || mods.Count == 0
            ? []
            : mods.Select(m => ParserNodeFactory.create_identifier(m)).ToList();
        var mergedModifiers = leadingModifiers.Count == 0
            ? existing.modifiers
            : [.. leadingModifiers, .. existing.modifiers];

        var mergedDocuments = docs is null || docs.Count == 0
            ? existing.documents
            : [.. docs, .. existing.documents];

        return existing with
        {
            attribute_lists = mergedAttributeLists,
            modifiers = mergedModifiers,
            documents = mergedDocuments
        };
    }

    extension(TokenStream tokens)
    {
        internal bool is_declaration_start(ValkyrieLanguage language)
        {
            if (tokens.is_at_end()) return false;

            var kind = tokens.peek_valkyrie_kind();
            if (kind is ValkyrieTokenKind.bracket_l or
                ValkyrieTokenKind.component or
                ValkyrieTokenKind.system or
                ValkyrieTokenKind.widget or
                ValkyrieTokenKind.micro or
                ValkyrieTokenKind.let or
                ValkyrieTokenKind.structure or
                ValkyrieTokenKind.@class or
                ValkyrieTokenKind.enums or
                ValkyrieTokenKind.flags or
                ValkyrieTokenKind.union or
                ValkyrieTokenKind.unite or
                ValkyrieTokenKind.imply or
                ValkyrieTokenKind.type or
                ValkyrieTokenKind.@namespace or
                ValkyrieTokenKind.@using or
                ValkyrieTokenKind.shader or
                ValkyrieTokenKind.service or
                ValkyrieTokenKind.model or
                ValkyrieTokenKind.trait or
                ValkyrieTokenKind.neural or
                ValkyrieTokenKind.@unsafe)
                return true;

            if (kind == ValkyrieTokenKind.identifier)
            {
                if (string.Equals(tokens.peek_text(), "plugin", StringComparison.OrdinalIgnoreCase)) return true;

                var offset = 0;
                while (tokens.peek_valkyrie_kind(offset) == ValkyrieTokenKind.identifier) offset++;

                var afterModifiers = tokens.peek(offset);
                return (afterModifiers.kind.is_keyword() && !afterModifiers.kind.is_statement_keyword()) ||
                       (afterModifiers.kind == ValkyrieTokenKind.parenthesis_l.to_node_kind() && offset >= 2);
            }

            return false;
        }

        internal ValkyrieNode parse_declaration(ValkyrieLanguage language)
        {
            if (tokens.is_keyword()) return tokens.dispatch_keyword(language, []);

            var modifiers = new List<string> { tokens.advance_text() };
            modifiers.AddRange(tokens.parse_modifiers());

            if (tokens.is_keyword()) return tokens.dispatch_keyword(language, modifiers);

            if (tokens.check(ValkyrieTokenKind.parenthesis_l, 1))
                return tokens.parse_function_decl(language, modifiers);

            return tokens.skip_unknown_decl();
        }

        private ValkyrieNode dispatch_keyword(ValkyrieLanguage language,
            IReadOnlyList<string> modifiers)
        {
            var vKind = (ValkyrieTokenKind)tokens.current.kind.value;
            tokens.advance();

            switch (vKind)
            {
                case ValkyrieTokenKind.micro:
                    return tokens.parse_function_decl(language, modifiers);
                case ValkyrieTokenKind.component:
                    return tokens.parse_component_decl(language, modifiers);
                case ValkyrieTokenKind.system:
                    return tokens.parse_system_decl(language, modifiers);
                case ValkyrieTokenKind.widget:
                    return tokens.parse_widget_decl(language, modifiers);
                case ValkyrieTokenKind.model:
                    return tokens.parse_model_declaration();
                case ValkyrieTokenKind.service:
                    return tokens.parse_service_declaration();
                case ValkyrieTokenKind.enums:
                    return tokens.parse_enum_decl();
                case ValkyrieTokenKind.flags:
                    return tokens.parse_flags_decl();
                case ValkyrieTokenKind.union:
                    return tokens.parse_union_decl();
                case ValkyrieTokenKind.unite:
                    return tokens.parse_unite_decl();
                case ValkyrieTokenKind.imply:
                    return tokens.parse_imply_decl(language);
                case ValkyrieTokenKind.@namespace:
                    return tokens.parse_namespace_decl();
                case ValkyrieTokenKind.@class:
                    return tokens.parse_class(language);
                case ValkyrieTokenKind.structure:
                    return tokens.parse_struct_decl();
                case ValkyrieTokenKind.trait:
                    return tokens.parse_trait(language);
                case ValkyrieTokenKind.neural:
                    return tokens.parse_neural_decl();
                case ValkyrieTokenKind.@using:
                    return tokens.parse_using_decl();
                case ValkyrieTokenKind.type:
                    return tokens.parse_type_alias_decl();
                case ValkyrieTokenKind.let:
                    return tokens.parse_variable_decl(language, modifiers);
                case ValkyrieTokenKind.shader:
                    return tokens.parse_shader_decl();
                case ValkyrieTokenKind.uniform:
                    return tokens.parse_uniform_decl();
                case ValkyrieTokenKind.varying:
                    return tokens.parse_varying_decl();
                case ValkyrieTokenKind.c_buffer:
                    return tokens.parse_constant_buffer_decl();
                case ValkyrieTokenKind.texture:
                    return tokens.parse_texture_decl();
                case ValkyrieTokenKind.sampler:
                    return tokens.parse_sampler_decl();
                case ValkyrieTokenKind.@unsafe:
                {
                    var combinedMods = new List<string>(modifiers) { "unsafe" };
                    if (tokens.is_keyword())
                    {
                        return tokens.dispatch_keyword(language, combinedMods);
                    }

                    if (tokens.check(ValkyrieTokenKind.parenthesis_l, 1))
                    {
                        return tokens.parse_function_decl(language, combinedMods);
                    }

                    return tokens.skip_unknown_decl();
                }
                default:
                    return tokens.skip_unknown_decl();
            }
        }

        internal DeclareMicro parse_function_decl(ValkyrieLanguage language,
            IReadOnlyList<string> modifiers)
        {
            var name = tokens.advance_text();

            var typeParameters = tokens.parse_type_parameters();
            var attributes = tokens.parse_attributes();
            var docComments = tokens.parse_doc_comments();

            tokens.expect(ValkyrieTokenKind.parenthesis_l);
            var parameters = tokens.parse_param_list();
            tokens.expect(ValkyrieTokenKind.parenthesis_r);

            TypeNode? returnType = null;
            if (tokens.check(ValkyrieTokenKind.arrow))
            {
                tokens.advance();
                returnType = tokens.parse_type();
            }
            else if (tokens.check(ValkyrieTokenKind.colon))
            {
                tokens.advance();
                returnType = tokens.parse_type();
            }

            var genericConstraints = tokens.parse_generic_constraints();

            FunctionBody? body = null;
            if (tokens.check(ValkyrieTokenKind.brace_l))
                body = tokens.parse_block(language);
            else
                tokens.match(ValkyrieTokenKind.semicolon);

            return new DeclareMicro
            {
                name = ParserNodeFactory.create_identifier(name),
                type_parameters = typeParameters,
                parameters = parameters,
                return_type = returnType,
                body = body,
                annotations = build_annotations(attributes, modifiers, docComments),
                generic_constraints = genericConstraints
            };
        }

        internal FunctionBody parse_block(ValkyrieLanguage language)
        {
            tokens.expect(ValkyrieTokenKind.brace_l);
            var stmts = new List<ValkyrieNode>();

            while (!tokens.is_at_end() && !tokens.check(ValkyrieTokenKind.brace_r))
                stmts.Add(tokens.parse_statement_node(language));

            tokens.expect(ValkyrieTokenKind.brace_r);
            return new FunctionBody(stmts);
        }

        private DeclareComponent parse_component_decl(ValkyrieLanguage language,
            IReadOnlyList<string> modifiers)
        {
            var name = tokens.advance_text();

            if (tokens.check(ValkyrieTokenKind.less))
            {
                tokens.advance();
                while (!tokens.is_at_end()
                       && !tokens.check(ValkyrieTokenKind.greater))
                    tokens.advance();

                tokens.advance();
            }

            var attrs = tokens.parse_attributes();
            var docs = tokens.parse_doc_comments();
            var body = tokens.parse_object_body(language);

            return new DeclareComponent
            {
                name = ParserNodeFactory.create_identifier(name),
                annotations = build_annotations(attrs, modifiers, docs),
                body = body
            };
        }

        private DeclareSystem parse_system_decl(ValkyrieLanguage language,
            IReadOnlyList<string> modifiers)
        {
            var name = tokens.advance_text();
            var attrs = tokens.parse_attributes();
            var docs = tokens.parse_doc_comments();
            var queries = new List<QueryExpr>();
            var body = tokens.parse_object_body(language, queries, false, allowDomains: true);

            return new DeclareSystem
            {
                name = ParserNodeFactory.create_identifier(name),
                annotations = build_annotations(attrs, modifiers, docs),
                queries = queries,
                body = body
            };
        }

        private DeclareWidget parse_widget_decl(ValkyrieLanguage language,
            IReadOnlyList<string> modifiers)
        {
            var name = tokens.advance_text();
            var attrs = tokens.parse_attributes();
            var docs = tokens.parse_doc_comments();

            tokens.expect(ValkyrieTokenKind.brace_l);
            var props = new List<DeclareObjectField>();
            DeclareMicro? renderMethod = null;

            while (!tokens.check(ValkyrieTokenKind.brace_r) && !tokens.is_at_end())
                if (tokens.is_function_decl_follows())
                {
                    var blockMods = tokens.collect_block_function_modifiers();
                    renderMethod = tokens.parse_function_decl(language, blockMods);
                }
                else if (tokens.peek_valkyrie_kind() == ValkyrieTokenKind.identifier)
                {
                    props.Add(tokens.parse_field());
                }
                else
                {
                    tokens.skip_unrecognized_token();
                }

            tokens.expect(ValkyrieTokenKind.brace_r);

            return new DeclareWidget
            {
                name = ParserNodeFactory.create_identifier(name),
                properties = props,
                render_method = renderMethod,
                annotations = build_annotations(attrs, modifiers, docs)
            };
        }


        private bool is_identifier_followed_by_equals()
        {
            return tokens.peek_valkyrie_kind(1) == ValkyrieTokenKind.equal;
        }

        private DeclareEnums parse_enum_decl()
        {
            var name = tokens.advance_text();
            var attrs = tokens.parse_attributes();

            tokens.expect(ValkyrieTokenKind.brace_l);
            var members = new List<DeclareSemanticMember>();

            while (!tokens.check(ValkyrieTokenKind.brace_r) && !tokens.is_at_end())
            {
                var memberName = tokens.advance_text();
                TermNode? value = null;
                if (tokens.check(ValkyrieTokenKind.equal))
                {
                    tokens.advance();
                    value = tokens.parse_term_value_node();
                }

                members.Add(new DeclareSemanticMember
                    { name = ParserNodeFactory.create_identifier(memberName), value = value });
                tokens.match(ValkyrieTokenKind.comma);
            }

            tokens.expect(ValkyrieTokenKind.brace_r);
            return new DeclareEnums
            {
                name = ParserNodeFactory.create_identifier(name),
                members = members,
                annotations = build_annotations(attrs)
            };
        }

        private DeclareFlags parse_flags_decl()
        {
            var enumDecl = tokens.parse_enum_decl();
            return new DeclareFlags
            {
                name = ParserNodeFactory.create_identifier(enumDecl.name.name),
                members = enumDecl.members,
                annotations = enumDecl.annotations
            };
        }

        private DeclareUnite parse_union_decl()
        {
            var name = tokens.advance_text();
            var typeParameters = tokens.parse_type_parameters();
            var attrs = tokens.parse_attributes();

            tokens.expect(ValkyrieTokenKind.brace_l);
            var variants = new List<DeclareUniteVariant>();

            while (!tokens.check(ValkyrieTokenKind.brace_r) && !tokens.is_at_end())
            {
                // 解析变体级别的属性（如 [tag(0)]）
                var variantAttrs = tokens.parse_attributes();
                var variantDocs = tokens.parse_doc_comments();

                var varName = tokens.advance_text();
                var fields = new List<DeclareObjectField>();

                // 圆括号表示单载荷变体，括号内为类型列表（如 Some(T)）
                if (tokens.check(ValkyrieTokenKind.parenthesis_l))
                {
                    tokens.advance();
                    var payloadIndex = 0;
                    while (!tokens.check(ValkyrieTokenKind.parenthesis_r) && !tokens.is_at_end())
                    {
                        var payloadType = tokens.parse_type();
                        fields.Add(new DeclareObjectField
                        {
                            name = $"_{payloadIndex}",
                            field_type = payloadType
                        });
                        payloadIndex++;
                        tokens.match(ValkyrieTokenKind.comma);
                    }

                    tokens.expect(ValkyrieTokenKind.parenthesis_r);
                }
                // 花括号表示命名字段变体，括号内为 name : type 列表
                else if (tokens.check(ValkyrieTokenKind.brace_l))
                {
                    tokens.advance();
                    while (!tokens.check(ValkyrieTokenKind.brace_r) && !tokens.is_at_end())
                    {
                        fields.Add(tokens.parse_field());
                        tokens.match(ValkyrieTokenKind.comma);
                    }

                    tokens.expect(ValkyrieTokenKind.brace_r);
                }

                variants.Add(new DeclareUniteVariant
                {
                    name = ParserNodeFactory.create_identifier(varName),
                    body = new ObjectBody
                    {
                        fields = fields
                    },
                    annotations = build_annotations(variantAttrs, docs: variantDocs)
                });
                tokens.match(ValkyrieTokenKind.comma);
            }

            tokens.expect(ValkyrieTokenKind.brace_r);
            return new DeclareUnite
            {
                name = ParserNodeFactory.create_identifier(name),
                type_parameters = typeParameters,
                variants = variants,
                annotations = build_annotations(attrs)
            };
        }

        private DeclareUnite parse_unite_decl()
        {
            return tokens.parse_union_decl();
        }

        private DeclareImply parse_imply_decl(ValkyrieLanguage language)
        {
            var targetType = tokens.parse_type();
            TypeNode? contractType = null;
            if (tokens.match(ValkyrieTokenKind.colon)) contractType = tokens.parse_type();

            var attrs = tokens.parse_attributes();
            var docs = tokens.parse_doc_comments();

            tokens.expect(ValkyrieTokenKind.brace_l);
            var methods = new List<DeclareObjectMethod>();
            var associatedTypes = new List<DeclareAssociatedType>();
            while (!tokens.check(ValkyrieTokenKind.brace_r) && !tokens.is_at_end())
            {
                if (tokens.is_function_decl_follows())
                {
                    var blockMods = tokens.collect_block_function_modifiers();
                    var method = tokens.parse_function_decl(language, blockMods);
                    methods.Add(new DeclareObjectMethod
                    {
                        name = method.name,
                        annotations = method.annotations,
                        parameters = method.parameters,
                        return_type = method.return_type,
                        body = method.body
                    });
                    continue;
                }

                if (tokens.check(ValkyrieTokenKind.type))
                {
                    associatedTypes.Add(tokens.parse_associated_type_decl([], [], []));
                    continue;
                }

                tokens.skip_unrecognized_token();
            }

            tokens.expect(ValkyrieTokenKind.brace_r);
            return new DeclareImply
            {
                target_type = targetType,
                contract_type = contractType,
                methods = methods,
                associated_types = associatedTypes,
                annotations = build_annotations(attrs, docs: docs)
            };
        }

        private DeclareUsing parse_using_decl()
        {
            var isReexport = false;
            if (tokens.check(ValkyrieTokenKind.bang))
            {
                tokens.advance();
                isReexport = true;
            }

            var (modulePath, selections) = tokens.parse_using_target();

            string? alias = null;
            if (tokens.check(ValkyrieTokenKind.@as))
            {
                tokens.advance();
                alias = tokens.advance_text();
            }

            if (!tokens.match(ValkyrieTokenKind.semicolon))
                tokens.diagnostics?.report_warning(new DiagnosticTextSpan(tokens.position, 1), "缺少分号");

            return new DeclareUsing
            {
                is_reexport = isReexport,
                module_path = modulePath,
                @namespace = ParserNodeFactory.create_qualified_path(modulePath),
                alias = alias is null ? null : ParserNodeFactory.create_identifier(alias),
                selections = selections
            };
        }

        private DeclareNamespace parse_namespace_decl()
        {
            var isPrimary = false;
            if (tokens.check(ValkyrieTokenKind.bang))
            {
                tokens.advance();
                isPrimary = true;
            }

            var name = tokens.parse_dotted_name();
            var attrs = tokens.parse_attributes();

            if (isPrimary || tokens.check(ValkyrieTokenKind.semicolon))
            {
                tokens.match(ValkyrieTokenKind.semicolon);
                return new DeclareNamespace
                {
                    name = ParserNodeFactory.create_identifier(name),
                    is_primary = isPrimary,
                    declarations = [],
                    attributes = attrs
                };
            }

            tokens.expect(ValkyrieTokenKind.brace_l);
            var decls = new List<ValkyrieNode>();

            while (!tokens.check(ValkyrieTokenKind.brace_r) && !tokens.is_at_end())
                if (tokens.is_declaration_start(ValkyrieLanguage.standard))
                    decls.Add(tokens.parse_declaration(ValkyrieLanguage.standard));
                else
                    decls.Add(tokens.parse_statement_node(ValkyrieLanguage.standard));

            tokens.expect(ValkyrieTokenKind.brace_r);

            return new DeclareNamespace
            {
                name = ParserNodeFactory.create_identifier(name),
                declarations = decls,
                attributes = attrs
            };
        }

        private DeclareModel parse_model_declaration()
        {
            var name = tokens.advance_text();
            var attrs = tokens.parse_attributes();
            var docs = tokens.parse_doc_comments();
            var body = tokens.parse_object_body(ValkyrieLanguage.standard, allowMethods: false, allowDomains: false);

            return new DeclareModel
            {
                name = ParserNodeFactory.create_identifier(name),
                body = body,
                annotations = build_annotations(attrs, docs: docs)
            };
        }

        private DeclareService parse_service_declaration()
        {
            var name = tokens.advance_text();
            var attrs = tokens.parse_attributes();

            tokens.expect(ValkyrieTokenKind.brace_l);

            tokens.expect(ValkyrieTokenKind.brace_r);

            return new DeclareService
            {
                name = ParserNodeFactory.create_identifier(name),
                annotations = build_annotations(attrs)
            };
        }

        private NeuralDecl parse_neural_decl()
        {
            var name = tokens.advance_text();
            var attrs = tokens.parse_attributes();

            tokens.expect(ValkyrieTokenKind.brace_l);
            var layers = new List<NeuralLayerDecl>();

            while (!tokens.check(ValkyrieTokenKind.brace_r) && !tokens.is_at_end())
                if (tokens.check(ValkyrieTokenKind.identifier))
                    layers.Add(tokens.parse_generic_neural_layer());
                else
                    tokens.skip_unrecognized_token();

            tokens.expect(ValkyrieTokenKind.brace_r);

            return new NeuralDecl { name = name, attributes = attrs, layers = layers };
        }

        private NeuralLayerDecl parse_generic_neural_layer()
        {
            var layerKind = tokens.advance_text();
            var name = tokens.advance_text();
            tokens.expect(ValkyrieTokenKind.brace_l);
            var parameters = new List<NeuralLayerParamDecl>();
            while (!tokens.check(ValkyrieTokenKind.brace_r) && !tokens.is_at_end())
            {
                var paramName = tokens.advance_text();
                tokens.expect(ValkyrieTokenKind.equal);
                var value = tokens.parse_term_node();
                if (value is TermLiteralNumberNode or
                    TermLiteralTextNode or
                    TermLiteralBooleanNode or
                    LiteralNullNode)
                    parameters.Add(new NeuralLayerParamDecl { name = paramName, value = value });
                else
                    tokens.diagnostics?.report_error(new DiagnosticTextSpan(tokens.position, 1), "Neural 层参数必须是字面量。");

                tokens.match(ValkyrieTokenKind.semicolon);
                tokens.match(ValkyrieTokenKind.comma);
            }

            tokens.expect(ValkyrieTokenKind.brace_r);

            return new SimpleLayerDecl { layer_kind = layerKind, name = name, parameters = parameters };
        }

        private DeclareObjectDomain parse_domain_decl(ValkyrieLanguage language)
        {
            var name = tokens.advance_text();
            var attrs = tokens.parse_attributes();
            var body = tokens.parse_object_body(language);

            return new DeclareObjectDomain
            {
                name = ParserNodeFactory.create_identifier(name),
                attributes = attrs,
                body = body
            };
        }

        private ObjectBody parse_object_body(
            ValkyrieLanguage language,
            List<QueryExpr>? queries = null,
            bool allowFields = true,
            bool allowMethods = true,
            bool allowDomains = true)
        {
            tokens.expect(ValkyrieTokenKind.brace_l);

            var fields = new List<DeclareObjectField>();
            var methods = new List<DeclareObjectMethod>();
            var associatedTypes = new List<DeclareAssociatedType>();
            var domains = new List<DeclareObjectDomain>();

            while (!tokens.check(ValkyrieTokenKind.brace_r) && !tokens.is_at_end())
            {
                var leadingDocs = tokens.parse_doc_comments();
                var leadingAttrs = tokens.parse_attributes();
                var leadingMods = tokens.parse_modifiers();

                if (queries is not null
                    && tokens.peek_valkyrie_kind() == ValkyrieTokenKind.identifier
                    && tokens.peek_text().StartsWith("query", StringComparison.OrdinalIgnoreCase))
                {
                    if (tokens.is_identifier_followed_by_equals())
                        queries.Add(tokens.parse_named_query_expr());
                    else
                        queries.Add(tokens.parse_query_expr());

                    continue;
                }

                if (allowMethods && tokens.is_function_decl_follows())
                {
                    var blockMods = leadingMods.Count > 0 ? leadingMods : tokens.collect_block_function_modifiers();
                    var method = tokens.parse_function_decl(language, blockMods);
                    if (leadingAttrs.Count > 0 || leadingDocs.Count > 0)
                        method = method with
                        {
                            annotations = merge_annotations(method.annotations, leadingAttrs, [], leadingDocs)
                        };

                    methods.Add(new DeclareObjectMethod
                    {
                        name = method.name,
                        annotations = method.annotations,
                        parameters = method.parameters,
                        return_type = method.return_type,
                        body = method.body
                    });
                    continue;
                }

                if (tokens.check(ValkyrieTokenKind.type))
                {
                    associatedTypes.Add(tokens.parse_associated_type_decl(leadingAttrs, leadingMods, leadingDocs));
                    continue;
                }

                if (allowDomains
                    && tokens.peek_valkyrie_kind() == ValkyrieTokenKind.identifier
                    && tokens.peek_valkyrie_kind(1) == ValkyrieTokenKind.brace_l)
                {
                    domains.Add(tokens.parse_domain_decl(language));
                    continue;
                }

                if (allowFields && tokens.is_field_start())
                {
                    fields.Add(tokens.parse_field(leadingAttrs, leadingMods, leadingDocs));
                    continue;
                }

                tokens.skip_unrecognized_token();
            }

            tokens.expect(ValkyrieTokenKind.brace_r);

            return new ObjectBody
            {
                fields = fields,
                methods = methods,
                associated_types = associatedTypes,
                domains = domains
            };
        }

        private DeclareTraitAlias parse_type_alias_decl()
        {
            var name = tokens.advance_text();
            tokens.expect(ValkyrieTokenKind.equal);
            var target = tokens.parse_type();
            tokens.match(ValkyrieTokenKind.semicolon);

            return new DeclareTraitAlias { name = ParserNodeFactory.create_identifier(name), target_type = target };
        }

        private DeclareAssociatedType parse_associated_type_decl(
            IReadOnlyList<AttributeItem>? leadingAttrs = null,
            IReadOnlyList<string>? leadingModifiers = null,
            IReadOnlyList<DocumentComment>? leadingDocs = null)
        {
            tokens.expect(ValkyrieTokenKind.type);
            var name = tokens.advance_text();
            TypeNode? constraintType = null;
            TypeNode? defaultType = null;

            if (tokens.check(ValkyrieTokenKind.colon))
            {
                tokens.advance();
                constraintType = tokens.parse_type();
            }

            if (tokens.check(ValkyrieTokenKind.equal))
            {
                tokens.advance();
                defaultType = tokens.parse_type();
            }

            tokens.match(ValkyrieTokenKind.semicolon);

            return new DeclareAssociatedType
            {
                name = ParserNodeFactory.create_identifier(name),
                constraint = constraintType is null ? null : new WhereConstraintNode(),
                default_type = defaultType,
                annotations = build_annotations(leadingAttrs ?? [], leadingModifiers ?? [], leadingDocs ?? [])
            };
        }

        private ShaderDecl parse_shader_decl()
        {
            var name = tokens.advance_text();
            var attrs = tokens.parse_attributes();
            tokens.expect(ValkyrieTokenKind.brace_l);
            var stages = new List<ShaderStageDecl>();
            while (!tokens.check(ValkyrieTokenKind.brace_r) && !tokens.is_at_end())
            {
                if (!tokens.is_keyword())
                {
                    tokens.skip_unrecognized_token();
                    continue;
                }

                var keyword = tokens.peek_text().ToLowerInvariant();
                if (keyword == "vertex")
                    stages.Add(tokens.parse_shader_stage<VertexShaderDecl>("vertex"));
                else if (keyword == "fragment")
                    stages.Add(tokens.parse_shader_stage<FragmentShaderDecl>("fragment"));
                else if (keyword == "compute")
                    stages.Add(tokens.parse_shader_stage<ComputeShaderDecl>("compute"));
                else
                    tokens.skip_unrecognized_token();
            }

            tokens.expect(ValkyrieTokenKind.brace_r);
            return new ShaderDecl
            {
                name = ParserNodeFactory.create_identifier(name),
                annotations = build_annotations(attrs),
                stages = stages
            };
        }

        private T parse_shader_stage<T>(string keyword) where T : ShaderStageDecl, new()
        {
            tokens.advance();
            var name = tokens.advance_text();
            var attrs = tokens.parse_attributes();
            tokens.expect(ValkyrieTokenKind.parenthesis_l);
            tokens.expect(ValkyrieTokenKind.parenthesis_r);
            var body = new List<ValkyrieNode>();
            if (tokens.check(ValkyrieTokenKind.brace_l))
            {
                tokens.advance();
                while (!tokens.check(ValkyrieTokenKind.brace_r) && !tokens.is_at_end())
                    body.Add(tokens.parse_statement_node(ValkyrieLanguage.standard));

                tokens.expect(ValkyrieTokenKind.brace_r);
            }

            return new T { name = name, attributes = attrs, body = body };
        }

        private DeclareStructure parse_struct_decl()
        {
            var name = tokens.advance_text();
            var typeParameters = tokens.parse_type_parameters();
            var attrs = tokens.parse_attributes();
            var body = tokens.parse_object_body(ValkyrieLanguage.standard, allowDomains: false);
            var genericConstraints = tokens.parse_generic_constraints();
            return new DeclareStructure
            {
                name = ParserNodeFactory.create_identifier(name),
                body = body,
                annotations = build_annotations(attrs),
                type_parameters = typeParameters,
                generic_constraints = genericConstraints
            };
        }

        private ValkyrieNode parse_class(ValkyrieLanguage language)
        {
            if (tokens.check(ValkyrieTokenKind.brace_l))
            {
                var anonymousBody = tokens.parse_object_body(language);
                var anonymousGenericConstraints = tokens.parse_generic_constraints();

                return new AnonymousClass
                {
                    body = anonymousBody,
                    type_parameters = tokens.parse_type_parameters(),
                    generic_constraints = anonymousGenericConstraints
                };
            }

            var name = tokens.advance_text();
            IReadOnlyList<InheritanceItem>? inheritItems = null;

            if (tokens.check(ValkyrieTokenKind.parenthesis_l))
            {
                tokens.advance();
                var items = new List<InheritanceItem>();

                while (!tokens.check(ValkyrieTokenKind.parenthesis_r) && !tokens.is_at_end())
                {
                    items.Add(tokens.parse_inheritance_item());

                    if (tokens.check(ValkyrieTokenKind.comma))
                    {
                        tokens.advance();
                        continue;
                    }

                    break;
                }

                tokens.expect(ValkyrieTokenKind.parenthesis_r);
                inheritItems = items;
            }

            var typeParameters = tokens.parse_type_parameters();
            var attrs = tokens.parse_attributes();
            var body = tokens.parse_object_body(language);
            var genericConstraints = tokens.parse_generic_constraints();

            return new DeclareClass
            {
                name = ParserNodeFactory.create_identifier(name),
                inheritance = inheritItems is null
                    ? null
                    : new InheritanceList
                    {
                        bases = inheritItems
                    },
                type_parameters = typeParameters,
                body = body,
                annotations = build_annotations(attrs),
                generic_constraints = genericConstraints
            };
        }

        private ValkyrieNode parse_trait(ValkyrieLanguage language)
        {
            if (tokens.check(ValkyrieTokenKind.brace_l))
            {
                var anonymousBody = tokens.parse_object_body(language, allowFields: false, allowDomains: false);

                var anonymousTypeParameters = tokens.parse_type_parameters();
                var anonymousGenericConstraints = tokens.parse_generic_constraints();

                return new AnonymousTrait
                {
                    type_parameters = anonymousTypeParameters,
                    body = anonymousBody, generic_constraints = anonymousGenericConstraints
                };
            }

            var name = tokens.advance_text();

            var typeParameters = tokens.parse_type_parameters();
            IReadOnlyList<InheritanceItem>? inheritItems = null;
            if (tokens.check(ValkyrieTokenKind.colon))
            {
                tokens.advance();
                var items = new List<InheritanceItem> { tokens.parse_inheritance_item() };
                while (tokens.check(ValkyrieTokenKind.comma))
                {
                    tokens.advance();
                    items.Add(tokens.parse_inheritance_item());
                }

                inheritItems = items;
            }

            ObjectBody? body = null;

            if (tokens.check(ValkyrieTokenKind.equal))
            {
                tokens.advance();
                _ = tokens.parse_type();
            }
            else
            {
                body = tokens.parse_object_body(language, allowFields: false, allowDomains: false);
            }

            var genericConstraints = tokens.parse_generic_constraints();
            return new DeclareTrait
            {
                name = ParserNodeFactory.create_identifier(name),
                type_parameters = typeParameters,
                inheritance = inheritItems is null
                    ? null
                    : new InheritanceList
                    {
                        bases = inheritItems
                    },
                body = body,
                generic_constraints = genericConstraints
            };
        }

        private InheritanceItem parse_inheritance_item()
        {
            if (tokens.check(ValkyrieTokenKind.identifier))
            {
                var nameText = tokens.peek_text();

                if (tokens.peek_text(1) == ":")
                {
                    tokens.advance();
                    tokens.advance();

                    var baseType = tokens.parse_type();
                    return new InheritanceItem
                    {
                        name = ParserNodeFactory.create_identifier(nameText),
                        base_type = baseType
                    };
                }
            }

            var type = tokens.parse_type();
            return new InheritanceItem
            {
                base_type = type
            };
        }

        private UniformDecl parse_uniform_decl()
        {
            var name = tokens.advance_text();
            var attrs = tokens.parse_attributes();
            tokens.expect(ValkyrieTokenKind.colon);
            var type = tokens.parse_type();
            int? group = null;
            int? binding = null;

            if (tokens.check(ValkyrieTokenKind.identifier) && tokens.peek_text() == "group")
            {
                tokens.advance();
                group = tokens.safe_parse_int(tokens.advance_text());
            }

            if (tokens.check(ValkyrieTokenKind.binding))
            {
                tokens.advance();
                binding = tokens.safe_parse_int(tokens.advance_text());
            }
            else if (tokens.check(ValkyrieTokenKind.identifier) && tokens.peek_text() == "binding")
            {
                tokens.advance();
                binding = tokens.safe_parse_int(tokens.advance_text());
            }

            tokens.match(ValkyrieTokenKind.semicolon);
            tokens.match(ValkyrieTokenKind.comma);
            return new UniformDecl
            {
                name = name,
                uniform_type = type,
                group = group,
                binding = binding,
                annotations = build_annotations(attrs)
            };
        }

        private VaryingDecl parse_varying_decl()
        {
            var name = tokens.advance_text();
            var attrs = tokens.parse_attributes();
            tokens.expect(ValkyrieTokenKind.colon);
            var type = tokens.parse_type();
            string? interpolation = null;

            tokens.match(ValkyrieTokenKind.semicolon);
            tokens.match(ValkyrieTokenKind.comma);
            return new VaryingDecl
            {
                name = name,
                varying_type = type,
                interpolation = interpolation,
                annotations = build_annotations(attrs)
            };
        }

        private ConstantBufferDecl parse_constant_buffer_decl()
        {
            var name = tokens.advance_text();
            var attrs = tokens.parse_attributes();
            tokens.expect(ValkyrieTokenKind.brace_l);
            var fields = new List<DeclareObjectField>();
            while (!tokens.check(ValkyrieTokenKind.brace_r) && !tokens.is_at_end())
                if (tokens.peek_valkyrie_kind() == ValkyrieTokenKind.identifier)
                    fields.Add(tokens.parse_field());
                else
                    tokens.skip_unrecognized_token();

            tokens.expect(ValkyrieTokenKind.brace_r);
            int? group = null;
            int? binding = null;
            if (tokens.check(ValkyrieTokenKind.identifier) && tokens.peek_text() == "group")
            {
                tokens.advance();
                group = tokens.safe_parse_int(tokens.advance_text());
            }

            if (tokens.check(ValkyrieTokenKind.binding))
            {
                tokens.advance();
                binding = tokens.safe_parse_int(tokens.advance_text());
            }
            else if (tokens.check(ValkyrieTokenKind.identifier) && tokens.peek_text() == "binding")
            {
                tokens.advance();
                binding = tokens.safe_parse_int(tokens.advance_text());
            }

            tokens.match(ValkyrieTokenKind.semicolon);
            tokens.match(ValkyrieTokenKind.comma);
            return new ConstantBufferDecl
            {
                name = name,
                fields = fields,
                group = group,
                binding = binding,
                annotations = build_annotations(attrs)
            };
        }

        private TextureDecl parse_texture_decl()
        {
            var name = tokens.advance_text();
            var attrs = tokens.parse_attributes();
            tokens.expect(ValkyrieTokenKind.colon);
            var type = tokens.parse_type();
            int? group = null;
            int? binding = null;
            if (tokens.check(ValkyrieTokenKind.identifier) && tokens.peek_text() == "group")
            {
                tokens.advance();
                group = tokens.safe_parse_int(tokens.advance_text());
            }

            if (tokens.check(ValkyrieTokenKind.binding))
            {
                tokens.advance();
                binding = tokens.safe_parse_int(tokens.advance_text());
            }
            else if (tokens.check(ValkyrieTokenKind.identifier) && tokens.peek_text() == "binding")
            {
                tokens.advance();
                binding = tokens.safe_parse_int(tokens.advance_text());
            }

            tokens.match(ValkyrieTokenKind.semicolon);
            tokens.match(ValkyrieTokenKind.comma);
            return new TextureDecl
            {
                name = name,
                texture_type = type,
                group = group,
                binding = binding,
                annotations = build_annotations(attrs)
            };
        }

        private SamplerDecl parse_sampler_decl()
        {
            var name = tokens.advance_text();
            var attrs = tokens.parse_attributes();
            int? group = null;
            int? binding = null;
            if (tokens.check(ValkyrieTokenKind.identifier) && tokens.peek_text() == "group")
            {
                tokens.advance();
                group = tokens.safe_parse_int(tokens.advance_text());
            }

            if (tokens.check(ValkyrieTokenKind.binding))
            {
                tokens.advance();
                binding = tokens.safe_parse_int(tokens.advance_text());
            }
            else if (tokens.check(ValkyrieTokenKind.identifier) && tokens.peek_text() == "binding")
            {
                tokens.advance();
                binding = tokens.safe_parse_int(tokens.advance_text());
            }

            tokens.match(ValkyrieTokenKind.semicolon);
            tokens.match(ValkyrieTokenKind.comma);
            return new SamplerDecl
            {
                name = name,
                group = group,
                binding = binding,
                annotations = build_annotations(attrs)
            };
        }

        internal DeclareLet parse_variable_decl(ValkyrieLanguage language,
            IReadOnlyList<string> modifiers)
        {
            var isMutable = false;
            PatternNode? pattern = null;

            if (tokens.check(ValkyrieTokenKind.identifier)
                && string.Equals(tokens.peek_text(), "mut", StringComparison.OrdinalIgnoreCase))
            {
                tokens.advance();
                isMutable = true;
            }

            var name = tokens.advance_text();
            var attrs = tokens.parse_attributes();

            TypeNode? type = null;
            ValkyrieNode? init = null;

            if (tokens.check(ValkyrieTokenKind.colon))
            {
                tokens.advance();
                type = tokens.parse_type();
            }

            if (tokens.check(ValkyrieTokenKind.equal))
            {
                tokens.advance();
                init = tokens.parse_term_node();
            }

            tokens.match(ValkyrieTokenKind.semicolon);
            tokens.match(ValkyrieTokenKind.comma);

            return new DeclareLet
            {
                name = name is null ? null : ParserNodeFactory.create_identifier(name),
                pattern = pattern,
                is_mutable = isMutable,
                var_type = type,
                initializer = init,
                annotations = build_annotations(attrs, modifiers)
            };
        }

        private ValkyrieNode skip_unknown_decl()
        {
            if (tokens.check(ValkyrieTokenKind.brace_r) || tokens.check(ValkyrieTokenKind.semicolon))
                return new UnknownDecl { content = "" };

            var content = tokens.advance_text();
            while (!tokens.is_at_end()
                   && !tokens.check(ValkyrieTokenKind.semicolon)
                   && !tokens.check(ValkyrieTokenKind.brace_r))
                content += " " + tokens.advance_text();

            tokens.match(ValkyrieTokenKind.semicolon);
            tokens.diagnostics?.report_warning(new DiagnosticTextSpan(tokens.position, 1), $"无法识别的声明：{content}");
            return new UnknownDecl { content = content };
        }

        private bool is_field_start()
        {
            var kind = tokens.peek_valkyrie_kind();
            return kind is ValkyrieTokenKind.identifier or ValkyrieTokenKind.bracket_l;
        }
    }

    extension(TokenStream tokens)
    {
        private bool is_function_decl_follows()
        {
            return tokens.count_leading_function_modifiers() >= 0;
        }

        private int count_leading_function_modifiers()
        {
            var count = 0;

            var firstToken = tokens.peek(count);
            if (firstToken.kind.is_keyword() || firstToken.kind == ValkyrieTokenKind.identifier.to_node_kind())
            {
                if (_s_body_declaration_keywords.Contains(tokens.peek_text(count))) return -1;

                if (firstToken.kind.is_keyword() && is_shader_stage_keyword(tokens.peek_text(count))) return -1;
            }

            while (true)
            {
                var kind = tokens.peek(count).kind;
                if (kind != ValkyrieTokenKind.identifier.to_node_kind() && !kind.is_keyword()) return -1;

                count++;
                var next = tokens.peek(count);

                // 跳过函数名后的泛型类型参数（例如 micro map<U>(...)）
                while (next.kind == ValkyrieTokenKind.less.to_node_kind()
                       || next.kind == ValkyrieTokenKind.generic_l.to_node_kind())
                {
                    var depth = 1;
                    count++;
                    while (depth > 0)
                    {
                        var tk = tokens.peek(count).kind;
                        if (tk == ValkyrieTokenKind.less.to_node_kind()
                            || tk == ValkyrieTokenKind.generic_l.to_node_kind())
                        {
                            depth++;
                        }
                        else if (tk == ValkyrieTokenKind.greater.to_node_kind()
                                 || tk == ValkyrieTokenKind.generic_r.to_node_kind())
                        {
                            depth--;
                        }
                        else if (tk == ValkyrieTokenKind.greater_greater.to_node_kind())
                        {
                            depth -= 2;
                        }

                        count++;
                    }

                    next = tokens.peek(count);
                }

                if (next.kind == ValkyrieTokenKind.parenthesis_l.to_node_kind()) return count;

                if (next.kind != ValkyrieTokenKind.identifier.to_node_kind() && !next.kind.is_keyword()) return -1;
            }
        }

        private IReadOnlyList<string> collect_block_function_modifiers()
        {
            var count = tokens.count_leading_function_modifiers();
            if (count <= 0) return [];

            var mods = new List<string>(count);
            for (var i = 0; i < count - 1; i++) mods.Add(tokens.advance_text());

            return mods;
        }
    }

    private static bool is_shader_stage_keyword(string text)
    {
        return text is "vertex" or "fragment" or "compute"
            or "raygen" or "closesthit" or "anyhit" or "miss";
    }

    extension(TokenStream tokens)
    {
        private DeclareObjectField parse_field(
            IReadOnlyList<AttributeItem>? leadingAttrs = null,
            IReadOnlyList<string>? leadingModifiers = null,
            IReadOnlyList<DocumentComment>? leadingDocs = null)
        {
            var fieldAttrs = leadingAttrs ?? tokens.parse_attributes();
            var modifiers = leadingModifiers ?? tokens.parse_modifiers();
            var name = tokens.advance_text();
            var doc = leadingDocs ?? tokens.parse_doc_comments();

            tokens.expect(ValkyrieTokenKind.colon);
            var type = tokens.parse_type();

            ValkyrieNode? defaultVal = null;
            if (tokens.check(ValkyrieTokenKind.equal))
            {
                tokens.advance();
                defaultVal = tokens.parse_term_node();
            }

            tokens.match(ValkyrieTokenKind.semicolon);
            tokens.match(ValkyrieTokenKind.comma);

            return new DeclareObjectField
            {
                name = name,
                field_type = type,
                default_value = defaultVal,
                annotations = build_annotations(fieldAttrs, modifiers, doc)
            };
        }

        internal TypeNode parse_type()
        {
            return tokens.parse_type_expression_node();
        }

        private QueryExpr parse_query_expr()
        {
            tokens.advance();

            if (tokens.check(ValkyrieTokenKind.equal)) tokens.advance();

            var result = tokens.parse_query_body();

            return result;
        }

        private QueryKind parse_query_kind()
        {
            if (tokens.check(ValkyrieTokenKind.dot))
            {
                tokens.advance();
                var method = tokens.advance_text().ToLowerInvariant();
                return method switch
                {
                    "all" => QueryKind.all,
                    "any" => QueryKind.any,
                    "none" => QueryKind.none,
                    _ => QueryKind.all
                };
            }

            return QueryKind.all;
        }

        private QueryExpr parse_query_body()
        {
            var calleeText = tokens.advance_text();

            var kind = tokens.parse_query_kind();

            tokens.expect(ValkyrieTokenKind.parenthesis_l);
            var comps = new List<TypeNode>();

            while (!tokens.check(ValkyrieTokenKind.parenthesis_r) && !tokens.is_at_end())
            {
                comps.Add(tokens.parse_type());
                tokens.match(ValkyrieTokenKind.comma);
            }

            tokens.expect(ValkyrieTokenKind.parenthesis_r);

            var filters = new List<QueryExpr>();
            while (tokens.check(ValkyrieTokenKind.dot))
            {
                tokens.advance();
                break;
            }

            return new QueryExpr
            {
                kind = kind,
                component_types = comps,
                filters = filters.Count > 0 ? filters : null
            };
        }

        private QueryExpr parse_named_query_expr()
        {
            tokens.advance();
            tokens.advance();
            return tokens.parse_query_body();
        }

        private List<TermParameterList> parse_param_list()
        {
            var list = new List<TermParameterList>();
            while (!tokens.check(ValkyrieTokenKind.parenthesis_r) && !tokens.is_at_end())
            {
                if (tokens.check(ValkyrieTokenKind.identifier)
                    && string.Equals(tokens.peek_text(), "mut", StringComparison.OrdinalIgnoreCase))
                {
                    tokens.advance();
                }

                var name = tokens.advance_text();
                TypeNode? type = null;
                if (tokens.check(ValkyrieTokenKind.colon))
                {
                    tokens.advance();
                    type = tokens.parse_type();
                }

                list.Add(DeclarationNodeExtensions.create_single_term_parameter_list(
                    ParserNodeFactory.create_identifier(name),
                    type ?? TypeNodeExtensions.create_any_type()));
                tokens.match(ValkyrieTokenKind.comma);
            }

            return list;
        }

        internal IReadOnlyList<AttributeItem> parse_attributes()
        {
            var attrs = new List<AttributeItem>();

            while (tokens.check(ValkyrieTokenKind.bracket_l))
            {
                var nextKind = tokens.peek(1).kind;
                if (nextKind != ValkyrieTokenKind.identifier.to_node_kind() && !nextKind.is_keyword()) break;

                tokens.advance();

                var contentBuilder = new StringBuilder();
                var depth = 1;
                while (depth > 0 && !tokens.is_at_end())
                    if (tokens.check(ValkyrieTokenKind.bracket_l))
                    {
                        depth++;
                        contentBuilder.Append(tokens.advance_text());
                    }
                    else if (tokens.check(ValkyrieTokenKind.bracket_r))
                    {
                        depth--;
                        if (depth > 0)
                            contentBuilder.Append(tokens.advance_text());
                        else
                            tokens.advance();
                    }
                    else
                    {
                        contentBuilder.Append(tokens.advance_text());
                    }

                var content = contentBuilder.ToString().Trim();
                var parts = split_attribute_parts(content);
                foreach (var part in parts)
                {
                    var trimmed = part.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    attrs.Add(parse_single_attribute(trimmed));
                }
            }

            return attrs;
        }
    }

    private static List<string> split_attribute_parts(string content)
    {
        var parts = new List<string>();
        var parenDepth = 0;
        var inString = false;
        var start = 0;

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];
            if (c == '"' && (i == 0 || content[i - 1] != '\\'))
            {
                inString = !inString;
            }
            else if (!inString)
            {
                if (c == '(')
                {
                    parenDepth++;
                }
                else if (c == ')')
                {
                    parenDepth--;
                }
                else if (c == ',' && parenDepth == 0)
                {
                    parts.Add(content[start..i]);
                    start = i + 1;
                }
            }
        }

        if (start < content.Length) parts.Add(content[start..]);

        return parts;
    }

    private static AttributeItem parse_single_attribute(string text)
    {
        var parenIndex = text.IndexOf('(');
        if (parenIndex < 0) return new AttributeItem { name = text };

        var name = text[..parenIndex].Trim();
        var lastParen = text.LastIndexOf(')');
        if (lastParen <= parenIndex) return new AttributeItem { name = text };

        var argsContent = text[(parenIndex + 1)..lastParen];

        var argParts = split_attribute_parts(argsContent);
        var arguments = new List<TermArgumentItem>();
        foreach (var arg in argParts)
        {
            var value = arg.Trim().Trim('"');
            arguments.Add(TermNodeExtensions.create_argument_item(
                ParserNodeFactory.create_term_literal_text(value, TextLiteralKind.literal_text)));
        }

        return new AttributeItem
        {
            name = name,
            arguments = TermNodeExtensions.create_argument_list(arguments)
        };
    }

    extension(TokenStream tokens)
    {
        internal IReadOnlyList<DocumentComment> parse_doc_comments()
        {
            var docs = new List<DocumentComment>();
            while (tokens.check(ValkyrieTokenKind.comment_start))
            {
                var marker = tokens.advance_text();
                if (tokens.check(ValkyrieTokenKind.comment_content))
                {
                    var content = tokens.advance_text();
                    if (marker is "#?" or "///") docs.Add(new DocumentComment { content = content });
                }
            }

            return docs;
        }

        private string parse_dotted_name()
        {
            var name = tokens.advance_text();
            while (tokens.check(ValkyrieTokenKind.dot) || tokens.check(ValkyrieTokenKind.double_colon))
            {
                tokens.advance();
                name += "." + tokens.advance_text();
            }

            return name;
        }

        private (string modulePath, IReadOnlyList<IdentifierNode> selections) parse_using_target()
        {
            var segments = new List<string> { tokens.advance_text() };

            while (tokens.check(ValkyrieTokenKind.dot) || tokens.check(ValkyrieTokenKind.double_colon))
            {
                if (tokens.peek(1).kind == ValkyrieTokenKind.brace_l.to_node_kind())
                {
                    tokens.advance();
                    return (string.Join(".", segments), tokens.parse_using_selection_list());
                }

                tokens.advance();
                segments.Add(tokens.advance_text());
            }

            return (string.Join(".", segments), []);
        }

        private IReadOnlyList<IdentifierNode> parse_using_selection_list()
        {
            tokens.expect(ValkyrieTokenKind.brace_l);
            var selections = new List<IdentifierNode>();

            while (!tokens.check(ValkyrieTokenKind.brace_r) && !tokens.is_at_end())
            {
                selections.Add(ParserNodeFactory.create_identifier(tokens.advance_text()));
                tokens.match(ValkyrieTokenKind.comma);
            }

            tokens.expect(ValkyrieTokenKind.brace_r);
            return selections;
        }

        private IReadOnlyList<string> parse_modifiers()
        {
            var mods = new List<string>();
            while (!tokens.is_at_end())
            {
                var kind = tokens.peek().kind;
                var nextKind = tokens.peek(1).kind;

                if (kind == ValkyrieTokenKind.identifier.to_node_kind()
                    && (nextKind == ValkyrieTokenKind.identifier.to_node_kind()
                        || (nextKind.is_keyword() && !nextKind.is_statement_keyword())))
                    mods.Add(tokens.advance_text());
                else
                    break;
            }

            return mods;
        }

        private IReadOnlyList<TypeParameterList> parse_type_parameters()
        {
            if (!tokens.check(ValkyrieTokenKind.less)) return [];

            tokens.advance();
            var typeParams = new List<TypeParameterList>();

            while (!tokens.is_at_end()
                   && !tokens.check(ValkyrieTokenKind.greater))
            {
                var name = tokens.advance_text();
                typeParams.Add(DeclarationNodeExtensions.create_single_type_parameter_list(
                    ParserNodeFactory.create_identifier(name)));
                tokens.match(ValkyrieTokenKind.comma);
            }

            tokens.advance();
            return typeParams;
        }

        private IReadOnlyList<GenericConstraint> parse_generic_constraints()
        {
            var constraints = new List<GenericConstraint>();

            while (tokens.check(ValkyrieTokenKind.where))
            {
                tokens.advance();
                var parameterName = tokens.advance_text();
                tokens.expect(ValkyrieTokenKind.colon);
                var constraintTypes = new List<TypeNode> { tokens.parse_type() };

                while (tokens.check(ValkyrieTokenKind.comma))
                {
                    tokens.advance();
                    constraintTypes.Add(tokens.parse_type());
                }

                constraints.Add(new GenericConstraint
                {
                    parameter_name = parameterName,
                    constraint_types = constraintTypes
                });
            }

            return constraints;
        }

        private int safe_parse_int(string text)
        {
            if (int.TryParse(text, out var result)) return result;

            tokens.diagnostics?.report_error(new DiagnosticTextSpan(tokens.position, 1), $"无法将 \"{text}\" 解析为整数。");
            return 0;
        }

        private void skip_unrecognized_token()
        {
            var text = tokens.peek_text();
            tokens.diagnostics?.report_warning(
                new DiagnosticTextSpan(tokens.position, 1), $"块内无法识别的元素：\"{text}\"");
            tokens.advance();
        }


        private bool is_generic_type_close()
        {
            var state = ValkyrieParser._s_generic_close_states.GetOrCreateValue(tokens);
            return state.pending_greater_closers > 0
                   || tokens.check(ValkyrieTokenKind.greater)
                   || tokens.check(ValkyrieTokenKind.generic_r)
                   || tokens.check(ValkyrieTokenKind.greater_greater);
        }

        private void consume_generic_type_close()
        {
            var state = ValkyrieParser._s_generic_close_states.GetOrCreateValue(tokens);
            if (state.pending_greater_closers > 0)
            {
                state.pending_greater_closers--;
                return;
            }

            if (tokens.check(ValkyrieTokenKind.greater) || tokens.check(ValkyrieTokenKind.generic_r))
            {
                tokens.advance();
                return;
            }

            if (tokens.check(ValkyrieTokenKind.greater_greater))
            {
                tokens.advance();
                state.pending_greater_closers++;
                return;
            }

            tokens.expect(ValkyrieTokenKind.greater);
        }
    }
}
