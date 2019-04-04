using Std.Data.Text.Haskell.AST;
using Std.Data.Text.Haskell.Lexer;

namespace Std.Data.Text.Haskell.Parser;

public sealed class HsParser : IParser<IReadOnlyList<HsToken>, HsAstNode>
{
    private int _current;
    private DiagnosticSink? _diagnostics;
    private IReadOnlyList<HsToken> _tokens = [];

    public HsParser(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    public HsAstNode Parse(IReadOnlyList<HsToken> tokens)
    {
        _tokens = tokens;
        _current = 0;
        _diagnostics ??= new DiagnosticSink();

        var declarations = new List<HsAstNode>();

        while (!IsAtEnd())
        {
            var decl = ParseDeclaration();
            if (decl is not null)
            {
                declarations.Add(decl);
            }
        }

        return new HsModule("Main", [], [], declarations);
    }

    #region Token Access

    private bool IsAtEnd()
    {
        return Peek().Type == HsTokenType.Eof;
    }

    private HsToken Peek()
    {
        return _current < _tokens.Count ? _tokens[_current] : _tokens[^1];
    }

    private HsToken Previous()
    {
        return _tokens[_current - 1];
    }

    private HsToken Advance()
    {
        if (!IsAtEnd())
        {
            _current++;
        }

        return Previous();
    }

    private bool Check(HsTokenType type)
    {
        return !IsAtEnd() && Peek().Type == type;
    }

    private bool Check(HsTokenType type, string value)
    {
        return !IsAtEnd() && Peek().Type == type && Peek().Value == value;
    }

    private bool Match(HsTokenType type)
    {
        if (Check(type))
        {
            Advance();
            return true;
        }

        return false;
    }

    private bool Match(HsTokenType type, string value)
    {
        if (Check(type, value))
        {
            Advance();
            return true;
        }

        return false;
    }

    private HsToken Consume(HsTokenType type, string errorCode, string message)
    {
        if (Check(type))
        {
            return Advance();
        }

        var token = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default(TextSpan),
            errorCode,
            message);

        throw new ParseException(message);
    }

    private HsToken ConsumeKeyword(string keyword, string errorCode, string message)
    {
        if (Check(HsTokenType.Keyword, keyword))
        {
            return Advance();
        }

        var token = Peek();
        _diagnostics?.AddError(
            string.Empty,
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
            if (Peek().Type == HsTokenType.Keyword)
            {
                var value = Peek().Value;
                if (value is "module" or "import" or "data" or "type" or "class"
                    or "instance" or "newtype" or "deriving")
                {
                    return;
                }
            }

            Advance();
        }
    }

    #endregion

    #region Declarations

    private HsAstNode? ParseDeclaration()
    {
        try
        {
            if (Check(HsTokenType.Keyword, "module"))
            {
                return ParseModuleDecl();
            }

            if (Check(HsTokenType.Keyword, "import"))
            {
                return ParseImportDecl();
            }

            if (Check(HsTokenType.Keyword, "data"))
            {
                return ParseDataDecl();
            }

            if (Check(HsTokenType.Keyword, "newtype"))
            {
                return ParseNewtypeDecl();
            }

            if (Check(HsTokenType.Keyword, "type"))
            {
                return ParseTypeDecl();
            }

            if (Check(HsTokenType.Keyword, "class"))
            {
                return ParseClassDecl();
            }

            if (Check(HsTokenType.Keyword, "instance"))
            {
                return ParseInstanceDecl();
            }

            return ParseBindingOrSignature();
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    private HsModule ParseModuleDecl()
    {
        ConsumeKeyword("module", "HSK2001", "期望 'module' 关键字");
        var name = Consume(HsTokenType.Identifier, "HSK2002", "期望模块名").Value;

        IReadOnlyList<HsAstNode> exports = [];
        if (Match(HsTokenType.Delimiter, "("))
        {
            exports = ParseExportList();
        }

        ConsumeKeyword("where", "HSK2003", "期望 'where' 关键字");

        var imports = new List<HsAstNode>();
        var declarations = new List<HsAstNode>();

        while (!IsAtEnd())
        {
            if (Check(HsTokenType.Keyword, "import"))
            {
                var imp = ParseImportDecl();
                if (imp is not null)
                {
                    imports.Add(imp);
                }
            }
            else
            {
                var decl = ParseDeclaration();
                if (decl is not null)
                {
                    declarations.Add(decl);
                }
            }
        }

        return new HsModule(name, exports, imports, declarations);
    }

    private IReadOnlyList<HsAstNode> ParseExportList()
    {
        var exports = new List<HsAstNode>();

        while (!Check(HsTokenType.Delimiter, ")") && !IsAtEnd())
        {
            var name = Consume(HsTokenType.Identifier, "HSK2004", "期望导出名").Value;
            exports.Add(new HsExport(name));

            Match(HsTokenType.Delimiter, ",");
        }

        Consume(HsTokenType.Delimiter, "HSK2005", "期望 ')'");
        return exports;
    }

    private HsImport ParseImportDecl()
    {
        ConsumeKeyword("import", "HSK2006", "期望 'import' 关键字");

        var qualified = Match(HsTokenType.Keyword, "qualified");

        var moduleBuilder = new System.Text.StringBuilder();
        moduleBuilder.Append(Consume(HsTokenType.Identifier, "HSK2007", "期望模块名").Value);

        while (Match(HsTokenType.Operator, "."))
            moduleBuilder.Append('.').Append(Consume(HsTokenType.Identifier, "HSK2008", "期望模块名").Value);

        var moduleName = moduleBuilder.ToString();

        string? alias = null;
        if (Match(HsTokenType.Keyword, "as"))
        {
            alias = Consume(HsTokenType.Identifier, "HSK2009", "期望别名").Value;
        }

        var hiding = false;
        IReadOnlyList<string>? names = null;

        if (Match(HsTokenType.Keyword, "hiding"))
        {
            hiding = true;
            if (Match(HsTokenType.Delimiter, "("))
            {
                names = ParseImportNames();
            }
        }
        else if (Match(HsTokenType.Delimiter, "("))
        {
            names = ParseImportNames();
        }

        return new HsImport(qualified, moduleName, alias, hiding, names);
    }

    private IReadOnlyList<string> ParseImportNames()
    {
        var names = new List<string>();

        while (!Check(HsTokenType.Delimiter, ")") && !IsAtEnd())
        {
            names.Add(Consume(HsTokenType.Identifier, "HSK2010", "期望导入名").Value);
            Match(HsTokenType.Delimiter, ",");
        }

        Consume(HsTokenType.Delimiter, "HSK2011", "期望 ')'");
        return names;
    }

    private HsDataDecl ParseDataDecl()
    {
        return ParseDataDeclInner(false);
    }

    private HsDataDecl ParseNewtypeDecl()
    {
        return ParseDataDeclInner(true);
    }

    private HsDataDecl ParseDataDeclInner(bool isNewtype)
    {
        ConsumeKeyword(isNewtype ? "newtype" : "data", "HSK2012",
            isNewtype ? "期望 'newtype' 关键字" : "期望 'data' 关键字");

        var name = Consume(HsTokenType.Identifier, "HSK2013", "期望数据类型名").Value;

        var typeVars = new List<string>();
        while (Check(HsTokenType.Identifier) && !Check(HsTokenType.Operator, "="))
        {
            var tv = Peek().Value;
            if (char.IsLower(tv[0]))
            {
                typeVars.Add(Advance().Value);
            }
            else
            {
                break;
            }
        }

        Consume(HsTokenType.Operator, "HSK2014", "期望 '='");

        var constructors = new List<HsAstNode>();
        constructors.Add(ParseConstructor());

        while (Match(HsTokenType.Operator, "|")) constructors.Add(ParseConstructor());

        var deriving = new List<string>();
        if (Check(HsTokenType.Keyword, "deriving"))
        {
            Advance();
            if (Match(HsTokenType.Delimiter, "("))
            {
                while (!Check(HsTokenType.Delimiter, ")") && !IsAtEnd())
                {
                    deriving.Add(Consume(HsTokenType.Identifier, "HSK2015", "期望派生类名").Value);
                    Match(HsTokenType.Delimiter, ",");
                }

                Consume(HsTokenType.Delimiter, "HSK2016", "期望 ')'");
            }
            else
            {
                deriving.Add(Consume(HsTokenType.Identifier, "HSK2017", "期望派生类名").Value);
            }
        }

        return new HsDataDecl(name, typeVars, constructors, deriving, isNewtype);
    }

    private HsConstructor ParseConstructor()
    {
        var name = Consume(HsTokenType.Identifier, "HSK2018", "期望构造器名").Value;

        var fields = new List<HsAstNode>();
        while (!IsAtEnd() && !Check(HsTokenType.Operator, "|") &&
               !Check(HsTokenType.Keyword, "deriving") && !Check(HsTokenType.Delimiter, "{"))
        {
            if (Check(HsTokenType.Identifier) || Check(HsTokenType.Delimiter, "(") ||
                Check(HsTokenType.Delimiter, "["))
            {
                fields.Add(ParseType());
            }
            else
            {
                break;
            }
        }

        return new HsConstructor(name, fields);
    }

    private HsAstNode ParseTypeDecl()
    {
        ConsumeKeyword("type", "HSK2019", "期望 'type' 关键字");

        var name = Consume(HsTokenType.Identifier, "HSK2020", "期望类型名").Value;

        var typeVars = new List<string>();
        while (Check(HsTokenType.Identifier))
        {
            var tv = Peek().Value;
            if (char.IsLower(tv[0]))
            {
                typeVars.Add(Advance().Value);
            }
            else
            {
                break;
            }
        }

        Consume(HsTokenType.Operator, "HSK2021", "期望 '='");
        var type = ParseType();

        return new HsTypeDecl(name, typeVars, type);
    }

    private HsTypeClassDecl ParseClassDecl()
    {
        ConsumeKeyword("class", "HSK2022", "期望 'class' 关键字");

        var superClasses = new List<string>();
        if (Check(HsTokenType.Identifier) && PeekNextIsConstraintArrow())
        {
            superClasses.Add(Consume(HsTokenType.Identifier, "HSK2023", "期望超类名").Value);
            while (Match(HsTokenType.Operator, ","))
                superClasses.Add(Consume(HsTokenType.Identifier, "HSK2024", "期望超类名").Value);
            Consume(HsTokenType.Operator, "HSK2025", "期望 '=>'");
        }

        var className = Consume(HsTokenType.Identifier, "HSK2026", "期望类名").Value;

        var typeVars = new List<string>();
        while (Check(HsTokenType.Identifier))
        {
            var tv = Peek().Value;
            if (char.IsLower(tv[0]))
            {
                typeVars.Add(Advance().Value);
            }
            else
            {
                break;
            }
        }

        var methods = new List<HsAstNode>();
        if (Match(HsTokenType.Keyword, "where"))
        {
            if (Match(HsTokenType.Delimiter, "{"))
            {
                while (!Check(HsTokenType.Delimiter, "}") && !IsAtEnd())
                {
                    var method = ParseTypeSignatureOrDefault();
                    if (method is not null)
                    {
                        methods.Add(method);
                    }

                    Match(HsTokenType.Delimiter, ";");
                }

                Consume(HsTokenType.Delimiter, "HSK2027", "期望 '}'");
            }
        }

        return new HsTypeClassDecl(className, typeVars, superClasses.Select(s => new HsTypeCon(s)).ToList(), methods);
    }

    private HsInstanceDecl ParseInstanceDecl()
    {
        ConsumeKeyword("instance", "HSK2028", "期望 'instance' 关键字");

        var constraints = new List<string>();
        if (Check(HsTokenType.Identifier) && PeekNextIsConstraintArrow())
        {
            constraints.Add(Consume(HsTokenType.Identifier, "HSK2029", "期望约束名").Value);
            while (Match(HsTokenType.Operator, ","))
                constraints.Add(Consume(HsTokenType.Identifier, "HSK2030", "期望约束名").Value);
            Consume(HsTokenType.Operator, "HSK2031", "期望 '=>'");
        }

        var className = Consume(HsTokenType.Identifier, "HSK2032", "期望类名").Value;

        var types = new List<HsAstNode>();
        while (Check(HsTokenType.Identifier) || Check(HsTokenType.Delimiter, "("))
            types.Add(ParseType());

        var definitions = new List<HsAstNode>();
        if (Match(HsTokenType.Keyword, "where"))
        {
            if (Match(HsTokenType.Delimiter, "{"))
            {
                while (!Check(HsTokenType.Delimiter, "}") && !IsAtEnd())
                {
                    var def = ParseBindingOrSignature();
                    if (def is not null)
                    {
                        definitions.Add(def);
                    }

                    Match(HsTokenType.Delimiter, ";");
                }

                Consume(HsTokenType.Delimiter, "HSK2033", "期望 '}'");
            }
        }

        return new HsInstanceDecl(constraints, className, types, definitions);
    }

    private bool PeekNextIsConstraintArrow()
    {
        if (_current + 1 >= _tokens.Count)
        {
            return false;
        }

        var next = _tokens[_current + 1];
        return next.Type == HsTokenType.Operator && next.Value == "=>";
    }

    private HsAstNode? ParseBindingOrSignature()
    {
        if (!Check(HsTokenType.Identifier))
        {
            return null;
        }

        var name = Advance().Value;

        if (Match(HsTokenType.Operator, "::"))
        {
            var type = ParseType();
            return new HsTypeSignature(name, [], type);
        }

        var patterns = new List<HsAstNode>();
        while (!Check(HsTokenType.Operator, "=") && !IsAtEnd())
        {
            if (Check(HsTokenType.Identifier) || Check(HsTokenType.Delimiter, "(") ||
                Check(HsTokenType.Delimiter, "[") || Check(HsTokenType.Char) ||
                Check(HsTokenType.Number) || Check(HsTokenType.String))
            {
                patterns.Add(ParsePattern());
            }
            else
            {
                break;
            }
        }

        Consume(HsTokenType.Operator, "HSK2034", "期望 '='");
        var body = ParseExpression();

        return new HsPatternBind(patterns.Count > 0
            ? patterns.Count == 1 ? patterns[0] : new HsTuplePattern(patterns)
            : new HsWildCardPattern(), body);
    }

    private HsAstNode? ParseTypeSignatureOrDefault()
    {
        if (!Check(HsTokenType.Identifier))
        {
            return null;
        }

        var name = Advance().Value;

        if (Match(HsTokenType.Operator, "::"))
        {
            var type = ParseType();
            return new HsTypeSignature(name, [], type);
        }

        return new HsTypeSignature(name, [], new HsTypeCon("unknown"));
    }

    #endregion

    #region Types

    private HsAstNode ParseType()
    {
        return ParseFunctionType();
    }

    private HsAstNode ParseFunctionType()
    {
        var left = ParseTypeApplication();

        while (Match(HsTokenType.Operator, "->"))
        {
            var right = ParseTypeApplication();
            left = new HsFunctionType(left, right);
        }

        return left;
    }

    private HsAstNode ParseTypeApplication()
    {
        var type = ParseAtomicType();

        while (Check(HsTokenType.Identifier) || Check(HsTokenType.Delimiter, "(") ||
               Check(HsTokenType.Delimiter, "["))
        {
            var arg = ParseAtomicType();
            type = new HsTypeApp(type, arg);
        }

        return type;
    }

    private HsAstNode ParseAtomicType()
    {
        if (Check(HsTokenType.Delimiter, "("))
        {
            Advance();
            if (Match(HsTokenType.Delimiter, ")"))
            {
                return new HsTupleType([]);
            }

            var type = ParseType();

            if (Check(HsTokenType.Delimiter, ","))
            {
                var types = new List<HsAstNode> { type };
                while (Match(HsTokenType.Delimiter, ",")) types.Add(ParseType());
                Consume(HsTokenType.Delimiter, "HSK2035", "期望 ')'");
                return new HsTupleType(types);
            }

            Consume(HsTokenType.Delimiter, "HSK2036", "期望 ')'");
            return type;
        }

        if (Match(HsTokenType.Delimiter, "["))
        {
            var elementType = ParseType();
            Consume(HsTokenType.Delimiter, "HSK2037", "期望 ']'");
            return new HsListType(elementType);
        }

        if (Check(HsTokenType.Identifier))
        {
            var name = Advance().Value;
            if (char.IsLower(name[0]))
            {
                return new HsTypeVar(name);
            }

            return new HsTypeCon(name);
        }

        var errorToken = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default(TextSpan),
            "HSK2038",
            $"期望类型，遇到 '{errorToken.Value}'");

        throw new ParseException($"期望类型，遇到 '{errorToken.Value}'");
    }

    #endregion

    #region Expressions

    private HsAstNode ParseExpression()
    {
        return ParseInfixExpression();
    }

    private HsAstNode ParseInfixExpression()
    {
        var left = ParseApplication();

        while (Check(HsTokenType.Operator) || Check(HsTokenType.Delimiter, "`"))
        {
            if (Check(HsTokenType.Delimiter, "`"))
            {
                Advance();
                var opName = Consume(HsTokenType.Identifier, "HSK2039", "期望中缀函数名").Value;
                Consume(HsTokenType.Delimiter, "HSK2040", "期望 '`'");
                var right = ParseApplication();
                left = new HsInfixApp(left, new HsIdentifier(opName), right);
            }
            else
            {
                var op = Advance().Value;
                var right = ParseApplication();
                left = new HsBinaryOp(left, op, right);
            }
        }

        return left;
    }

    private HsAstNode ParseApplication()
    {
        var expr = ParseAtomicExpression();

        while (Check(HsTokenType.Identifier) || Check(HsTokenType.Delimiter, "(") ||
               Check(HsTokenType.Delimiter, "[") || Check(HsTokenType.Char) ||
               Check(HsTokenType.Number) || Check(HsTokenType.String))
        {
            var arg = ParseAtomicExpression();
            expr = new HsApplication(expr, [arg]);
        }

        return expr;
    }

    private HsAstNode ParseAtomicExpression()
    {
        if (Check(HsTokenType.Number))
        {
            var token = Advance();
            return new HsLiteral("number", Peek().Value);
        }

        if (Check(HsTokenType.String))
        {
            var token = Advance();
            return new HsLiteral("string", Peek().Value);
        }

        if (Check(HsTokenType.Char))
        {
            var token = Advance();
            return new HsLiteral("char", Peek().Value);
        }

        if (Check(HsTokenType.Identifier))
        {
            var token = Advance();
            return new HsIdentifier(Peek().Value);
        }

        if (Check(HsTokenType.Delimiter, "("))
        {
            Advance();

            if (Match(HsTokenType.Delimiter, ")"))
            {
                return new HsUnit();
            }

            var expr = ParseExpression();

            if (Check(HsTokenType.Delimiter, ","))
            {
                var elements = new List<HsAstNode> { expr };
                while (Match(HsTokenType.Delimiter, ",")) elements.Add(ParseExpression());
                Consume(HsTokenType.Delimiter, "HSK2041", "期望 ')'");
                return new HsTuple(elements);
            }

            Consume(HsTokenType.Delimiter, "HSK2042", "期望 ')'");
            return expr;
        }

        if (Match(HsTokenType.Delimiter, "["))
        {
            var elements = new List<HsAstNode>();

            if (!Check(HsTokenType.Delimiter, "]"))
            {
                elements.Add(ParseExpression());

                while (Match(HsTokenType.Delimiter, ",")) elements.Add(ParseExpression());
            }

            Consume(HsTokenType.Delimiter, "HSK2043", "期望 ']'");
            return new HsList(elements);
        }

        if (Match(HsTokenType.Operator, "\\"))
        {
            var patterns = new List<HsAstNode>();
            while (!Check(HsTokenType.Operator, "->") && !IsAtEnd())
                patterns.Add(ParsePattern());

            Consume(HsTokenType.Operator, "HSK2044", "期望 '->'");
            var body = ParseExpression();
            return new HsLambda(patterns, body);
        }

        if (Check(HsTokenType.Keyword, "if"))
        {
            return ParseIfExpr();
        }

        if (Check(HsTokenType.Keyword, "case"))
        {
            return ParseCaseExpr();
        }

        if (Check(HsTokenType.Keyword, "let"))
        {
            return ParseLetExpr();
        }

        if (Check(HsTokenType.Keyword, "do"))
        {
            return ParseDoBlock();
        }

        var errorToken = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default(TextSpan),
            "HSK2045",
            $"意外的标记 '{errorToken.Value}'");

        throw new ParseException($"意外的标记 '{errorToken.Value}'");
    }

    private HsIfExpr ParseIfExpr()
    {
        ConsumeKeyword("if", "HSK2046", "期望 'if' 关键字");
        var condition = ParseExpression();
        ConsumeKeyword("then", "HSK2047", "期望 'then' 关键字");
        var thenBranch = ParseExpression();
        ConsumeKeyword("else", "HSK2048", "期望 'else' 关键字");
        var elseBranch = ParseExpression();

        return new HsIfExpr(condition, thenBranch, elseBranch);
    }

    private HsCaseExpr ParseCaseExpr()
    {
        ConsumeKeyword("case", "HSK2049", "期望 'case' 关键字");
        var scrutinee = ParseExpression();
        ConsumeKeyword("of", "HSK2050", "期望 'of' 关键字");

        var alternatives = new List<HsAlternative>();

        if (Match(HsTokenType.Delimiter, "{"))
        {
            while (!Check(HsTokenType.Delimiter, "}") && !IsAtEnd())
            {
                var alt = ParseAlternative();
                if (alt is not null)
                {
                    alternatives.Add(alt);
                }

                Match(HsTokenType.Delimiter, ";");
            }

            Consume(HsTokenType.Delimiter, "HSK2051", "期望 '}'");
        }

        return new HsCaseExpr(scrutinee, alternatives);
    }

    private HsAlternative? ParseAlternative()
    {
        var pattern = ParsePattern();
        Consume(HsTokenType.Operator, "HSK2052", "期望 '->'");
        var body = ParseExpression();

        return new HsAlternative(pattern, [], body);
    }

    private HsLetExpr ParseLetExpr()
    {
        ConsumeKeyword("let", "HSK2053", "期望 'let' 关键字");

        var declarations = new List<HsAstNode>();
        if (Match(HsTokenType.Delimiter, "{"))
        {
            while (!Check(HsTokenType.Delimiter, "}") && !IsAtEnd())
            {
                var decl = ParseBindingOrSignature();
                if (decl is not null)
                {
                    declarations.Add(decl);
                }

                Match(HsTokenType.Delimiter, ";");
            }

            Consume(HsTokenType.Delimiter, "HSK2054", "期望 '}'");
        }

        ConsumeKeyword("in", "HSK2055", "期望 'in' 关键字");
        var body = ParseExpression();

        return new HsLetExpr(declarations, body);
    }

    private HsDoBlock ParseDoBlock()
    {
        ConsumeKeyword("do", "HSK2056", "期望 'do' 关键字");

        var statements = new List<HsAstNode>();
        if (Match(HsTokenType.Delimiter, "{"))
        {
            while (!Check(HsTokenType.Delimiter, "}") && !IsAtEnd())
            {
                var stmt = ParseDoStatement();
                if (stmt is not null)
                {
                    statements.Add(stmt);
                }

                Match(HsTokenType.Delimiter, ";");
            }

            Consume(HsTokenType.Delimiter, "HSK2057", "期望 '}'");
        }
        else
        {
            var stmt = ParseDoStatement();
            if (stmt is not null)
            {
                statements.Add(stmt);
            }
        }

        return new HsDoBlock(statements);
    }

    private HsAstNode? ParseDoStatement()
    {
        if (Check(HsTokenType.Keyword, "let"))
        {
            Advance();
            var declarations = new List<HsAstNode>();
            if (Match(HsTokenType.Delimiter, "{"))
            {
                while (!Check(HsTokenType.Delimiter, "}") && !IsAtEnd())
                {
                    var decl = ParseBindingOrSignature();
                    if (decl is not null)
                    {
                        declarations.Add(decl);
                    }

                    Match(HsTokenType.Delimiter, ";");
                }

                Consume(HsTokenType.Delimiter, "HSK2058", "期望 '}'");
            }

            return new HsDoLet(declarations);
        }

        var expr = ParseExpression();

        if (Match(HsTokenType.Operator, "<-"))
        {
            var value = ParseExpression();
            return new HsDoBind(expr, value);
        }

        return expr;
    }

    #endregion

    #region Patterns

    private HsAstNode ParsePattern()
    {
        if (Check(HsTokenType.Identifier))
        {
            var name = Peek().Value;
            if (char.IsLower(name[0]))
            {
                Advance();
                return new HsVarPattern(name);
            }

            Advance();
            var fields = new List<HsAstNode>();
            while (Check(HsTokenType.Identifier) || Check(HsTokenType.Delimiter, "(") ||
                   Check(HsTokenType.Delimiter, "[") || Check(HsTokenType.Char) ||
                   Check(HsTokenType.Number) || Check(HsTokenType.String))
                fields.Add(ParsePattern());

            return new HsConPattern(name, fields);
        }

        if (Check(HsTokenType.Number))
        {
            var token = Advance();
            return new HsLiteral("number", Peek().Value);
        }

        if (Check(HsTokenType.String))
        {
            var token = Advance();
            return new HsLiteral("string", Peek().Value);
        }

        if (Check(HsTokenType.Char))
        {
            var token = Advance();
            return new HsLiteral("char", Peek().Value);
        }

        if (Match(HsTokenType.Delimiter, "("))
        {
            var elements = new List<HsAstNode>();
            if (!Check(HsTokenType.Delimiter, ")"))
            {
                elements.Add(ParsePattern());

                while (Match(HsTokenType.Delimiter, ",")) elements.Add(ParsePattern());
            }

            Consume(HsTokenType.Delimiter, "HSK2059", "期望 ')'");
            return elements.Count == 1 ? elements[0] : new HsTuplePattern(elements);
        }

        if (Match(HsTokenType.Delimiter, "["))
        {
            var elements = new List<HsAstNode>();
            if (!Check(HsTokenType.Delimiter, "]"))
            {
                elements.Add(ParsePattern());

                while (Match(HsTokenType.Delimiter, ",")) elements.Add(ParsePattern());
            }

            Consume(HsTokenType.Delimiter, "HSK2060", "期望 ']'");
            return new HsListPattern(elements);
        }

        if (Match(HsTokenType.Operator, "_"))
        {
            return new HsWildCardPattern();
        }

        var errorToken = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default(TextSpan),
            "HSK2061",
            $"期望模式，遇到 '{errorToken.Value}'");

        throw new ParseException($"期望模式，遇到 '{errorToken.Value}'");
    }

    #endregion
}
