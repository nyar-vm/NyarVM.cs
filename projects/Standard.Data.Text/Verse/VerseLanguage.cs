namespace Std.Data.Text.Verse;


/// <summary>

///     Verse 语言配置


/// </summary>
public sealed class VerseLanguage : Language
{
    public override string Name => "Verse";

    
/// <summary>
    
///     文件扩展名列表
    

/// </summary>
    public IReadOnlyList<string> Extensions { get; init; } = [".story"];

    
/// <summary>
    
///     是否支持条件分支扩展
    

/// </summary>
    public bool SupportConditionalBranch { get; init; } = true;

    
/// <summary>
    
///     是否支持变量扩展
    

/// </summary>
    public bool SupportVariable { get; init; } = true;

    
/// <summary>
    
///     是否支持命令扩展
    

/// </summary>
    public bool SupportCommand { get; init; } = true;

    
/// <summary>
    
///     标准 Verse 语言（Galgame 剧本模式）
    

/// </summary>
    public static VerseLanguage Standard => new()
    {
        Extensions = [".story"],
        SupportConditionalBranch = true,
        SupportVariable = true,
        SupportCommand = true
    };
}