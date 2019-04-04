namespace Std.Data.Text.Lexing;

public interface ILexerProvider
{
    ILexer create_lexer();
}