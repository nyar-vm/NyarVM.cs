namespace Nyar.Language.Valkyrie.TypeSystem;

/// <summary>
///     泛型约束描述，表示类型参数上的单个约束
/// </summary>
public sealed class GenericConstraintDescriptor
{
    public GenericConstraintDescriptor(ConstraintKind kind, string? targetTypeName = null,
        ValkyrieType? targetType = null)
    {
        this.kind = kind;
        target_type_name = targetTypeName;
        target_type = targetType;
    }

    /// <summary>约束种类</summary>
    public ConstraintKind kind { get; }

    /// <summary>约束目标类型名（Trait 约束时为 trait 名称）</summary>
    public string? target_type_name { get; }

    /// <summary>约束目标类型（解析后的 ValkyrieType，Trait 约束时有效）</summary>
    public ValkyrieType? target_type { get; }

    /// <summary>
    ///     从约束类型名推断约束种类
    /// </summary>
    public static GenericConstraintDescriptor from_name(string name, ValkyrieType? resolvedType = null)
    {
        return name switch
        {
            "class" => new GenericConstraintDescriptor(ConstraintKind.@class),
            "struct" => new GenericConstraintDescriptor(ConstraintKind.@struct),
            "new" or "new()" => new GenericConstraintDescriptor(ConstraintKind.@new),
            "enum" => new GenericConstraintDescriptor(ConstraintKind.@enum),
            "Numeric" => new GenericConstraintDescriptor(ConstraintKind.numeric, name, resolvedType),
            "Integer" => new GenericConstraintDescriptor(ConstraintKind.integer, name, resolvedType),
            "Float" => new GenericConstraintDescriptor(ConstraintKind.@float, name, resolvedType),
            "nullable" => new GenericConstraintDescriptor(ConstraintKind.nullable),
            _ => new GenericConstraintDescriptor(ConstraintKind.trait, name, resolvedType)
        };
    }

    public override string ToString()
    {
        return kind switch
        {
            ConstraintKind.trait => target_type_name ?? "trait",
            ConstraintKind.@class => "class",
            ConstraintKind.@struct => "struct",
            ConstraintKind.@new => "new()",
            ConstraintKind.@enum => "enum",
            ConstraintKind.numeric => "Numeric",
            ConstraintKind.integer => "Integer",
            ConstraintKind.@float => "Float",
            ConstraintKind.nullable => "nullable",
            _ => kind.ToString()
        };
    }
}