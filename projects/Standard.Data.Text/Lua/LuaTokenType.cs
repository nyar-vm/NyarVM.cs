namespace Std.Data.Text.Lua;

/// <summary>
///     Lua 词法分析器 Token 类型。
/// </summary>
public enum LuaTokenType
{
    /// <summary>
    ///     名称标识符。
    /// </summary>
    name,


    /// <summary>
    ///     整数。
    /// </summary>
    integer,


    /// <summary>
    ///     浮点数。
    /// </summary>
    @float,


    /// <summary>
    ///     字符串。
    /// </summary>
    @string,


    /// <summary>
    ///     长字符串（[[...]]）。
    /// </summary>
    long_string,


    /// <summary>
    ///     注释。
    ///     。
    /// </summary>
    comment,


    /// <summary>
    ///     长注释（--[[...]]）。
    ///     。
    /// </summary>
    long_comment,


    /// <summary>
    ///     关键字 and。
    ///     。
    /// </summary>
    and,


    /// <summary>
    ///     关键字 break。
    ///     。
    /// </summary>
    @break,


    /// <summary>
    ///     关键字 do。
    ///     。
    /// </summary>
    @do,


    /// <summary>
    ///     关键字 else。
    ///     。
    /// </summary>
    @else,


    /// <summary>
    ///     关键字 elseif。
    ///     。
    /// </summary>
    else_if,


    /// <summary>
    ///     关键字 end。
    ///     。
    /// </summary>
    end,


    /// <summary>
    ///     关键字 false。
    ///     。
    /// </summary>
    @false,


    /// <summary>
    ///     关键字 for。
    ///     。
    /// </summary>
    @for,


    /// <summary>
    ///     关键字 function。
    ///     。
    /// </summary>
    function,


    /// <summary>
    ///     关键字 goto。
    ///     。
    /// </summary>
    @goto,


    /// <summary>
    ///     关键字 if。
    ///     。
    /// </summary>
    @if,


    /// <summary>
    ///     关键字 in。
    ///     。
    /// </summary>
    @in,


    /// <summary>
    ///     关键字 local。
    ///     。
    /// </summary>
    local,


    /// <summary>
    ///     关键字 nil。
    ///     。
    /// </summary>
    nil,


    /// <summary>
    ///     关键字 not。
    ///     。
    /// </summary>
    not,


    /// <summary>
    ///     关键字 or。
    ///     。
    /// </summary>
    or,


    /// <summary>
    ///     关键字 repeat。
    ///     。
    /// </summary>
    repeat,


    /// <summary>
    ///     关键字 return。
    ///     。
    /// </summary>
    @return,


    /// <summary>
    ///     关键字 then。
    ///     。
    /// </summary>
    then,


    /// <summary>
    ///     关键字 true。
    ///     。
    /// </summary>
    @true,


    /// <summary>
    ///     关键字 until。
    ///     。
    /// </summary>
    until,


    /// <summary>
    ///     关键字 while。
    ///     。
    /// </summary>
    @while,


    /// <summary>
    ///     加号。
    ///     。
    /// </summary>
    plus,


    /// <summary>
    ///     减号。
    ///     。
    /// </summary>
    minus,


    /// <summary>
    ///     星号。
    ///     。
    /// </summary>
    star,


    /// <summary>
    ///     斜杠。
    ///     。
    /// </summary>
    slash,


    /// <summary>
    ///     百分号。
    ///     。
    /// </summary>
    percent,


    /// <summary>
    ///     脱字号。
    ///     。
    /// </summary>
    caret,


    /// <summary>
    ///     井号。
    ///     。
    /// </summary>
    hash,


    /// <summary>
    ///     等于。
    ///     。
    /// </summary>
    equal_equal,


    /// <summary>
    ///     不等于。
    ///     。
    /// </summary>
    tilde_equal,


    /// <summary>
    ///     小于。
    ///     。
    /// </summary>
    less,


    /// <summary>
    ///     小于等于。
    ///     。
    /// </summary>
    less_equal,


    /// <summary>
    ///     大于。
    ///     。
    /// </summary>
    greater,


    /// <summary>
    ///     大于等于。
    ///     。
    /// </summary>
    greater_equal,


    /// <summary>
    ///     赋值。
    ///     。
    /// </summary>
    equal,


    /// <summary>
    ///     左括号。
    ///     。
    /// </summary>
    left_paren,


    /// <summary>
    ///     右括号。
    ///     。
    /// </summary>
    right_paren,


    /// <summary>
    ///     左花括号。
    ///     。
    /// </summary>
    left_brace,


    /// <summary>
    ///     右花括号。
    ///     。
    /// </summary>
    right_brace,


    /// <summary>
    ///     左方括号。
    ///     。
    /// </summary>
    left_bracket,


    /// <summary>
    ///     右方括号。
    ///     。
    /// </summary>
    right_bracket,


    /// <summary>
    ///     双冒号。
    ///     。
    /// </summary>
    double_colon,


    /// <summary>
    ///     分号。
    ///     。
    /// </summary>
    semicolon,


    /// <summary>
    ///     冒号。
    ///     。
    /// </summary>
    colon,


    /// <summary>
    ///     逗号。
    ///     。
    /// </summary>
    comma,


    /// <summary>
    ///     点。
    ///     。
    /// </summary>
    dot,


    /// <summary>
    ///     连接点。
    ///     。
    /// </summary>
    concat,


    /// <summary>
    ///     变长参数。
    ///     。
    /// </summary>
    dots,


    /// <summary>
    ///     行尾。
    ///     。
    /// </summary>
    end_of_line,


    /// <summary>
    ///     文件结尾。
    ///     。
    /// </summary>
    end_of_file
}