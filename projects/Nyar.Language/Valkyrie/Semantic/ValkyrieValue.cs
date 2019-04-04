using Nyar.Analyzer.Semantic;

namespace Nyar.Language.Valkyrie.Semantic;

/// <summary>
///     `Valkyrie` 自身的值模型基类。
///     这里承载的是 literal 经由 expected type 绑定后的 value 语义，而不是 `Serde` 的中性字面量树。
/// </summary>
public abstract record ValkyrieValue(IType type);

public sealed record ValkyrieNullValue(IType type) : ValkyrieValue(type);

public sealed record ValkyrieBoolValue(IType type, bool value) : ValkyrieValue(type);

public sealed record ValkyrieSignedIntegerValue(IType type, long value) : ValkyrieValue(type);

public sealed record ValkyrieUnsignedIntegerValue(IType type, ulong value) : ValkyrieValue(type);

public sealed record ValkyrieFloatValue(IType type, double value) : ValkyrieValue(type);

public sealed record ValkyrieCharValue(IType type, char value) : ValkyrieValue(type);

public sealed record ValkyrieTextValue(IType type, string value) : ValkyrieValue(type);

public sealed record ValkyrieArrayValue(IType type, IReadOnlyList<ValkyrieValue> elements) : ValkyrieValue(type);

public sealed record ValkyrieMapEntry(ValkyrieValue key, ValkyrieValue value);

public sealed record ValkyrieMapValue(IType type, IReadOnlyList<ValkyrieMapEntry> entries) : ValkyrieValue(type);

public sealed record ValkyrieObjectField(string name, IType field_type, ValkyrieValue value, ISymbol? symbol = null);

public sealed record ValkyrieObjectValue(IType type, IReadOnlyList<ValkyrieObjectField> fields) : ValkyrieValue(type);
