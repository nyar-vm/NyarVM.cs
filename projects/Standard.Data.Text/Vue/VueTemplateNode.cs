namespace Std.Data.Text.Vue;


/// <summary>

///     Vue SFC template 块


/// </summary>
public sealed class VueTemplateBlock
{
    
/// <summary>
    
///     模板根节点列表
    

/// </summary>
    public IReadOnlyList<VueTemplateNode> Children { get; init; } = [];
}


/// <summary>

///     Vue 模板节点基类


/// </summary>
public abstract class VueTemplateNode;


/// <summary>

///     文本节点


/// </summary>
public sealed class VueTextNode : VueTemplateNode
{
    
/// <summary>
    
///     文本内容
    

/// </summary>
    public string Text { get; init; } = string.Empty;

    
/// <summary>
    
///     是否包含插值表达式（{{ ... }}）
    

/// </summary>
    public bool HasInterpolation { get; init; }
}


/// <summary>

///     元素节点


/// </summary>
public sealed class VueElementNode : VueTemplateNode
{
    
/// <summary>
    
///     标签名
    

/// </summary>
    public string TagName { get; init; } = string.Empty;

    
/// <summary>
    
///     属性列表（含 Vue 指令）
    

/// </summary>
    public IReadOnlyList<VueAttribute> Attributes { get; init; } = [];

    
/// <summary>
    
///     子节点
    

/// </summary>
    public IReadOnlyList<VueTemplateNode> Children { get; init; } = [];

    
/// <summary>
    
///     是否自闭合标签
    

/// </summary>
    public bool IsSelfClosing { get; init; }
}


/// <summary>

///     Vue 指令节点（v-if / v-for / v-slot 等）


/// </summary>
public sealed class VueDirectiveNode : VueTemplateNode
{
    
/// <summary>
    
///     指令名称（if / for / else-if / else / slot / model 等）
    

/// </summary>
    public string DirectiveName { get; init; } = string.Empty;

    
/// <summary>
    
///     指令参数（v-slot:name 中的 name，v-model:arg 中的 arg）
    

/// </summary>
    public string? Argument { get; init; }

    
/// <summary>
    
///     指令修饰符（v-model.trim 中的 trim）
    

/// </summary>
    public IReadOnlyList<string> Modifiers { get; init; } = [];

    
/// <summary>
    
///     指令表达式
    

/// </summary>
    public string Expression { get; init; } = string.Empty;

    
/// <summary>
    
///     子节点
    

/// </summary>
    public IReadOnlyList<VueTemplateNode> Children { get; init; } = [];
}


/// <summary>

///     Vue 属性（含指令）


/// </summary>
public sealed class VueAttribute
{
    
/// <summary>
    
///     属性名
    

/// </summary>
    public string Name { get; init; } = string.Empty;

    
/// <summary>
    
///     属性值
    

/// </summary>
    public string? Value { get; init; }

    
/// <summary>
    
///     属性种类
    

/// </summary>
    public VueAttributeKind Kind { get; init; }
}


/// <summary>

///     Vue 属性种类


/// </summary>
public enum VueAttributeKind
{
    
/// <summary>
    
///     普通 HTML 属性
    

/// </summary>
    Plain,

    
/// <summary>
    
///     动态绑定（:attr / v-bind:attr）
    

/// </summary>
    Bind,

    
/// <summary>
    
///     事件绑定（@event / v-on:event）
    

/// </summary>
    On,

    
/// <summary>
    
///     双向绑定（v-model / v-model:arg）
    

/// </summary>
    Model,

    
/// <summary>
    
///     插槽（#name / v-slot:name）
    

/// </summary>
    Slot,

    
/// <summary>
    
///     指令（v-if / v-for / v-show / v-html / v-text / v-once / v-pre / v-cloak / v-memo）
    

/// </summary>
    Directive
}
