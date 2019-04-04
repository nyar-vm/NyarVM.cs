using Nyar.Analyzer.Semantic;

namespace Nyar.Language.Von.Semantic;

/// <summary>
///     `VON` 专用的字面量语义类型。
///     这套模型只描述 `VON` 自身的字面量形状，不直接复用 `Valkyrie` 的值类型命名。
/// </summary>
public sealed class VonLiteralType : IType
{
    private VonLiteralType(string name, VonLiteralKind kind, IReadOnlyList<IType>? typeArguments = null)
    {
        this.name = name;
        this.kind = kind;
        type_arguments = typeArguments ?? [];
    }

    public VonLiteralKind kind { get; }
    public string name { get; }
    public IType? base_type => null;
    public IReadOnlyList<IType> type_arguments { get; }
    public IReadOnlyList<ISymbol> members => [];

    public static VonLiteralType boolean { get; } = new("von.bool_literal", VonLiteralKind.boolean);
    public static VonLiteralType number { get; } = new("von.number_literal", VonLiteralKind.number);
    public static VonLiteralType text { get; } = new("von.text_literal", VonLiteralKind.text);
    public static VonLiteralType @null { get; } = new("von.null_literal", VonLiteralKind.@null);

    public static VonLiteralType create_array(IType elementType)
    {
        return new VonLiteralType("von.array_literal", VonLiteralKind.array, [elementType]);
    }

    public static VonLiteralType create_object(IType keyType, IType valueType)
    {
        return new VonLiteralType("von.object_literal", VonLiteralKind.@object, [keyType, valueType]);
    }

    public bool is_assignable_from(IType other)
    {
        return equals(other);
    }

    public bool is_assignable_to(IType other)
    {
        return other.is_assignable_from(this);
    }

    public bool is_subtype_of(IType other)
    {
        return equals(other);
    }

    public bool equals(IType? other)
    {
        if (other is not VonLiteralType literalType || literalType.kind != kind
                                                || literalType.type_arguments.Count != type_arguments.Count)
        {
            return false;
        }

        for (var i = 0; i < type_arguments.Count; i++)
        {
            if (!type_arguments[i].equals(literalType.type_arguments[i]))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
///     `VON` 字面量语义类型的分类标签。
/// </summary>
public enum VonLiteralKind
{
    boolean,
    number,
    text,
    array,
    @object,
    @null
}
