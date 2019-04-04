namespace Std.Data.Text.Vue;


/// <summary>

///     Vue SFC script 块


/// </summary>
public sealed class VueScriptBlock
{
    
/// <summary>
    
///     script 块内容
    

/// </summary>
    public string Content { get; init; } = string.Empty;

    
/// <summary>
    
///     是否为 setup 语法糖（&lt;script setup&gt;）
    

/// </summary>
    public bool IsSetup { get; init; }

    
/// <summary>
    
///     语言标识（ts /tsx /jsx 等）
    

/// </summary>
    public string Lang { get; init; } = "js";

    
/// <summary>
    
///     从 defineProps 提取的属性列表
    

/// </summary>
    public IReadOnlyList<VuePropDef> Props { get; init; } = [];

    
/// <summary>
    
///     从 defineEmits 提取的事件列表
    

/// </summary>
    public IReadOnlyList<string> Emits { get; init; } = [];

    
/// <summary>
    
///     从 ref/reactive 提取的响应式变量
    

/// </summary>
    public IReadOnlyList<VueReactiveVar> ReactiveVars { get; init; } = [];

    
/// <summary>
    
///     从 defineExpose 提取的暴露成员
    

/// </summary>
    public IReadOnlyList<string> ExposedMembers { get; init; } = [];
}


/// <summary>

///     Vue 属性定义（从 defineProps 提取）


/// </summary>
public sealed class VuePropDef
{
    
/// <summary>
    
///     属性名
    

/// </summary>
    public string Name { get; init; } = string.Empty;

    
/// <summary>
    
///     类型名（String / Number / Boolean / Array / Object / Function 等）
    

/// </summary>
    public string TypeName { get; init; } = "any";

    
/// <summary>
    
///     是否必填
    

/// </summary>
    public bool Required { get; init; }

    
/// <summary>
    
///     默认值
    

/// </summary>
    public string? DefaultValue { get; init; }

    
/// <summary>
    
///     验证器表达式
    

/// </summary>
    public string? Validator { get; init; }
}


/// <summary>

///     Vue 响应式变量（从 ref / reactive / computed 提取）


/// </summary>
public sealed class VueReactiveVar
{
    
/// <summary>
    
///     变量名
    

/// </summary>
    public string Name { get; init; } = string.Empty;

    
/// <summary>
    
///     响应式种类（ref / reactive / computed）
    

/// </summary>
    public VueReactiveKind Kind { get; init; }

    
/// <summary>
    
///     初始值表达式
    

/// </summary>
    public string? Initializer { get; init; }
}


/// <summary>

///     Vue 响应式变量种类


/// </summary>
public enum VueReactiveKind
{
    Ref,
    Reactive,
    Computed
}
