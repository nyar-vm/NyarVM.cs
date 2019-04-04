using Std.Data.Text.Syntax;

namespace Std.Data.Text.Typescript;


/// <summary>
///     TypeScript 词法节点类型
/// </summary>
public static class TsNodeKind
{
    #region 璇嶆硶节点

    public static readonly NodeKind eof = 0;
    public static readonly NodeKind identifier = 1;
    public static readonly NodeKind keyword = 2;
    public static readonly NodeKind number = 3;
    public static readonly NodeKind @string = 4;
    public static readonly NodeKind template_string = 5;
    public static readonly NodeKind @operator = 6;
    public static readonly NodeKind delimiter = 7;
    public static readonly NodeKind punctuation = 8;
    public static readonly NodeKind literal = 9;
    public static readonly NodeKind attribute = 10;
    public static readonly NodeKind comment = 11;
    public static readonly NodeKind jsx_text = 12;

    #endregion

    #region 语句节点

    
/// <summary>
    
///     鍧楄鍙?    

/// </summary>
    public static readonly NodeKind block_stmt = 100;

    
/// <summary>
    
///     if 鏉′欢语句
    

/// </summary>
    public static readonly NodeKind if_stmt = 101;

    
/// <summary>
    
///     for 寰幆语句
    

/// </summary>
    public static readonly NodeKind for_stmt = 102;

    
/// <summary>
    
///     while 寰幆语句
    

/// </summary>
    public static readonly NodeKind while_stmt = 103;

    
/// <summary>
    
///     return 语句
    

/// </summary>
    public static readonly NodeKind return_stmt = 104;

    
/// <summary>
    
///     琛ㄨ揪寮忚鍙?    

/// </summary>
    public static readonly NodeKind expr_stmt = 105;

    
/// <summary>
    
///     do-while 寰幆语句
    

/// </summary>
    public static readonly NodeKind do_while_stmt = 106;

    
/// <summary>
    
///     switch 语句
    

/// </summary>
    public static readonly NodeKind switch_stmt = 107;

    
/// <summary>
    
///     switch case 子句
    

/// </summary>
    public static readonly NodeKind switch_case = 108;

    
/// <summary>
    
///     try-catch-finally 语句
    

/// </summary>
    public static readonly NodeKind try_stmt = 109;

    
/// <summary>
    
///     catch 子句
    

/// </summary>
    public static readonly NodeKind catch_clause = 110;

    
/// <summary>
    
///     throw 语句
    

/// </summary>
    public static readonly NodeKind throw_stmt = 111;

    
/// <summary>
    
///     break 语句
    

/// </summary>
    public static readonly NodeKind break_stmt = 112;

    
/// <summary>
    
///     continue 语句
    

/// </summary>
    public static readonly NodeKind continue_stmt = 113;

    
/// <summary>
    
///     for-in 语句
    

/// </summary>
    public static readonly NodeKind for_in_stmt = 114;

    
/// <summary>
    
///     for-of 语句
    

/// </summary>
    public static readonly NodeKind for_of_stmt = 115;

    
/// <summary>
    
///     debugger 语句
    

/// </summary>
    public static readonly NodeKind debugger_stmt = 116;

    
/// <summary>
    
///     绌鸿鍙?    

/// </summary>
    public static readonly NodeKind empty_stmt = 117;

    
/// <summary>
    
///     鏍囩语句
    

/// </summary>
    public static readonly NodeKind labeled_stmt = 118;

    #endregion

    #region 琛ㄨ揪寮忚妭鐐?
    
/// <summary>
    
///     鏍囪瘑绗﹁〃杈惧紡
    

/// </summary>
    public static readonly NodeKind identifier_expr = 200;

    
/// <summary>
    
///     字面量表达式
    

/// </summary>
    public static readonly NodeKind literal_expr = 201;

    
/// <summary>
    
///     二元表达式    

/// </summary>
    public static readonly NodeKind binary_expr = 202;

    
/// <summary>
    
///     一元表达式
    

/// </summary>
    public static readonly NodeKind unary_expr = 203;

    
/// <summary>
    
///     赋值表达式
    

/// </summary>
    public static readonly NodeKind assignment_expr = 204;

    
/// <summary>
    
///     璋冪敤表达式    

/// </summary>
    public static readonly NodeKind call_expr = 205;

    
/// <summary>
    
///     鎴愬憳表达式    

/// </summary>
    public static readonly NodeKind member_expr = 206;

    
/// <summary>
    
///     灞炴€ц闂〃杈惧紡
    

/// </summary>
    public static readonly NodeKind property_access_expr = 207;

    
/// <summary>
    
///     元素访问表达式    

/// </summary>
    public static readonly NodeKind element_access_expr = 208;

    
/// <summary>
    
///     绠ご鍑芥暟表达式    

/// </summary>
    public static readonly NodeKind arrow_function_expr = 209;

    
/// <summary>
    
///     鏁扮粍字面量    

/// </summary>
    public static readonly NodeKind array_literal = 210;

    
/// <summary>
    
///     瀵硅薄字面量    

/// </summary>
    public static readonly NodeKind object_literal = 211;

    
/// <summary>
    
///     this 表达式    

/// </summary>
    public static readonly NodeKind this_expr = 212;

    
/// <summary>
    
///     super 表达式    

/// </summary>
    public static readonly NodeKind super_expr = 213;

    
/// <summary>
    
///     鍑芥暟表达式    

/// </summary>
    public static readonly NodeKind function_expr = 214;

    
/// <summary>
    
///     类型表达式
    

/// </summary>
    public static readonly NodeKind class_expr = 215;

    
/// <summary>
    
///     鏉′欢表达式    

/// </summary>
    public static readonly NodeKind conditional_expr = 216;

    
/// <summary>
    
///     new 表达式    

/// </summary>
    public static readonly NodeKind new_expr = 217;

    
/// <summary>
    
///     模板字面量插值    

/// </summary>
    public static readonly NodeKind template_literal = 218;

    
/// <summary>
    
///     展开元素
    

/// </summary>
    public static readonly NodeKind spread_element = 219;

    
/// <summary>
    
///     yield 表达式    

/// </summary>
    public static readonly NodeKind yield_expr = 220;

    
/// <summary>
    
///     typeof 表达式    

/// </summary>
    public static readonly NodeKind typeof_expr = 221;

    
/// <summary>
    
///     instanceof 表达式    

/// </summary>
    public static readonly NodeKind instanceof_expr = 222;

    #endregion

    #region 声明节点

    
/// <summary>
    
///     缂栬瘧鍗曞厓
    

/// </summary>
    public static readonly NodeKind compilation_unit = 300;

    
/// <summary>
    
///     瀵煎叆声明
    

/// </summary>
    public static readonly NodeKind import_decl = 301;

    
/// <summary>
    
///     瀵煎嚭声明
    

/// </summary>
    public static readonly NodeKind export_decl = 302;

    
/// <summary>
    
///     变量声明
    

/// </summary>
    public static readonly NodeKind variable_decl = 303;

    
/// <summary>
    
///     鍑芥暟声明
    

/// </summary>
    public static readonly NodeKind function_decl = 304;

    
/// <summary>
    
///     类声明    

/// </summary>
    public static readonly NodeKind class_decl = 305;

    
/// <summary>
    
///     接口声明
    

/// </summary>
    public static readonly NodeKind interface_decl = 306;

    
/// <summary>
    
///     绫诲瀷鍒悕声明
    

/// </summary>
    public static readonly NodeKind type_alias_decl = 307;

    
/// <summary>
    
///     鏋氫妇声明
    

/// </summary>
    public static readonly NodeKind enum_decl = 308;

    
/// <summary>
    
///     鏋氫妇鎴愬憳
    

/// </summary>
    public static readonly NodeKind enum_member = 309;

    
/// <summary>
    
///     命名空间声明
    

/// </summary>
    public static readonly NodeKind namespace_decl = 310;

    #endregion

    #region 类型节点

    
/// <summary>
    
///     类型注解
    

/// </summary>
    public static readonly NodeKind type_annotation = 400;

    
/// <summary>
    
///     鍘熷绫诲瀷
    

/// </summary>
    public static readonly NodeKind primitive_type = 401;

    
/// <summary>
    
///     联合类型
    

/// </summary>
    public static readonly NodeKind union_type = 402;

    
/// <summary>
    
///     数组类型
    

/// </summary>
    public static readonly NodeKind array_type = 403;

    #endregion

    #region 閫氱敤节点

    
/// <summary>
    
///     鍑芥暟鍙傛暟
    

/// </summary>
    public static readonly NodeKind parameter = 500;

    
/// <summary>
    
///     瀵硅薄灞炴€?    

/// </summary>
    public static readonly NodeKind property = 501;

    #endregion
}

