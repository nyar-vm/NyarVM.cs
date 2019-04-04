using System.Runtime.CompilerServices;
using Std.Data.Binary.Frame;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;
using TextSpan = Std.Text.TextSpan;

namespace Nyar.Analyzer.Semantic;

public static class SemanticModelSnapshotSerializer
{
    private const int _snapshot_version = 1;

    private const int _type_kind_primitive = 1;
    private const int _type_kind_named = 2;
    private const int _type_kind_function = 3;
    private const int _type_kind_generic = 4;
    private const int _type_kind_array = 5;
    private const int _type_kind_nullable = 6;
    private const int _type_kind_row = 7;
    private const int _type_kind_type_variable = 8;
    private const int _type_kind_auto = 9;
    private const int _type_kind_unknown = 10;
    private const int _type_kind_error = 11;

    #region 公共入口

    public static byte[] serialize(SemanticModel semanticModel)
    {
        ArgumentNullException.ThrowIfNull(semanticModel);

        var builder = SnapshotBuilder.build(semanticModel);
        var writer = new ByteBufferWriter(1024);

        writer.write_i32_le(_snapshot_version);
        ByteBufferStringIO.write_nullable_string(ref writer, semanticModel.file_path);

        write_scopes(ref writer, builder);
        write_symbols(ref writer, builder);
        write_types(ref writer, builder);
        write_symbol_bindings(ref writer, builder);
        write_type_bindings(ref writer, builder);
        write_diagnostics(ref writer, semanticModel.diagnostics);
        write_reference_buckets(ref writer, builder);

        return writer.to_array();
    }

    public static SemanticModel deserialize(ReadOnlySpan<byte> data)
    {
        var reader = new ByteBuffer(data);
        var version = reader.read_i32_le();
        if (version != _snapshot_version) throw new InvalidDataException($"不支持的 SemanticModel 快照版本：{version}");

        var filePath = ByteBufferStringIO.read_nullable_string(ref reader) ?? string.Empty;

        var scopes = read_scopes(ref reader);
        var symbolSnapshots = read_symbol_snapshots(ref reader);
        var symbols = create_symbols(symbolSnapshots);
        attach_symbols_to_scopes(scopes, symbols, symbolSnapshots);

        var typeSnapshots = read_type_snapshots(ref reader);
        var types = create_type_shells(typeSnapshots);
        hydrate_types(types, typeSnapshots, symbols);
        attach_symbol_types(symbols, symbolSnapshots, types);

        var symbolTable = new SymbolTable(scopes[0]);
        var semanticModel = new SemanticModel(filePath, symbolTable);

        read_symbol_bindings(ref reader, semanticModel, symbols);
        read_type_bindings(ref reader, semanticModel, types);
        read_diagnostics(ref reader, semanticModel);
        read_reference_buckets(ref reader, symbolTable, symbols);

        return semanticModel;
    }

    #endregion

    #region 写入快照

    private static void write_scopes(ref ByteBufferWriter writer, SnapshotBuilder builder)
    {
        writer.write_i32_le(builder.scopes.Count);
        foreach (var scope in builder.scopes)
        {
            ByteBufferStringIO.write_nullable_string(ref writer, scope.name);
            writer.write_i32_le(builder.get_scope_id(scope.parent as Scope));
        }
    }

    private static void write_symbols(ref ByteBufferWriter writer, SnapshotBuilder builder)
    {
        writer.write_i32_le(builder.symbols.Count);
        foreach (var symbol in builder.symbols)
        {
            if (symbol is not Symbol concreteSymbol) throw new InvalidOperationException($"当前仅支持序列化基于 Symbol 的符号，遇到 {symbol?.GetType().Name ?? "null"}。");
            writer.write_i32_le(builder.get_scope_id(symbol.containing_scope as Scope));
            writer.write_i32_le(builder.get_type_id(symbol.type));
            ByteBufferStringIO.write_nullable_string(ref writer, symbol.name);
            writer.write_i32_le((int)symbol.kind);
            writer.write_i32_le((int)symbol.accessibility);
            write_bool(ref writer, symbol.is_static);
            write_bool(ref writer, symbol.is_read_only);
            write_bool(ref writer, symbol.is_abstract);
            write_bool(ref writer, symbol.is_sealed);
            write_text_span(ref writer, concreteSymbol.definition_span);
            write_source_span(ref writer, concreteSymbol.definition_source_span);
            ByteBufferStringIO.write_nullable_string(ref writer, concreteSymbol.file_path);
        }
    }

    private static void write_types(ref ByteBufferWriter writer, SnapshotBuilder builder)
    {
        writer.write_i32_le(builder.types.Count);
        foreach (var type in builder.types)
            switch (type)
            {
                case PrimitiveType primitiveType:
                    writer.write_i32_le(_type_kind_primitive);
                    ByteBufferStringIO.write_nullable_string(ref writer, primitiveType.name);
                    writer.write_i32_le(builder.get_type_id(primitiveType.base_type));
                    break;
                case NamedType namedType:
                    writer.write_i32_le(_type_kind_named);
                    ByteBufferStringIO.write_nullable_string(ref writer, namedType.name);
                    ByteBufferStringIO.write_nullable_string(ref writer, namedType.kind_tag);
                    writer.write_i32_le(builder.get_type_id(namedType.base_type));
                    write_type_id_list(ref writer, builder, namedType.type_arguments);
                    write_symbol_id_list(ref writer, builder, namedType.members);
                    break;
                case FunctionType functionType:
                    writer.write_i32_le(_type_kind_function);
                    write_type_id_list(ref writer, builder, functionType.parameter_types);
                    writer.write_i32_le(builder.get_type_id(functionType.return_type));
                    break;
                case GenericType genericType:
                    writer.write_i32_le(_type_kind_generic);
                    ByteBufferStringIO.write_nullable_string(ref writer, genericType.name);
                    write_type_id_list(ref writer, builder, genericType.type_arguments);
                    break;
                case ArrayType arrayType:
                    writer.write_i32_le(_type_kind_array);
                    writer.write_i32_le(builder.get_type_id(arrayType.element_type));
                    break;
                case NullableType nullableType:
                    writer.write_i32_le(_type_kind_nullable);
                    writer.write_i32_le(builder.get_type_id(nullableType.inner_type));
                    break;
                case RowType rowType:
                    writer.write_i32_le(_type_kind_row);
                    ByteBufferStringIO.write_nullable_string(ref writer, rowType.name);
                    write_bool(ref writer, rowType.is_open);
                    write_symbol_id_list(ref writer, builder, rowType.members);
                    break;
                case TypeVariable typeVariable:
                    writer.write_i32_le(_type_kind_type_variable);
                    ByteBufferStringIO.write_nullable_string(ref writer, typeVariable.name);
                    writer.write_i32_le(builder.get_type_id(typeVariable.inferred_type));
                    break;
                case AutoType:
                    writer.write_i32_le(_type_kind_auto);
                    break;
                case UnknownType:
                    writer.write_i32_le(_type_kind_unknown);
                    break;
                case ErrorType:
                    writer.write_i32_le(_type_kind_error);
                    break;
                default:
                    throw new InvalidOperationException($"不支持序列化的类型：{type.GetType().FullName}");
            }
    }

    private static void write_symbol_bindings(ref ByteBufferWriter writer, SnapshotBuilder builder)
    {
        write_binding_map(ref writer, builder.symbol_bindings, builder.get_symbol_id);
    }

    private static void write_type_bindings(ref ByteBufferWriter writer, SnapshotBuilder builder)
    {
        write_binding_map(ref writer, builder.type_bindings, builder.get_type_id);
    }

    private static void write_diagnostics(ref ByteBufferWriter writer, IReadOnlyList<SemanticDiagnostic> diagnostics)
    {
        writer.write_i32_le(diagnostics.Count);
        foreach (var diagnostic in diagnostics)
        {
            writer.write_i32_le((int)diagnostic.level);
            ByteBufferStringIO.write_nullable_string(ref writer, diagnostic.message);
            writer.write_i32_le(diagnostic.span.start);
            writer.write_i32_le(diagnostic.span.length);
            ByteBufferStringIO.write_nullable_string(ref writer, diagnostic.code);
            ByteBufferStringIO.write_nullable_string(ref writer, diagnostic.file_path);
            write_source_span(ref writer, diagnostic.source_span);
        }
    }

    private static void write_reference_buckets(ref ByteBufferWriter writer, SnapshotBuilder builder)
    {
        writer.write_i32_le(builder.reference_buckets.Count);
        foreach (var bucket in builder.reference_buckets)
        {
            ByteBufferStringIO.write_nullable_string(ref writer, bucket.Key);
            write_id_list(ref writer, bucket.Value, builder.get_symbol_id);
        }
    }

    #endregion

    #region 读取快照

    private static Scope[] read_scopes(ref ByteBuffer reader)
    {
        var count = reader.read_i32_le();
        if (count <= 0) throw new InvalidDataException("SemanticModel 快照缺少全局作用域。");

        var scopes = new Scope[count];
        for (var i = 0; i < count; i++)
        {
            var name = ByteBufferStringIO.read_nullable_string(ref reader);
            var parentId = reader.read_i32_le();
            if (i == 0)
            {
                if (parentId >= 0) throw new InvalidDataException("全局作用域不能包含父作用域。");

                scopes[i] = new Scope(name);
                continue;
            }

            if (parentId < 0 || parentId >= i) throw new InvalidDataException($"作用域父节点索引无效：{parentId}");

            var parent = scopes[parentId];
            var scope = new Scope(name, parent);
            parent.attach_child_scope(scope);
            scopes[i] = scope;
        }

        return scopes;
    }

    private static SymbolSnapshot[] read_symbol_snapshots(ref ByteBuffer reader)
    {
        var count = reader.read_i32_le();
        var snapshots = new SymbolSnapshot[count];
        for (var i = 0; i < count; i++)
            snapshots[i] = new SymbolSnapshot(
                reader.read_i32_le(),
                reader.read_i32_le(),
                ByteBufferStringIO.read_nullable_string(ref reader) ?? string.Empty,
                (SymbolKind)reader.read_i32_le(),
                (SymbolAccessibility)reader.read_i32_le(),
                read_bool(ref reader),
                read_bool(ref reader),
                read_bool(ref reader),
                read_bool(ref reader),
                read_text_span(ref reader),
                read_source_span(ref reader),
                ByteBufferStringIO.read_nullable_string(ref reader));

        return snapshots;
    }

    private static Symbol[] create_symbols(IReadOnlyList<SymbolSnapshot> snapshots)
    {
        var symbols = new Symbol[snapshots.Count];
        for (var i = 0; i < snapshots.Count; i++)
        {
            var snapshot = snapshots[i];
            symbols[i] = new Symbol(
                snapshot.name,
                snapshot.kind,
                snapshot.accessibility,
                null,
                null,
                snapshot.is_static,
                snapshot.is_read_only,
                snapshot.is_abstract,
                snapshot.is_sealed,
                snapshot.definition_span,
                snapshot.definition_source_span,
                snapshot.file_path);
        }

        return symbols;
    }

    private static void attach_symbols_to_scopes(
        IReadOnlyList<Scope> scopes,
        IReadOnlyList<Symbol> symbols,
        IReadOnlyList<SymbolSnapshot> snapshots)
    {
        for (var i = 0; i < symbols.Count; i++)
        {
            var scopeId = snapshots[i].scope_id;
            if (scopeId < 0) continue;

            scopes[scopeId].define(symbols[i]);
        }
    }

    private static TypeSnapshot[] read_type_snapshots(ref ByteBuffer reader)
    {
        var count = reader.read_i32_le();
        var snapshots = new TypeSnapshot[count];
        for (var i = 0; i < count; i++)
        {
            var kind = reader.read_i32_le();
            snapshots[i] = kind switch
            {
                _type_kind_primitive => new TypeSnapshot(kind, ByteBufferStringIO.read_nullable_string(ref reader), null, reader.read_i32_le()),
                _type_kind_named => new TypeSnapshot(
                    kind,
                    ByteBufferStringIO.read_nullable_string(ref reader),
                    ByteBufferStringIO.read_nullable_string(ref reader),
                    reader.read_i32_le(),
                    read_index_list(ref reader),
                    read_index_list(ref reader)),
                _type_kind_function => read_function_type_snapshot(ref reader),
                _type_kind_generic => new TypeSnapshot(
                    kind,
                    ByteBufferStringIO.read_nullable_string(ref reader),
                    null,
                    -1,
                    read_index_list(ref reader),
                    []),
                _type_kind_array => new TypeSnapshot(kind, null, null, reader.read_i32_le()),
                _type_kind_nullable => new TypeSnapshot(kind, null, null, reader.read_i32_le()),
                _type_kind_row => new TypeSnapshot(
                    kind,
                    ByteBufferStringIO.read_nullable_string(ref reader),
                    null,
                    read_bool(ref reader) ? 1 : 0,
                    [],
                    read_index_list(ref reader)),
                _type_kind_type_variable => new TypeSnapshot(kind, ByteBufferStringIO.read_nullable_string(ref reader), null, reader.read_i32_le()),
                _type_kind_auto => new TypeSnapshot(kind),
                _type_kind_unknown => new TypeSnapshot(kind),
                _type_kind_error => new TypeSnapshot(kind),
                _ => throw new InvalidDataException($"未知的类型快照种类：{kind}")
            };
        }

        return snapshots;
    }

    private static IType[] create_type_shells(IReadOnlyList<TypeSnapshot> snapshots)
    {
        var types = new IType[snapshots.Count];
        for (var i = 0; i < snapshots.Count; i++)
        {
            var snapshot = snapshots[i];
            types[i] = snapshot.kind switch
            {
                _type_kind_primitive => new PrimitiveType(snapshot.name ?? string.Empty),
                _type_kind_named => new NamedType(snapshot.name ?? string.Empty, snapshot.tag ?? string.Empty),
                _type_kind_function => new FunctionType([], ErrorType.instance),
                _type_kind_generic => new GenericType(snapshot.name ?? string.Empty, []),
                _type_kind_array => new ArrayType(ErrorType.instance),
                _type_kind_nullable => new NullableType(ErrorType.instance),
                _type_kind_row => new RowType([], snapshot.scalar != 0, snapshot.name),
                _type_kind_type_variable => new TypeVariable(snapshot.name ?? string.Empty),
                _type_kind_auto => AutoType.instance,
                _type_kind_unknown => UnknownType.instance,
                _type_kind_error => ErrorType.instance,
                _ => throw new InvalidDataException($"未知的类型快照种类：{snapshot.kind}")
            };
        }

        return types;
    }

    private static void hydrate_types(
        IReadOnlyList<IType> types,
        IReadOnlyList<TypeSnapshot> snapshots,
        IReadOnlyList<Symbol> symbols)
    {
        for (var i = 0; i < types.Count; i++)
        {
            var snapshot = snapshots[i];
            switch (types[i])
            {
                case PrimitiveType primitiveType:
                    primitiveType.base_type = get_type_or_null(types, snapshot.scalar);
                    break;
                case NamedType namedType:
                    namedType.base_type = get_type_or_null(types, snapshot.scalar);
                    namedType.type_arguments = resolve_type_list(types, snapshot.type_indices);
                    namedType.members = resolve_symbol_list(symbols, snapshot.symbol_indices);
                    break;
                case FunctionType functionType:
                    functionType.parameter_types = resolve_type_list(types, snapshot.type_indices);
                    functionType.return_type = get_required_type(types, snapshot.scalar);
                    break;
                case GenericType genericType:
                    genericType.type_arguments = resolve_type_list(types, snapshot.type_indices);
                    break;
                case ArrayType arrayType:
                    arrayType.element_type = get_required_type(types, snapshot.scalar);
                    break;
                case NullableType nullableType:
                    nullableType.inner_type = get_required_type(types, snapshot.scalar);
                    break;
                case RowType rowType:
                    rowType.members = resolve_symbol_list(symbols, snapshot.symbol_indices);
                    break;
                case TypeVariable typeVariable:
                    if (snapshot.scalar >= 0) typeVariable.bind(get_required_type(types, snapshot.scalar));

                    break;
            }
        }
    }

    private static void attach_symbol_types(
        IReadOnlyList<Symbol> symbols,
        IReadOnlyList<SymbolSnapshot> snapshots,
        IReadOnlyList<IType> types)
    {
        for (var i = 0; i < symbols.Count; i++) symbols[i].type = get_type_or_null(types, snapshots[i].type_id);
    }

    private static void read_symbol_bindings(ref ByteBuffer reader, SemanticModel semanticModel, IReadOnlyList<Symbol> symbols)
    {
        read_binding_map(ref reader, id => semanticModel.bind_symbol(id.key, symbols[id.value]));
    }

    private static void read_type_bindings(ref ByteBuffer reader, SemanticModel semanticModel, IReadOnlyList<IType> types)
    {
        read_binding_map(ref reader, id => semanticModel.bind_type(id.key, types[id.value]));
    }

    private static void read_diagnostics(ref ByteBuffer reader, SemanticModel semanticModel)
    {
        var count = reader.read_i32_le();
        for (var i = 0; i < count; i++)
        {
            var diagnostic = new SemanticDiagnostic(
                (DiagnosticSeverity)reader.read_i32_le(),
                ByteBufferStringIO.read_nullable_string(ref reader) ?? string.Empty,
                new TextSpan(reader.read_i32_le(), reader.read_i32_le()),
                ByteBufferStringIO.read_nullable_string(ref reader),
                read_source_span(ref reader),
                ByteBufferStringIO.read_nullable_string(ref reader));
            semanticModel.add_diagnostic(diagnostic);
        }
    }

    private static void read_reference_buckets(ref ByteBuffer reader, SymbolTable symbolTable, IReadOnlyList<Symbol> symbols)
    {
        var count = reader.read_i32_le();
        for (var i = 0; i < count; i++)
        {
            var symbolName = ByteBufferStringIO.read_nullable_string(ref reader) ?? string.Empty;
            symbolTable.restore_reference_bucket(symbolName, resolve_indexed_list(symbols, read_index_list(ref reader)));
        }
    }

    #endregion

    #region 基础读写

    private static void write_type_id_list(ref ByteBufferWriter writer, SnapshotBuilder builder, IReadOnlyList<IType> types)
    {
        write_id_list(ref writer, types, builder.get_type_id);
    }

    private static void write_symbol_id_list(ref ByteBufferWriter writer, SnapshotBuilder builder, IReadOnlyList<ISymbol> symbols)
    {
        write_id_list(ref writer, symbols, builder.get_symbol_id);
    }

    private static void write_id_list<T>(ref ByteBufferWriter writer, IReadOnlyList<T> items, Func<T, int> idSelector)
    {
        writer.write_i32_le(items.Count);
        foreach (var item in items) writer.write_i32_le(idSelector(item));
    }

    private static void write_binding_map<T>(ref ByteBufferWriter writer, IReadOnlyDictionary<int, T> bindings, Func<T, int> idSelector)
    {
        writer.write_i32_le(bindings.Count);
        foreach (var binding in bindings)
        {
            writer.write_i32_le(binding.Key);
            writer.write_i32_le(idSelector(binding.Value));
        }
    }

    private static int[] read_index_list(ref ByteBuffer reader)
    {
        var count = reader.read_i32_le();
        var indices = new int[count];
        for (var i = 0; i < count; i++) indices[i] = reader.read_i32_le();

        return indices;
    }

    private static TypeSnapshot read_function_type_snapshot(ref ByteBuffer reader)
    {
        var parameterTypeIds = read_index_list(ref reader);
        var returnTypeId = reader.read_i32_le();
        return new TypeSnapshot(_type_kind_function, null, null, returnTypeId, parameterTypeIds, []);
    }

    private static void read_binding_map(ref ByteBuffer reader, Action<(int key, int value)> bindingReader)
    {
        var count = reader.read_i32_le();
        for (var i = 0; i < count; i++) bindingReader((reader.read_i32_le(), reader.read_i32_le()));
    }

    private static IType? get_type_or_null(IReadOnlyList<IType> types, int typeId)
    {
        return typeId < 0 ? null : types[typeId];
    }

    private static IType get_required_type(IReadOnlyList<IType> types, int typeId)
    {
        if (typeId < 0) throw new InvalidDataException("类型引用不能为空。");

        return types[typeId];
    }

    private static IReadOnlyList<IType> resolve_type_list(IReadOnlyList<IType> types, IReadOnlyList<int> indices)
    {
        return resolve_indexed_list(types, indices);
    }

    private static IReadOnlyList<ISymbol> resolve_symbol_list(IReadOnlyList<Symbol> symbols, IReadOnlyList<int> indices)
    {
        return resolve_indexed_list<ISymbol>(symbols, indices);
    }

    private static IReadOnlyList<T> resolve_indexed_list<T>(IReadOnlyList<T> items, IReadOnlyList<int> indices)
    {
        var resolved = new T[indices.Count];
        for (var i = 0; i < indices.Count; i++) resolved[i] = items[indices[i]];

        return resolved;
    }

    private static void write_source_span(ref ByteBufferWriter writer, SourceSpan sourceSpan)
    {
        ByteBufferStringIO.write_nullable_string(ref writer, sourceSpan.file_path);
        writer.write_i32_le(sourceSpan.start_line);
        writer.write_i32_le(sourceSpan.start_column);
        writer.write_i32_le(sourceSpan.end_line);
        writer.write_i32_le(sourceSpan.end_column);
    }

    private static SourceSpan read_source_span(ref ByteBuffer reader)
    {
        return new SourceSpan(
            ByteBufferStringIO.read_nullable_string(ref reader) ?? string.Empty,
            reader.read_i32_le(),
            reader.read_i32_le(),
            reader.read_i32_le(),
            reader.read_i32_le());
    }

    private static void write_text_span(ref ByteBufferWriter writer, TextSpan textSpan)
    {
        writer.write_i32_le(textSpan.start);
        writer.write_i32_le(textSpan.length);
    }

    private static TextSpan read_text_span(ref ByteBuffer reader)
    {
        return new TextSpan(reader.read_i32_le(), reader.read_i32_le());
    }

    private static void write_bool(ref ByteBufferWriter writer, bool value)
    {
        writer.write_u8(value ? (byte)1 : (byte)0);
    }

    private static bool read_bool(ref ByteBuffer reader)
    {
        return reader.read_u8() != 0;
    }

    #endregion

    #region 快照构建

    private sealed class SnapshotBuilder
    {
        private readonly Dictionary<Scope, int> _scope_ids;
        private readonly Dictionary<ISymbol, int> _symbol_ids;
        private readonly Dictionary<IType, int> _type_ids;

        private SnapshotBuilder()
        {
            _scope_ids = new Dictionary<Scope, int>(ReferenceIdentityComparer<Scope>.instance);
            _symbol_ids = new Dictionary<ISymbol, int>(ReferenceIdentityComparer<ISymbol>.instance);
            _type_ids = new Dictionary<IType, int>(ReferenceIdentityComparer<IType>.instance);
            scopes = [];
            symbols = [];
            types = [];
            symbol_bindings = new Dictionary<int, ISymbol>();
            type_bindings = new Dictionary<int, IType>();
            reference_buckets = new Dictionary<string, List<ISymbol>>(StringComparer.Ordinal);
        }

        public List<Scope> scopes { get; }
        public List<ISymbol> symbols { get; }
        public List<IType> types { get; }
        public Dictionary<int, ISymbol> symbol_bindings { get; }
        public Dictionary<int, IType> type_bindings { get; }
        public Dictionary<string, List<ISymbol>> reference_buckets { get; }

        public static SnapshotBuilder build(SemanticModel semanticModel)
        {
            if (semanticModel.symbols is not SymbolTable symbolTable) throw new InvalidOperationException("当前仅支持序列化基于 SymbolTable 的 SemanticModel。");

            if (symbolTable.global_scope is not Scope globalScope) throw new InvalidOperationException("当前仅支持序列化基于 Scope 的全局符号表。");

            var builder = new SnapshotBuilder();
            builder.collect_scope(globalScope);

            foreach (var binding in semanticModel.enumerate_symbol_bindings())
            {
                builder.symbol_bindings[binding.Key] = binding.Value;
                builder.collect_symbol(binding.Value);
            }

            foreach (var binding in semanticModel.enumerate_type_bindings())
            {
                builder.type_bindings[binding.Key] = binding.Value;
                builder.collect_type(binding.Value);
            }

            foreach (var bucket in symbolTable.enumerate_reference_buckets())
            {
                var list = new List<ISymbol>(bucket.Value.Count);
                foreach (var reference in bucket.Value)
                {
                    builder.collect_symbol(reference);
                    list.Add(reference);
                }

                builder.reference_buckets[bucket.Key] = list;
            }

            return builder;
        }

        public int get_scope_id(Scope? scope)
        {
            return scope is null ? -1 : _scope_ids[scope];
        }

        public int get_symbol_id(ISymbol symbol)
        {
            return _symbol_ids[symbol];
        }

        public int get_type_id(IType? type)
        {
            return type is null ? -1 : _type_ids[type];
        }

        private void collect_scope(Scope scope)
        {
            if (!_scope_ids.TryAdd(scope, scopes.Count)) return;

            scopes.Add(scope);

            foreach (var symbol in scope.get_members()) collect_symbol(symbol);

            foreach (var childScope in scope.get_child_scopes())
                if (childScope is Scope concreteChildScope)
                    collect_scope(concreteChildScope);
        }

        private void collect_symbol(ISymbol symbol)
        {
            if (!_symbol_ids.TryAdd(symbol, symbols.Count)) return;

            symbols.Add(symbol);

            if (symbol.containing_scope is Scope scope) collect_scope(scope);

            if (symbol.type is not null) collect_type(symbol.type);
        }

        private void collect_type(IType type)
        {
            if (!_type_ids.TryAdd(type, types.Count)) return;

            types.Add(type);

            switch (type)
            {
                case PrimitiveType primitiveType:
                    collect_optional_type(primitiveType.base_type);
                    break;
                case NamedType namedType:
                    collect_optional_type(namedType.base_type);
                    collect_type_list(namedType.type_arguments);
                    collect_symbol_list(namedType.members);
                    break;
                case FunctionType functionType:
                    collect_type_list(functionType.parameter_types);
                    collect_type(functionType.return_type);
                    break;
                case GenericType genericType:
                    collect_type_list(genericType.type_arguments);
                    break;
                case ArrayType arrayType:
                    collect_type(arrayType.element_type);
                    break;
                case NullableType nullableType:
                    collect_type(nullableType.inner_type);
                    break;
                case RowType rowType:
                    collect_symbol_list(rowType.members);
                    break;
                case TypeVariable typeVariable:
                    collect_optional_type(typeVariable.inferred_type);
                    break;
            }
        }

        private void collect_type_list(IReadOnlyList<IType> typesToCollect)
        {
            foreach (var type in typesToCollect) collect_type(type);
        }

        private void collect_symbol_list(IReadOnlyList<ISymbol> symbolsToCollect)
        {
            foreach (var symbol in symbolsToCollect) collect_symbol(symbol);
        }

        private void collect_optional_type(IType? type)
        {
            if (type is not null) collect_type(type);
        }
    }

    private sealed class ReferenceIdentityComparer<T> : IEqualityComparer<T> where T : class
    {
        public static ReferenceIdentityComparer<T> instance { get; } = new();

        public bool Equals(T? x, T? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

    private readonly record struct SymbolSnapshot(
        int scope_id,
        int type_id,
        string name,
        SymbolKind kind,
        SymbolAccessibility accessibility,
        bool is_static,
        bool is_read_only,
        bool is_abstract,
        bool is_sealed,
        TextSpan definition_span,
        SourceSpan definition_source_span,
        string? file_path);

    private readonly record struct TypeSnapshot(
        int kind,
        string? name = null,
        string? tag = null,
        int scalar = -1,
        int[]? type_indices = null,
        int[]? symbol_indices = null);

    #endregion
}