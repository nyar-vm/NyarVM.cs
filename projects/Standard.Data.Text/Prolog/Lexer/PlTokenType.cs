namespace Std.Data.Text.Prolog.Lexer;

public enum PlTokenType
{
    Eof,
    Atom,
    Variable,
    Number,
    String,
    Operator,
    Delimiter,
    Punctuation,
    Comment,
    Functor
}
