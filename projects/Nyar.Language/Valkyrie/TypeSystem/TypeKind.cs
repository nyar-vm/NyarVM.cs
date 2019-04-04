namespace Nyar.Language.Valkyrie.TypeSystem;

/// <summary>
///     类型种类枚举，定义所有可能的类型分类
/// </summary>
public enum TypeKind
{
    /// <summary>原始类型（i8~i64, u8~u64, f32, f64, bool, char, unit）</summary>
    primitive,

    /// <summary>类型变量（泛型推断用）</summary>
    type_variable,

    /// <summary>泛型类型（用户自定义泛型）</summary>
    generic,

    /// <summary>函数类型</summary>
    function,

    /// <summary>ECS 组件</summary>
    component,

    /// <summary>ECS 系统</summary>
    system,

    /// <summary>Widget</summary>
    widget,

    /// <summary>Plugin</summary>
    plugin,

    /// <summary>结构体</summary>
    @struct,

    /// <summary>类</summary>
    @class,

    /// <summary>枚举</summary>
    @enum,

    /// <summary>联合类型（代数数据类型）</summary>
    union,

    /// <summary>交集类型</summary>
    intersection,

    /// <summary>数组/Array</summary>
    array,

    /// <summary>定长数组/FixedArray</summary>
    fixed_array,

    /// <summary>映射</summary>
    map,

    /// <summary>可空类型</summary>
    nullable,

    /// <summary>着色器</summary>
    shader,

    /// <summary>Trait（特征/接口）</summary>
    trait,

    /// <summary>元组类型</summary>
    tuple,

    /// <summary>错误类型</summary>
    error,

    /// <summary>未知类型（含 auto）</summary>
    unknown
}