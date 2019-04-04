using Std.Data.Text.Syntax;

namespace Std.Data.Text.Python.Syntax;


/// <summary>

///     Python 节点类型


/// </summary>
public static class PythonNodeKind
{
    
/// <summary>
    
///     未知
    

/// </summary>
    public static readonly NodeKind unknown = 0;

    
/// <summary>
    
///     鏍囪瘑绗?    

/// </summary>
    public static readonly NodeKind identifier = 1;

    
/// <summary>
    
///     关键字    

/// </summary>
    public static readonly NodeKind keyword = 2;

    
/// <summary>
    
///     数字字面量    

/// </summary>
    public static readonly NodeKind number = 3;

    
/// <summary>
    
///     字符串字面量
    

/// </summary>
    public static readonly NodeKind @string = 4;

    
/// <summary>
    
///     杩愮畻绗?    

/// </summary>
    public static readonly NodeKind @operator = 5;

    
/// <summary>
    
///     分隔符（括号、逗号、冒号等）    

/// </summary>
    public static readonly NodeKind delimiter = 6;

    
/// <summary>
    
///     换行
    

/// </summary>
    public static readonly NodeKind new_line = 7;

    
/// <summary>
    
///     缩进
    

/// </summary>
    public static readonly NodeKind indent = 8;

    
/// <summary>
    
///     鍙嶇缉杩?    

/// </summary>
    public static readonly NodeKind dedent = 9;

    
/// <summary>
    
///     文件结束
    

/// </summary>
    public static readonly NodeKind eof = 10;
}

