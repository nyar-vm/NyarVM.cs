namespace Std.Data.Text.Vue;


/// <summary>

///     Vue SFC style 块


/// </summary>
public sealed class VueStyleBlock
{
    
/// <summary>
    
///     样式内容
    

/// </summary>
    public string Content { get; init; } = string.Empty;

    
/// <summary>
    
///     语言标识（css / scss / less / stylus 等）
    

/// </summary>
    public string Lang { get; init; } = "css";

    
/// <summary>
    
///     是否 scoped
    

/// </summary>
    public bool Scoped { get; init; }

    
/// <summary>
    
///     是否 module
    

/// </summary>
    public bool Module { get; init; }

    
/// <summary>
    
///     解析后的 CSS 规则列表
    

/// </summary>
    public IReadOnlyList<VueCssRule> Rules { get; init; } = [];
}


/// <summary>

///     CSS 规则


/// </summary>
public sealed class VueCssRule
{
    
/// <summary>
    
///     选择器
    

/// </summary>
    public string Selector { get; init; } = string.Empty;

    
/// <summary>
    
///     属性列表
    

/// </summary>
    public IReadOnlyList<VueCssProperty> Properties { get; init; } = [];
}


/// <summary>

///     CSS 属性


/// </summary>
public sealed class VueCssProperty
{
    
/// <summary>
    
///     属性名
    

/// </summary>
    public string Name { get; init; } = string.Empty;

    
/// <summary>
    
///     属性值
    

/// </summary>
    public string Value { get; init; } = string.Empty;
}
