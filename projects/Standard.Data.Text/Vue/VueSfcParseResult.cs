namespace Std.Data.Text.Vue;


/// <summary>

///     Vue SFC 解析结果


/// </summary>
public sealed class VueSfcParseResult
{
    
/// <summary>
    
///     组件名称
    

/// </summary>
    public string Name { get; init; } = string.Empty;

    
/// <summary>
    
///     script 块内容
    

/// </summary>
    public VueScriptBlock? Script { get; init; }

    
/// <summary>
    
///     template 块内容
    

/// </summary>
    public VueTemplateBlock? Template { get; init; }

    
/// <summary>
    
///     style 块列表（Vue SFC 支持多个 style 块）
    

/// </summary>
    public IReadOnlyList<VueStyleBlock> Styles { get; init; } = [];
}
