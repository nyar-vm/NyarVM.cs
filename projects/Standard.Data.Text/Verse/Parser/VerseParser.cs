using System.Text;
using Std.Data.Text.Verse.AST;
using Std.Data.Text.Verse.Lexer;

namespace Std.Data.Text.Verse.Parser;


/// <summary>

///     Verse 语法解析器


/// </summary>
public sealed class VerseParser : ParserBase<string, CompilationUnit>
{
    private readonly string _filePath = string.Empty;
    private int _current;
    private IReadOnlyList<GreenLeafNode> _tokens = [];

    public VerseParser(DiagnosticSink? diagnostics = null)
        : base(diagnostics)
    {
    }

    
/// <summary>
    
///     解析 Verse 源代码
    

/// </summary>
    public override CompilationUnit Parse(string source)
    {
        var lexer = new VerseLexer(Diagnostics);
        _tokens = lexer.Tokenize(source);
        _current = 0;

        var declarations = new List<AstNode>();

        while (!IsAtEnd())
        {
            var decl = ParseDeclaration();

            if (decl is not null)
            {
                declarations.Add(decl);
            }
        }

        return new CompilationUnit(declarations, _filePath);
    }

    #region Token Access

    private bool IsAtEnd()
    {
        return Peek().Kind == VerseNodeKind.Eof;
    }

    private GreenLeafNode Peek()
    {
        return _current < _tokens.Count ? _tokens[_current] : _tokens[^1];
    }

    private GreenLeafNode Previous()
    {
        return _tokens[_current - 1];
    }

    private GreenLeafNode Advance()
    {
        if (!IsAtEnd())
        {
            _current++;
        }

        return Previous();
    }

    private bool Check(NodeKind type)
    {
        return !IsAtEnd() && Peek().Kind == type;
    }

    private bool Check(NodeKind type, string value)
    {
        return !IsAtEnd() && Peek().Kind == type && Peek().Text == value;
    }

    private bool Match(NodeKind type)
    {
        if (Check(type))
        {
            Advance();
            return true;
        }

        return false;
    }

    private bool Match(NodeKind type, string value)
    {
        if (Check(type, value))
        {
            Advance();
            return true;
        }

        return false;
    }

    private GreenLeafNode Consume(NodeKind type, string errorCode, string message)
    {
        if (Check(type))
        {
            return Advance();
        }

        var token = Peek();
        Diagnostics?.AddError(
            _filePath,
            default(TextSpan),
            errorCode,
            message);

        throw new ParseException(message);
    }

    private GreenLeafNode ConsumeKeyword(string keyword, string errorCode, string message)
    {
        if (Check(VerseNodeKind.Keyword, keyword))
        {
            return Advance();
        }

        var token = Peek();
        Diagnostics?.AddError(
            _filePath,
            default(TextSpan),
            errorCode,
            message);

        throw new ParseException(message);
    }

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if (Peek().Kind == VerseNodeKind.Keyword)
            {
                switch (Peek().Text)
                {
                    case "scene":
                    case "label":
                    case "menu":
                    case "if":
                    case "jump":
                    case "call":
                    case "return":
                    case "set":
                    case "pause":
                    case "wait":
                        return;
                }
            }

            if (Peek().Kind == VerseNodeKind.LabelMarker || Peek().Kind == VerseNodeKind.CommandPrefix)
            {
                return;
            }

            Advance();
        }
    }

    #endregion

    #region Declarations

    private AstNode? ParseDeclaration()
    {
        try
        {
            if (Check(VerseNodeKind.Keyword, "scene"))
            {
                return ParseSceneDecl();
            }

            if (Check(VerseNodeKind.LabelMarker))
            {
                return ParseLabelDecl();
            }

            if (Check(VerseNodeKind.Keyword, "menu"))
            {
                return ParseMenuDecl();
            }

            if (Check(VerseNodeKind.Keyword, "if"))
            {
                return ParseIfStmt();
            }

            if (Check(VerseNodeKind.Keyword, "jump"))
            {
                return ParseJumpStmt();
            }

            if (Check(VerseNodeKind.Keyword, "call"))
            {
                return ParseCallStmt();
            }

            if (Check(VerseNodeKind.Keyword, "return"))
            {
                return ParseReturnStmt();
            }

            if (Check(VerseNodeKind.Keyword, "set") || Check(VerseNodeKind.Keyword, "let") || Check(VerseNodeKind.Keyword, "var"))
            {
                return ParseSetStmt();
            }

            if (Check(VerseNodeKind.Keyword, "pause"))
            {
                return ParsePauseStmt();
            }

            if (Check(VerseNodeKind.Keyword, "wait"))
            {
                return ParseWaitStmt();
            }

            if (Check(VerseNodeKind.CommandPrefix))
            {
                return ParseCommandCall();
            }

            if (Check(VerseNodeKind.Identifier))
            {
                return ParseDialogueOrNarration();
            }

            if (Check(VerseNodeKind.String))
            {
                return ParseNarrationLine();
            }

            if (Check(VerseNodeKind.ChoiceMarker, "-"))
            {
                return ParseDialogueOrNarration();
            }

            Advance();
            return null;
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    private SceneDecl ParseSceneDecl()
    {
        var startToken = ConsumeKeyword("scene", "VERSE1101", "期望 'scene' 关键字");

        var name = Consume(VerseNodeKind.Identifier, "VERSE1102", "期望场景名称").Text!;

        var body = new List<AstNode>();

        while (!IsAtEnd() && !Check(VerseNodeKind.Keyword, "scene"))
        {
            var stmt = ParseDeclaration();

            if (stmt is not null)
            {
                body.Add(stmt);
            }
        }

        return new SceneDecl(name, body, default);
    }

    private LabelDecl ParseLabelDecl()
    {
        var startToken = Advance();

        return new LabelDecl(startToken.Text!, default);
    }

    private DialogueLine ParseDialogueOrNarration()
    {
        var startToken = Peek();

        if (Check(VerseNodeKind.ChoiceMarker, "-"))
        {
            Advance();
        }

        var speaker = Consume(VerseNodeKind.Identifier, "VERSE1103", "期望角色名称").Text!;

        string? emotion = null;

        if (Match(VerseNodeKind.Delimiter, "."))
        {
            emotion = Consume(VerseNodeKind.Identifier, "VERSE1104", "期望表情名称").Text;
        }

        if (Check(VerseNodeKind.Punctuation, ":") || Check(VerseNodeKind.Delimiter, ":"))
        {
            Advance();
        }
        else
        {
            Consume(VerseNodeKind.Delimiter, "VERSE1105", "期望 ':'");
        }

        var text = ParseDialogueText();

        return new DialogueLine(speaker, text, emotion, default);
    }

    private string ParseDialogueText()
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek().Kind != VerseNodeKind.Keyword &&
               Peek().Kind != VerseNodeKind.LabelMarker &&
               Peek().Kind != VerseNodeKind.CommandPrefix &&
               Peek().Kind != VerseNodeKind.ChoiceMarker)
        {
            if (IsNewDialogueLine())
            {
                break;
            }

            var token = Advance();
            sb.Append(token.Text);
        }

        return sb.ToString().Trim();
    }

    private bool IsNewDialogueLine()
    {
        if (Peek().Kind == VerseNodeKind.Identifier && LookaheadIsColon())
        {
            return true;
        }

        if (Peek().Kind == VerseNodeKind.String)
        {
            if (_current + 2 < _tokens.Count)
            {
                var next1 = _tokens[_current + 1];
                var next2 = _tokens[_current + 2];
                if (next1.Kind == VerseNodeKind.Identifier &&
                    next2.Kind == VerseNodeKind.Punctuation && next2.Text == ":")
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool LookaheadIsColon()
    {
        if (_current + 1 >= _tokens.Count)
        {
            return false;
        }

        var next = _tokens[_current + 1];
        return (next.Kind == VerseNodeKind.Punctuation || next.Kind == VerseNodeKind.Delimiter) && next.Text == ":";
    }

    private NarrationLine ParseNarrationLine()
    {
        var startToken = Peek();
        var text = Advance().Text;

        return new NarrationLine(text!, default);
    }

    private MenuDecl ParseMenuDecl()
    {
        var startToken = ConsumeKeyword("menu", "VERSE1106", "期望 'menu' 关键字");

        var items = new List<MenuItem>();

        while (!IsAtEnd() && Check(VerseNodeKind.ChoiceMarker)) items.Add(ParseMenuItem());

        if (Check(VerseNodeKind.Keyword, "endmenu"))
        {
            Advance();
        }

        return new MenuDecl(items, default);
    }

    private MenuItem ParseMenuItem()
    {
        var startToken = Advance();

        var text = Consume(VerseNodeKind.String, "VERSE1107", "期望选项文本").Text!;

        AstNode? condition = null;
        if (Match(VerseNodeKind.Keyword, "if"))
        {
            condition = ParseExpression();
        }

        var body = new List<AstNode>();

        while (!IsAtEnd() && !Check(VerseNodeKind.ChoiceMarker) &&
               !Check(VerseNodeKind.Keyword, "endmenu") &&
               !Check(VerseNodeKind.Keyword, "scene") &&
               !Check(VerseNodeKind.Keyword, "menu"))
        {
            var stmt = ParseDeclaration();

            if (stmt is not null)
            {
                body.Add(stmt);
            }
        }

        return new MenuItem(text, condition, body, default);
    }

    private JumpStmt ParseJumpStmt()
    {
        var startToken = ConsumeKeyword("jump", "VERSE1108", "期望 'jump' 关键字");

        AstNode? condition = null;
        if (Match(VerseNodeKind.Keyword, "if"))
        {
            condition = ParseExpression();
        }

        var target = Consume(VerseNodeKind.Identifier, "VERSE1109", "期望跳转目标").Text!;

        return new JumpStmt(target, condition, default);
    }

    private CallStmt ParseCallStmt()
    {
        var startToken = ConsumeKeyword("call", "VERSE1110", "期望 'call' 关键字");

        var target = Consume(VerseNodeKind.Identifier, "VERSE1111", "期望调用目标").Text!;

        var arguments = new List<AstNode>();

        if (Match(VerseNodeKind.Delimiter, "("))
        {
            if (!Check(VerseNodeKind.Delimiter, ")"))
            {
                arguments.Add(ParseExpression());

                while (Match(VerseNodeKind.Delimiter, ",")) arguments.Add(ParseExpression());
            }

            Consume(VerseNodeKind.Delimiter, "VERSE1112", "期望 ')'");
        }

        return new CallStmt(target, arguments, default);
    }

    private ReturnStmt ParseReturnStmt()
    {
        var startToken = ConsumeKeyword("return", "VERSE1113", "期望 'return' 关键字");

        return new ReturnStmt();
    }

    private PauseStmt ParsePauseStmt()
    {
        var startToken = ConsumeKeyword("pause", "VERSE1114", "期望 'pause' 关键字");

        double? duration = null;
        if (Check(VerseNodeKind.Number))
        {
            var numToken = Advance();
            if (double.TryParse(numToken.Text, out var d))
            {
                duration = d;
            }
        }

        return new PauseStmt(duration, default);
    }

    private WaitStmt ParseWaitStmt()
    {
        var startToken = ConsumeKeyword("wait", "VERSE1115", "期望 'wait' 关键字");

        var numToken = Consume(VerseNodeKind.Number, "VERSE1116", "期望等待时长");
        var duration = double.TryParse(numToken.Text, out var d) ? d : 0.0;

        return new WaitStmt(duration, default);
    }

    private SetStmt ParseSetStmt()
    {
        var startToken = Advance();

        var varName = Consume(VerseNodeKind.Identifier, "VERSE1117", "期望变量名").Text!;

        var op = "=";
        if (Check(VerseNodeKind.Operator))
        {
            op = Advance().Text!;
        }
        else
        {
            Consume(VerseNodeKind.Operator, "VERSE1118", "期望赋值运算符");
        }

        var value = ParseExpression();

        return new SetStmt(varName, op, value, default);
    }

    private IfStmt ParseIfStmt()
    {
        var startToken = ConsumeKeyword("if", "VERSE1119", "期望 'if' 关键字");

        var condition = ParseExpression();

        var thenBody = new List<AstNode>();

        while (!IsAtEnd() && !Check(VerseNodeKind.Keyword, "elif") &&
               !Check(VerseNodeKind.Keyword, "else") && !Check(VerseNodeKind.Keyword, "endif"))
        {
            var stmt = ParseDeclaration();

            if (stmt is not null)
            {
                thenBody.Add(stmt);
            }
        }

        var elifBranches = new List<ElifBranch>();

        while (Check(VerseNodeKind.Keyword, "elif"))
        {
            Advance();
            var elifCondition = ParseExpression();

            var elifBody = new List<AstNode>();

            while (!IsAtEnd() && !Check(VerseNodeKind.Keyword, "elif") &&
                   !Check(VerseNodeKind.Keyword, "else") && !Check(VerseNodeKind.Keyword, "endif"))
            {
                var stmt = ParseDeclaration();

                if (stmt is not null)
                {
                    elifBody.Add(stmt);
                }
            }

            elifBranches.Add(new ElifBranch(elifCondition, elifBody));
        }

        ElseBranch? elseBranch = null;

        if (Match(VerseNodeKind.Keyword, "else"))
        {
            var elseBody = new List<AstNode>();

            while (!IsAtEnd() && !Check(VerseNodeKind.Keyword, "endif"))
            {
                var stmt = ParseDeclaration();

                if (stmt is not null)
                {
                    elseBody.Add(stmt);
                }
            }

            elseBranch = new ElseBranch(elseBody);
        }

        if (Check(VerseNodeKind.Keyword, "endif"))
        {
            Advance();
        }

        return new IfStmt(condition, thenBody, elifBranches, elseBranch, default);
    }

    private CommandCall ParseCommandCall()
    {
        var startToken = Advance();

        var commandName = startToken.Text!;

        var arguments = new List<CommandArg>();

        while (!IsAtEnd() && (Check(VerseNodeKind.Identifier) || Check(VerseNodeKind.String) || Check(VerseNodeKind.Number)))
        {
            if (Check(VerseNodeKind.Identifier))
            {
                var argName = Advance().Text!;

                if (Match(VerseNodeKind.Punctuation, ":") || Match(VerseNodeKind.Delimiter, ":") || Match(VerseNodeKind.Operator, "="))
                {
                    var argValue = ParseExpression();
                    arguments.Add(new CommandArg(argName, argValue));
                }
                else
                {
                    arguments.Add(new CommandArg(null, new IdentifierExpr(argName)));
                }
            }
            else if (Check(VerseNodeKind.String))
            {
                var text = Advance().Text!;
                arguments.Add(new CommandArg(null, new LiteralExpr(LiteralType.String, text)));
            }
            else if (Check(VerseNodeKind.Number))
            {
                var text = Advance().Text!;
                arguments.Add(new CommandArg(null, new LiteralExpr(LiteralType.Number, text)));
            }
        }

        return new CommandCall(commandName, arguments, default);
    }

    #endregion

    #region Expressions

    private AstNode ParseExpression()
    {
        return ParseOr();
    }

    private AstNode ParseOr()
    {
        var left = ParseAnd();

        while (Match(VerseNodeKind.Operator, "||"))
        {
            var op = Previous().Text!;
            var right = ParseAnd();
            left = new BinaryExpr(left, op, right);
        }

        return left;
    }

    private AstNode ParseAnd()
    {
        var left = ParseEquality();

        while (Match(VerseNodeKind.Operator, "&&"))
        {
            var op = Previous().Text!;
            var right = ParseEquality();
            left = new BinaryExpr(left, op, right);
        }

        return left;
    }

    private AstNode ParseEquality()
    {
        var left = ParseComparison();

        while (Check(VerseNodeKind.Operator, "==") || Check(VerseNodeKind.Operator, "!="))
        {
            var op = Advance().Text!;
            var right = ParseComparison();
            left = new BinaryExpr(left, op, right);
        }

        return left;
    }

    private AstNode ParseComparison()
    {
        var left = ParseAddition();

        while (Check(VerseNodeKind.Operator, "<") || Check(VerseNodeKind.Operator, ">") ||
               Check(VerseNodeKind.Operator, "<=") || Check(VerseNodeKind.Operator, ">="))
        {
            var op = Advance().Text!;
            var right = ParseAddition();
            left = new BinaryExpr(left, op, right);
        }

        return left;
    }

    private AstNode ParseAddition()
    {
        var left = ParseMultiplication();

        while (Check(VerseNodeKind.Operator, "+") || Check(VerseNodeKind.Operator, "-"))
        {
            var op = Advance().Text!;
            var right = ParseMultiplication();
            left = new BinaryExpr(left, op, right);
        }

        return left;
    }

    private AstNode ParseMultiplication()
    {
        var left = ParseUnary();

        while (Check(VerseNodeKind.Operator, "*") || Check(VerseNodeKind.Operator, "/") || Check(VerseNodeKind.Operator, "%"))
        {
            var op = Advance().Text!;
            var right = ParseUnary();
            left = new BinaryExpr(left, op, right);
        }

        return left;
    }

    private AstNode ParseUnary()
    {
        if (Check(VerseNodeKind.Operator, "-") || Check(VerseNodeKind.Operator, "!"))
        {
            var op = Advance().Text!;
            var operand = ParseUnary();
            return new UnaryExpr(op, operand);
        }

        return ParsePrimary();
    }

    private AstNode ParsePrimary()
    {
        if (Check(VerseNodeKind.Number))
        {
            var token = Advance();
            return new LiteralExpr(LiteralType.Number, token.Text!, default);
        }

        if (Check(VerseNodeKind.String))
        {
            var token = Advance();
            return new LiteralExpr(LiteralType.String, token.Text!, default);
        }

        if (Check(VerseNodeKind.Literal))
        {
            var token = Advance();
            var kind = token.Text switch
            {
                "true" or "false" => LiteralType.Boolean,
                "null" => LiteralType.Null,
                _ => LiteralType.Null
            };
            return new LiteralExpr(kind, token.Text!, default);
        }

        if (Check(VerseNodeKind.Identifier))
        {
            var token = Advance();
            return new IdentifierExpr(token.Text!, default);
        }

        if (Check(VerseNodeKind.Delimiter, "("))
        {
            Advance();
            var expr = ParseExpression();
            Consume(VerseNodeKind.Delimiter, "VERSE1121", "期望 ')'");
            return expr;
        }

        var errorToken = Peek();
        Diagnostics?.AddError(
            _filePath,
            default(TextSpan),
            "VERSE1122",
            $"意外的标记 '{errorToken.Text}'");

        throw new ParseException($"意外的标记 '{errorToken.Text}'");
    }

    #endregion
}
