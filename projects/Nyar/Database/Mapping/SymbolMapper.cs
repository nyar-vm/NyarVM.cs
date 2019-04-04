using Nyar.Database.Index;
using ISymbol = Nyar.Analyzer.Semantic.ISymbol;
using Semantic_SymbolAccessibility = Nyar.Analyzer.Semantic.SymbolAccessibility;
using Semantic_SymbolKind = Nyar.Analyzer.Semantic.SymbolKind;
using SymbolStub = Nyar.Analyzer.Semantic.SymbolStub;

namespace Nyar.Database.Mapping;

/// <summary>
///     符号映射器，桥接 Nyar.Semantic 和 Nyar.Database 两套符号体系
/// </summary>
public static class SymbolMapper
{
    private static readonly Dictionary<Semantic_SymbolKind, SymbolKind> _kind_map = new()
    {
        { Semantic_SymbolKind.@namespace, SymbolKind.@namespace },
        { Semantic_SymbolKind.module, SymbolKind.module },
        { Semantic_SymbolKind.@class, SymbolKind.@class },
        { Semantic_SymbolKind.@interface, SymbolKind.@interface },
        { Semantic_SymbolKind.@enum, SymbolKind.@enum },
        { Semantic_SymbolKind.function, SymbolKind.function },
        { Semantic_SymbolKind.method, SymbolKind.method },
        { Semantic_SymbolKind.property, SymbolKind.property },
        { Semantic_SymbolKind.field, SymbolKind.field },
        { Semantic_SymbolKind.variable, SymbolKind.variable },
        { Semantic_SymbolKind.parameter, SymbolKind.parameter },
        { Semantic_SymbolKind.type_parameter, SymbolKind.type_parameter },
        { Semantic_SymbolKind.type_alias, SymbolKind.type_alias },
        { Semantic_SymbolKind.import, SymbolKind.import },
        { Semantic_SymbolKind.export, SymbolKind.export },
        { Semantic_SymbolKind.constant, SymbolKind.variable },
        { Semantic_SymbolKind.constructor, SymbolKind.method },
        { Semantic_SymbolKind.destructor, SymbolKind.method },
        { Semantic_SymbolKind.@operator, SymbolKind.function },
        { Semantic_SymbolKind.@event, SymbolKind.property },
        { Semantic_SymbolKind.@delegate, SymbolKind.function }
    };

    private static readonly Dictionary<Semantic_SymbolAccessibility, SymbolAccessibility> _accessibility_map = new()
    {
        { Semantic_SymbolAccessibility.@public, SymbolAccessibility.@public },
        { Semantic_SymbolAccessibility.@private, SymbolAccessibility.@private },
        { Semantic_SymbolAccessibility.@protected, SymbolAccessibility.@protected },
        { Semantic_SymbolAccessibility.@internal, SymbolAccessibility.@internal },
        { Semantic_SymbolAccessibility.protected_internal, SymbolAccessibility.@internal },
        { Semantic_SymbolAccessibility.private_protected, SymbolAccessibility.@private }
    };

    /// <summary>
    ///     将 Nyar.Semantic.SymbolKind 映射为 Nyar.Database.SymbolKind
    /// </summary>
    public static SymbolKind map_kind(Semantic_SymbolKind kind)
    {
        return _kind_map.GetValueOrDefault(kind, SymbolKind.unknown);
    }

    /// <summary>
    ///     将 Nyar.Semantic.SymbolAccessibility 映射为 Nyar.Database.Index.SymbolAccessibility
    /// </summary>
    public static SymbolAccessibility map_accessibility(Semantic_SymbolAccessibility accessibility)
    {
        return _accessibility_map.GetValueOrDefault(accessibility, SymbolAccessibility.unknown);
    }

    /// <summary>
    ///     将 ISymbol 转换为 SymbolRecord，用于持久化
    /// </summary>
    /// <param name="symbol">语义符号。</param>
    /// <param name="fileUri">符号所在文件的 URI。</param>
    /// <param name="line">行号（从 0 开始）。</param>
    /// <param name="column">列号（从 0 开始）。</param>
    public static SymbolRecord to_record(ISymbol symbol, string fileUri, int line = 0, int column = 0)
    {
        var dbKind = map_kind(symbol.kind);
        var dbAccessibility = map_accessibility(symbol.accessibility);

        return new SymbolRecord
        {
            id = SymbolId.create(fileUri, symbol.name, dbKind),
            name = symbol.name,
            kind = dbKind,
            file_uri = fileUri,
            location = new Loc(line, column),
            accessibility = dbAccessibility,
            metadata = build_metadata(symbol)
        };
    }

    /// <summary>
    ///     将 SymbolStub 转换为 SymbolRecord，用于持久化
    /// </summary>
    /// <param name="stub">符号桩。</param>
    /// <param name="fileUri">符号所在文件的 URI。</param>
    /// <param name="spanStart">符号在文件中的起始位置。</param>
    public static SymbolRecord to_record(SymbolStub stub, string fileUri, int spanStart = 0)
    {
        var dbKind = map_kind(stub.kind);
        var dbAccessibility = map_accessibility(stub.accessibility);

        return new SymbolRecord
        {
            id = SymbolId.create(fileUri, stub.name, dbKind),
            name = stub.name,
            kind = dbKind,
            file_uri = fileUri,
            location = new Loc(spanStart, 0),
            accessibility = dbAccessibility,
            metadata = stub.type_name
        };
    }

    /// <summary>
    ///     批量将 ISymbol 列表转换为 SymbolRecord 列表
    /// </summary>
    public static IReadOnlyList<SymbolRecord> to_records(IEnumerable<ISymbol> symbols, string fileUri,
        int line = 0, int column = 0)
    {
        var records = new List<SymbolRecord>();
        foreach (var symbol in symbols) records.Add(to_record(symbol, fileUri, line, column));

        return records;
    }

    /// <summary>
    ///     批量将 SymbolStub 列表转换为 SymbolRecord 列表
    /// </summary>
    public static IReadOnlyList<SymbolRecord> to_records(IEnumerable<SymbolStub> stubs, string fileUri,
        Func<SymbolStub, int>? spanStartProvider = null)
    {
        var records = new List<SymbolRecord>();
        foreach (var stub in stubs)
        {
            var spanStart = spanStartProvider?.Invoke(stub) ?? 0;
            records.Add(to_record(stub, fileUri, spanStart));
        }

        return records;
    }

    private static string? build_metadata(ISymbol symbol)
    {
        var parts = new List<string>();
        if (symbol.is_static) parts.Add("static");

        if (symbol.is_read_only) parts.Add("readonly");

        if (symbol.is_abstract) parts.Add("abstract");

        if (symbol.is_sealed) parts.Add("sealed");

        if (symbol.type is not null) parts.Add($"type:{symbol.type.name}");

        return parts.Count > 0 ? string.Join(",", parts) : null;
    }
}