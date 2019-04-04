namespace Std.Data.Text.Julia.Lexer;

public enum JlTokenType
{
    eof,
    identifier,
    keyword,
    number,
    @string,
    @char,
    @operator,
    delimiter,
    punctuation,
    comment,
    macro_name,
    command_type,
    symbol
}
