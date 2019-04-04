namespace Nyar.Language.Valkyrie.TypeSystem;

/// <summary>
///     泛型约束种类，定义类型参数可施加的约束类型
/// </summary>
public enum ConstraintKind
{
    /// <summary>Trait 约束：where T : TraitName</summary>
    trait,

    /// <summary>类约束：where T : class</summary>
    @class,

    /// <summary>结构体约束：where T : struct</summary>
    @struct,

    /// <summary>构造函数约束：where T : new()</summary>
    @new,

    /// <summary>枚举约束：where T : enum</summary>
    @enum,

    /// <summary>数值约束：where T : Numeric</summary>
    numeric,

    /// <summary>整数约束：where T : Integer</summary>
    integer,

    /// <summary>浮点约束：where T : Float</summary>
    @float,

    /// <summary>可空约束：where T : nullable</summary>
    nullable
}