namespace Std.Data.Text.Typescript.Lexer;


/// <summary>

///     TypeScript 词法单元类型


/// </summary>
public enum TsTokenType
{
    
/// <summary>
    
///     文件结束
    

/// </summary>
    eof,

    
/// <summary>
    
///     标识符
    

/// </summary>
    identifier,

    
/// <summary>
    
///     关键字
    

/// </summary>
    keyword,

    
/// <summary>
    
///     数字字面量
    

/// </summary>
    number,

    
/// <summary>
    
///     BigInt 字面量
    

/// </summary>
    big_int,

    
/// <summary>
    
///     字符串字面量
    

/// </summary>
    @string,

    
/// <summary>
    
///     模板字符串字面量
    

/// </summary>
    template_string,

    
/// <summary>
    
///     运算符
    

/// </summary>
    @operator,

    
/// <summary>
    
///     分隔符
    

/// </summary>
    delimiter,

    
/// <summary>
    
///     标点符号
    

/// </summary>
    punctuation,

    
/// <summary>
    
///     字面量（true/false/null）
    

/// </summary>
    literal,

    
/// <summary>
    
///     属性装饰器
    

/// </summary>
    attribute,

    
/// <summary>
    
///     注释
    

/// </summary>
    comment,

    
/// <summary>
    
///     JSX 文本
    

/// </summary>
    jsx_text
}

