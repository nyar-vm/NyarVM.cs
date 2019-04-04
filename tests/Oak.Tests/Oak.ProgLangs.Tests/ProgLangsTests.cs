using Oak.Rust;
using Oak.JavaScript.Lexer;
using Oak.JavaScript.Parser;
using Oak.Haskell.Lexer;
using Oak.Haskell.Parser;
using Oak.OCaml.Lexer;
using Oak.OCaml.Parser;
using Oak.Erlang.Lexer;
using Oak.Erlang.Parser;
using Oak.Julia.Lexer;
using Oak.Julia.Parser;
using Oak.Prolog.Lexer;
using Oak.Prolog.Parser;
using Oak.Lua;

namespace Oak.ProgLangs.Tests;

public class RustParserTests
{
    private readonly RustPipeline _pipeline = new();

    [Fact]
    public void Parse_EmptySource_ShouldReturnCrate()
    {
        var result = _pipeline.Parse("");
        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_FunctionDecl_ShouldParseFn()
    {
        var source = "micro main() {}";
        var result = _pipeline.Parse(source);
        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_LetBinding_ShouldParseLet()
    {
        var source = "micro main() { let x = 42; }";
        var result = _pipeline.Parse(source);
        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_StructDecl_ShouldParseStruct()
    {
        var source = "struct Point { x: f64, y: f64 }";
        var result = _pipeline.Parse(source);
        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_EnumDecl_ShouldParseEnum()
    {
        var source = "enum Color { Red, Green, Blue }";
        var result = _pipeline.Parse(source);
        Assert.NotNull(result);
    }
}

public class JsParserTests
{
    private readonly JsLexer _lexer = new();
    private readonly JsParser _parser = new();

    [Fact]
    public void Tokenize_Keywords_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize("function return var let const");
        Assert.NotEmpty(tokens);
    }

    [Fact]
    public void Parse_FunctionDecl_ShouldNotThrow()
    {
        var tokens = _lexer.Tokenize("function hello() {}");
        var result = _parser.Parse(tokens);
        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_VariableDecl_ShouldNotThrow()
    {
        var tokens = _lexer.Tokenize("let x = 10;");
        var result = _parser.Parse(tokens);
        Assert.NotNull(result);
    }
}

public class HsParserTests
{
    private readonly HsLexer _lexer = new();
    private readonly HsParser _parser = new();

    [Fact]
    public void Tokenize_Keywords_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize("module where import data type class");
        Assert.NotEmpty(tokens);
    }

    [Fact]
    public void Parse_ModuleDecl_ShouldNotThrow()
    {
        var tokens = _lexer.Tokenize("module Main where");
        var result = _parser.Parse(tokens);
        Assert.NotNull(result);
    }
}

public class OcParserTests
{
    private readonly OcLexer _lexer = new();
    private readonly OcParser _parser = new();

    [Fact]
    public void Tokenize_Keywords_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize("let rec type module open");
        Assert.NotEmpty(tokens);
    }

    [Fact]
    public void Parse_ModuleDecl_ShouldNotThrow()
    {
        var tokens = _lexer.Tokenize("open Sonic.Core\nlet x = 1");
        var result = _parser.Parse(tokens);
        Assert.NotNull(result);
    }
}

public class ErParserTests
{
    private readonly ErLexer _lexer = new();
    private readonly ErParser _parser = new();

    [Fact]
    public void Tokenize_Keywords_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize("-module -export -spec");
        Assert.NotEmpty(tokens);
    }

    [Fact]
    public void Parse_ModuleDecl_ShouldNotThrow()
    {
        var tokens = _lexer.Tokenize("-module(hello).");
        var result = _parser.Parse(tokens);
        Assert.NotNull(result);
    }
}

public class JlParserTests
{
    private readonly JlLexer _lexer = new();
    private readonly JlParser _parser = new();

    [Fact]
    public void Tokenize_Keywords_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize("function end if else for while");
        Assert.NotEmpty(tokens);
    }

    [Fact]
    public void Parse_Block_ShouldNotThrow()
    {
        var tokens = _lexer.Tokenize("x = 1 + 2");
        var result = _parser.Parse(tokens);
        Assert.NotNull(result);
    }
}

public class PlParserTests
{
    private readonly PlLexer _lexer = new();
    private readonly PlParser _parser = new();

    [Fact]
    public void Tokenize_Facts_ShouldReturnTokens()
    {
        var tokens = _lexer.Tokenize("likes(john, mary).");
        Assert.NotEmpty(tokens);
    }

    [Fact]
    public void Parse_Program_ShouldNotThrow()
    {
        var tokens = _lexer.Tokenize("parent(tom, bob).");
        var result = _parser.Parse(tokens);
        Assert.NotNull(result);
    }
}

public class LuaLexerTests
{
    private readonly LuaLexer _lexer = new();

    [Fact]
    public void NextToken_Keywords_ShouldReturnKeywordTokens()
    {
        _lexer.SetSource("function end if then else");
        var token = _lexer.NextToken();
        Assert.True(token.type != LuaTokenType.EndOfFile);
    }

    [Fact]
    public void NextToken_Number_ShouldReturnNumberToken()
    {
        _lexer.SetSource("42");
        var token = _lexer.NextToken();
        Assert.True(token.type != LuaTokenType.EndOfFile);
    }

    [Fact]
    public void NextToken_String_ShouldReturnStringToken()
    {
        _lexer.SetSource("\"hello\"");
        var token = _lexer.NextToken();
        Assert.True(token.type != LuaTokenType.EndOfFile);
    }
}
