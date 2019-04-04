namespace Std.Data.Text.CSharp;


/// <summary>

///     C# 语言语法解析器


/// </summary>
public sealed class CsParser : ParserBase<IReadOnlyList<CsToken>, CsAstNode>
{
    private int _current;
    private DiagnosticSink? _diagnostics;
    private IReadOnlyList<CsToken> _tokens = [];

    private static readonly HashSet<string> TypeKeywords =
    [
        new(StringComparer.Ordinal),
        "bool", "byte", "char", "decimal", "double", "float", "int", "long",
        "object", "sbyte", "short", "string", "uint", "ulong", "ushort", "void",
        "var", "dynamic", "nint", "nuint"
    ];

    private static readonly HashSet<string> ModifierKeywords =
    [
        new(StringComparer.Ordinal),
        "abstract", "sealed", "static", "virtual", "override", "readonly",
        "const", "volatile", "extern", "unsafe", "ref", "out", "in",
        "params", "async", "partial", "public", "private", "protected", "internal",
        "new", "required", "file", "scoped", "init", "get", "set"
    ];

    public CsParser(DiagnosticSink? diagnostics = null)
        : base(diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public override CsAstNode Parse(IReadOnlyList<CsToken> tokens)
    {
        _tokens = tokens;
        _current = 0;
        _diagnostics ??= new DiagnosticSink();

        var usings = new List<CsUsingDirective>();
        var declarations = new List<CsAstNode>();

        while (!IsAtEnd())
        {
            SkipNewLines();
            if (IsAtEnd())
            {
                break;
            }

            if (Check(CsNodeKind.Keyword, "using"))
            {
                var usingDir = ParseUsingDirective();
                if (usingDir is not null)
                {
                    usings.Add(usingDir);
                }
            }
            else
            {
                var decl = ParseNamespaceOrTypeDeclaration();
                if (decl is not null)
                {
                    declarations.Add(decl);
                }
            }
        }

        return new CsCompilationUnit(usings, declarations);
    }

    #region Using / Namespace

    private CsUsingDirective? ParseUsingDirective()
    {
        var startToken = Advance();
        var sb = new System.Text.StringBuilder();

        if (Check(CsNodeKind.Keyword, "static"))
        {
            Advance();
        }

        if (Check(CsNodeKind.Identifier) || Check(CsNodeKind.Keyword, "global"))
        {
            sb.Append(Advance().Text);
            while (Match(CsNodeKind.Operator, "."))
            {
                if (Check(CsNodeKind.Identifier) || Check(CsNodeKind.Keyword))
                {
                    sb.Append('.').Append(Advance().Text);
                }
                else
                {
                    break;
                }
            }
        }

        if (!Match(CsNodeKind.Delimiter, ";"))
        {
            Synchronize();
            return null;
        }

        return new CsUsingDirective(sb.ToString(), default);
    }

    private CsAstNode? ParseNamespaceOrTypeDeclaration()
    {
        SkipNewLines();
        if (IsAtEnd())
        {
            return null;
        }

        if (Check(CsNodeKind.Keyword, "namespace"))
        {
            return ParseNamespace();
        }

        return ParseTypeDeclaration();
    }

    private CsAstNode ParseNamespace()
    {
        var startToken = Advance();
        var sb = new System.Text.StringBuilder();
        sb.Append(ConsumeIdentifierOrKeyword("OCS2001", "期望命名空间名称"));

        while (Match(CsNodeKind.Operator, "."))
        {
            sb.Append('.');
            sb.Append(ConsumeIdentifierOrKeyword("OCS2002", "期望命名空间名称"));
        }

        var name = sb.ToString();
        var usings = new List<CsUsingDirective>();
        var declarations = new List<CsAstNode>();

        if (Match(CsNodeKind.Delimiter, "{"))
        {
            while (!Check(CsNodeKind.Delimiter, "}") && !IsAtEnd())
            {
                SkipNewLines();
                if (Check(CsNodeKind.Delimiter, "}"))
                {
                    break;
                }

                if (Check(CsNodeKind.Keyword, "using"))
                {
                    var usingDir = ParseUsingDirective();
                    if (usingDir is not null)
                    {
                        usings.Add(usingDir);
                    }
                }
                else
                {
                    var decl = ParseTypeDeclaration();
                    if (decl is not null)
                    {
                        declarations.Add(decl);
                    }
                }
            }

            Consume(CsNodeKind.Delimiter, "OCS2003", "期望 '}'");
        }
        else
        {
            Consume(CsNodeKind.Delimiter, "OCS2004", "期望 ';'");
        }

        return new CsNamespace(name, usings, declarations, default);
    }

    #endregion

    #region Type Declarations

    private CsAstNode? ParseTypeDeclaration()
    {
        SkipNewLines();
        if (IsAtEnd())
        {
            return null;
        }

        try
        {
            var modifiers = ParseModifiers();

            if (Check(CsNodeKind.Keyword, "class"))
            {
                return ParseClassDecl(modifiers);
            }

            if (Check(CsNodeKind.Keyword, "struct"))
            {
                return ParseStructDecl(modifiers);
            }

            if (Check(CsNodeKind.Keyword, "interface"))
            {
                return ParseInterfaceDecl(modifiers);
            }

            if (Check(CsNodeKind.Keyword, "enum"))
            {
                return ParseEnumDecl(modifiers);
            }

            if (Check(CsNodeKind.Keyword, "record"))
            {
                return ParseRecordDecl(modifiers);
            }

            if (Check(CsNodeKind.Keyword, "delegate"))
            {
                return ParseDelegateDecl(modifiers);
            }

            return ParseFieldOrMethodOrProperty(modifiers);
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    private List<string> ParseModifiers()
    {
        var modifiers = new List<string>();
        while (Check(CsNodeKind.Keyword) && ModifierKeywords.Contains(Peek().Text))
            modifiers.Add(Advance().Text);
        return modifiers;
    }

    private CsAstNode ParseClassDecl(List<string> modifiers)
    {
        var startToken = Advance();
        var name = ConsumeIdentifierOrKeyword("OCS2005", "期望类名");
        ParseTypeArgumentList();
        CsAstNode? baseType = null;
        var interfaces = new List<CsAstNode>();

        if (Match(CsNodeKind.Operator, ":"))
        {
            var firstType = ParseType();
            if (Check(CsNodeKind.Delimiter, ","))
            {
                interfaces.Add(firstType);
                while (Match(CsNodeKind.Delimiter, ",")) interfaces.Add(ParseType());
            }
            else
            {
                baseType = firstType;
                if (Match(CsNodeKind.Delimiter, ","))
                {
                    interfaces.Add(ParseType());
                    while (Match(CsNodeKind.Delimiter, ",")) interfaces.Add(ParseType());
                }
            }
        }

        Consume(CsNodeKind.Delimiter, "OCS2006", "期望 '{'");
        var members = ParseTypeMembers();
        Consume(CsNodeKind.Delimiter, "OCS2007", "期望 '}'");

        return new CsClassDecl(name, baseType, interfaces, members, modifiers, default);
    }

    private CsAstNode ParseStructDecl(List<string> modifiers)
    {
        var startToken = Advance();
        var name = ConsumeIdentifierOrKeyword("OCS2008", "期望结构体名");
        var interfaces = new List<CsAstNode>();

        if (Match(CsNodeKind.Operator, ":"))
        {
            interfaces.Add(ParseType());
            while (Match(CsNodeKind.Delimiter, ",")) interfaces.Add(ParseType());
        }

        Consume(CsNodeKind.Delimiter, "OCS2009", "期望 '{'");
        var members = ParseTypeMembers();
        Consume(CsNodeKind.Delimiter, "OCS2010", "期望 '}'");

        return new CsStructDecl(name, interfaces, members, modifiers, default);
    }

    private CsAstNode ParseInterfaceDecl(List<string> modifiers)
    {
        var startToken = Advance();
        var name = ConsumeIdentifierOrKeyword("OCS2011", "期望接口名");
        var baseInterfaces = new List<CsAstNode>();

        if (Match(CsNodeKind.Operator, ":"))
        {
            baseInterfaces.Add(ParseType());
            while (Match(CsNodeKind.Delimiter, ",")) baseInterfaces.Add(ParseType());
        }

        Consume(CsNodeKind.Delimiter, "OCS2012", "期望 '{'");
        var members = ParseTypeMembers();
        Consume(CsNodeKind.Delimiter, "OCS2013", "期望 '}'");

        return new CsInterfaceDecl(name, baseInterfaces, members, modifiers, default);
    }

    private CsAstNode ParseEnumDecl(List<string> modifiers)
    {
        var startToken = Advance();
        var name = ConsumeIdentifierOrKeyword("OCS2014", "期望枚举名");
        CsAstNode? baseType = null;

        if (Match(CsNodeKind.Operator, ":"))
        {
            baseType = ParseType();
        }

        Consume(CsNodeKind.Delimiter, "OCS2015", "期望 '{'");
        var members = new List<CsEnumMember>();

        while (!Check(CsNodeKind.Delimiter, "}") && !IsAtEnd())
        {
            SkipNewLines();
            if (Check(CsNodeKind.Delimiter, "}"))
            {
                break;
            }

            var memberName = ConsumeIdentifierOrKeyword("OCS2016", "期望枚举成员名");
            CsAstNode? value = null;
            if (Match(CsNodeKind.Operator, "="))
            {
                value = ParseExpression();
            }

            members.Add(new CsEnumMember(memberName, value));
            if (!Check(CsNodeKind.Delimiter, "}"))
            {
                Match(CsNodeKind.Delimiter, ",");
            }
        }

        Consume(CsNodeKind.Delimiter, "OCS2017", "期望 '}'");
        return new CsEnumDecl(name, baseType, members, modifiers, default);
    }

    private CsAstNode ParseRecordDecl(List<string> modifiers)
    {
        var startToken = Advance();
        var name = ConsumeIdentifierOrKeyword("OCS2018", "期望 record 名");
        var parameters = new List<CsParamDecl>();

        if (Match(CsNodeKind.Delimiter, "("))
        {
            if (!Check(CsNodeKind.Delimiter, ")"))
            {
                do
                {
                    var paramMod = ParseParamModifier();
                    var paramType = ParseType();
                    var paramName = ConsumeIdentifierOrKeyword("OCS2019", "期望参数名");
                    CsAstNode? defaultValue = null;
                    if (Match(CsNodeKind.Operator, "="))
                    {
                        defaultValue = ParseExpression();
                    }

                    parameters.Add(new CsParamDecl(paramType, paramName, defaultValue, paramMod));
                } while (Match(CsNodeKind.Delimiter, ","));
            }

            Consume(CsNodeKind.Delimiter, "OCS2020", "期望 ')'");
        }

        CsAstNode? baseType = null;
        if (Match(CsNodeKind.Operator, ":"))
        {
            baseType = ParseType();
        }

        Consume(CsNodeKind.Delimiter, "OCS2021", "期望 '{'");
        var members = ParseTypeMembers();
        Consume(CsNodeKind.Delimiter, "OCS2022", "期望 '}'");

        return new CsRecordDecl(name, parameters, baseType, members, modifiers, default);
    }

    private CsAstNode ParseDelegateDecl(List<string> modifiers)
    {
        var startToken = Advance();
        var returnType = ParseType();
        var name = ConsumeIdentifierOrKeyword("OCS2023", "期望委托名");

        Consume(CsNodeKind.Delimiter, "OCS2024", "期望 '('");
        var parameters = ParseParameterList();
        Consume(CsNodeKind.Delimiter, "OCS2025", "期望 ')'");
        Consume(CsNodeKind.Delimiter, "OCS2026", "期望 ';'");

        return new CsDelegateDecl(returnType, name, parameters, modifiers, default);
    }

    private List<CsAstNode> ParseTypeMembers()
    {
        var members = new List<CsAstNode>();
        while (!Check(CsNodeKind.Delimiter, "}") && !IsAtEnd())
        {
            SkipNewLines();
            if (Check(CsNodeKind.Delimiter, "}"))
            {
                break;
            }

            var member = ParseTypeMember();
            if (member is not null)
            {
                members.Add(member);
            }
        }
        return members;
    }

    private CsAstNode? ParseTypeMember()
    {
        try
        {
            var modifiers = ParseModifiers();

            if (Check(CsNodeKind.Keyword, "class"))
            {
                return ParseClassDecl(modifiers);
            }

            if (Check(CsNodeKind.Keyword, "struct"))
            {
                return ParseStructDecl(modifiers);
            }

            if (Check(CsNodeKind.Keyword, "interface"))
            {
                return ParseInterfaceDecl(modifiers);
            }

            if (Check(CsNodeKind.Keyword, "enum"))
            {
                return ParseEnumDecl(modifiers);
            }

            if (Check(CsNodeKind.Keyword, "record"))
            {
                return ParseRecordDecl(modifiers);
            }

            if (Check(CsNodeKind.Keyword, "delegate"))
            {
                return ParseDelegateDecl(modifiers);
            }

            if (Check(CsNodeKind.Keyword, "event"))
            {
                return ParseEventDecl(modifiers);
            }

            return ParseFieldOrMethodOrProperty(modifiers);
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    private CsAstNode ParseEventDecl(List<string> modifiers)
    {
        var startToken = Advance();
        var type = ParseType();
        var name = ConsumeIdentifierOrKeyword("OCS2027", "期望事件名");
        Consume(CsNodeKind.Delimiter, "OCS2028", "期望 ';'");
        return new CsEventDecl(type, name, modifiers, default);
    }

    private CsAstNode ParseFieldOrMethodOrProperty(List<string> modifiers)
    {
        var startToken = Peek();
        var type = ParseType();
        var name = ConsumeIdentifierOrKeyword("OCS2029", "期望成员名");

        if (Check(CsNodeKind.Delimiter, "("))
        {
            return ParseMethodRest(type, name, modifiers, default);
        }

        if (Check(CsNodeKind.Delimiter, "{"))
        {
            return ParsePropertyRest(type, name, modifiers, default);
        }

        CsAstNode? init = null;
        if (Match(CsNodeKind.Operator, "="))
        {
            init = ParseVariableInitializer();
        }

        Consume(CsNodeKind.Delimiter, "OCS2030", "期望 ';'");

        return new CsFieldDecl(type, name, init, modifiers, default);
    }

    private CsAstNode ParseMethodRest(CsAstNode returnType, string name, List<string> modifiers, CsToken startToken)
    {
        Consume(CsNodeKind.Delimiter, "OCS2031", "期望 '('");
        var parameters = ParseParameterList();
        Consume(CsNodeKind.Delimiter, "OCS2032", "期望 ')'");

        CsAstNode? body = null;
        if (Check(CsNodeKind.Delimiter, "{"))
        {
            body = ParseBlock();
        }
        else if (Match(CsNodeKind.Operator, "=>"))
        {
            body = ParseExpression();
        }
        else
        {
            Consume(CsNodeKind.Delimiter, "OCS2033", "期望 '{' 或 '=>'");
        }

        return new CsMethodDecl(returnType, name, parameters, body, modifiers, default);
    }

    private CsAstNode ParsePropertyRest(CsAstNode type, string name, List<string> modifiers, CsToken startToken)
    {
        CsAstNode? getter = null;
        CsAstNode? setter = null;

        Consume(CsNodeKind.Delimiter, "OCS2034", "期望 '{'");

        while (!Check(CsNodeKind.Delimiter, "}") && !IsAtEnd())
        {
            SkipNewLines();
            if (Check(CsNodeKind.Delimiter, "}"))
            {
                break;
            }

            ParseModifiers();

            if (Check(CsNodeKind.Keyword, "get"))
            {
                Advance();
                if (Check(CsNodeKind.Delimiter, "{"))
                {
                    getter = ParseBlock();
                }
                else
                {
                    Match(CsNodeKind.Delimiter, ";");
                    getter = new CsIdentifier("get");
                }
            }
            else if (Check(CsNodeKind.Keyword, "set"))
            {
                Advance();
                if (Check(CsNodeKind.Delimiter, "{"))
                {
                    setter = ParseBlock();
                }
                else
                {
                    Match(CsNodeKind.Delimiter, ";");
                    setter = new CsIdentifier("set");
                }
            }
            else
            {
                Advance();
            }
        }

        Consume(CsNodeKind.Delimiter, "OCS2035", "期望 '}'");
        return new CsPropertyDecl(type, name, getter, setter, modifiers, default);
    }

    private List<CsParamDecl> ParseParameterList()
    {
        var parameters = new List<CsParamDecl>();

        if (!Check(CsNodeKind.Delimiter, ")"))
        {
            do
            {
                SkipNewLines();
                if (Check(CsNodeKind.Delimiter, ")"))
                {
                    break;
                }

                var mod = ParseParamModifier();
                var paramType = ParseType();
                var paramName = ConsumeIdentifierOrKeyword("OCS2036", "期望参数名");
                CsAstNode? defaultValue = null;
                if (Match(CsNodeKind.Operator, "="))
                {
                    defaultValue = ParseExpression();
                }

                parameters.Add(new CsParamDecl(paramType, paramName, defaultValue, mod));
            } while (Match(CsNodeKind.Delimiter, ","));
        }

        return parameters;
    }

    private string? ParseParamModifier()
    {
        if (Check(CsNodeKind.Keyword, "ref")) { Advance(); return "ref"; }
        if (Check(CsNodeKind.Keyword, "out")) { Advance(); return "out"; }
        if (Check(CsNodeKind.Keyword, "in")) { Advance(); return "in"; }
        if (Check(CsNodeKind.Keyword, "params")) { Advance(); return "params"; }
        return null;
    }

    #endregion

    #region Type Parsing

    private CsAstNode ParseType()
    {
        var startToken = Peek();
        string typeName;

        if (Check(CsNodeKind.Keyword) && TypeKeywords.Contains(Peek().Text))
        {
            typeName = Advance().Text;
        }
        else if (Check(CsNodeKind.Identifier))
        {
            typeName = Advance().Text;
            while (Match(CsNodeKind.Operator, "."))
                typeName += '.' + ConsumeIdentifierOrKeyword("OCS2037", "期望类型名");
        }
        else
        {
            throw new ParseException("期望类型名");
        }

        var typeArgs = ParseTypeArgumentList();
        var isNullable = Match(CsNodeKind.Operator, "?");
        var isArray = false;

        if (Check(CsNodeKind.Delimiter, "["))
        {
            isArray = true;
            while (Match(CsNodeKind.Delimiter, "[")) Match(CsNodeKind.Delimiter, "]");
        }

        return new CsTypeNode(typeName, isNullable, isArray, typeArgs, default);
    }

    private List<CsAstNode>? ParseTypeArgumentList()
    {
        if (!Check(CsNodeKind.Operator, "<"))
        {
            return null;
        }

        Advance();
        var args = new List<CsAstNode>();

        if (!Check(CsNodeKind.Operator, ">") && !Check(CsNodeKind.Delimiter, "}"))
        {
            do { args.Add(ParseType()); } while (Match(CsNodeKind.Delimiter, ","));
        }

        if (!Match(CsNodeKind.Operator, ">"))
        {
            while (!IsAtEnd() && !Check(CsNodeKind.Delimiter, "(") && !Check(CsNodeKind.Delimiter, "{")
                   && !Check(CsNodeKind.Delimiter, ";"))
                Advance();
        }

        return args;
    }

    #endregion

    #region Statements

    private CsAstNode ParseStatement()
    {
        SkipNewLines();

        if (Check(CsNodeKind.Delimiter, "{"))
        {
            return ParseBlock();
        }

        if (Check(CsNodeKind.Keyword, "if"))
        {
            return ParseIfStmt();
        }

        if (Check(CsNodeKind.Keyword, "while"))
        {
            return ParseWhileStmt();
        }

        if (Check(CsNodeKind.Keyword, "do"))
        {
            return ParseDoWhileStmt();
        }

        if (Check(CsNodeKind.Keyword, "for"))
        {
            return ParseForStmt();
        }

        if (Check(CsNodeKind.Keyword, "foreach"))
        {
            return ParseForEachStmt();
        }

        if (Check(CsNodeKind.Keyword, "return"))
        {
            return ParseReturnStmt();
        }

        if (Check(CsNodeKind.Keyword, "break"))
        {
            return ParseBreakStmt();
        }

        if (Check(CsNodeKind.Keyword, "continue"))
        {
            return ParseContinueStmt();
        }

        if (Check(CsNodeKind.Keyword, "throw"))
        {
            return ParseThrowStmt();
        }

        if (Check(CsNodeKind.Keyword, "try"))
        {
            return ParseTryStmt();
        }

        if (Check(CsNodeKind.Keyword, "using") && IsUsingStatement())
        {
            return ParseUsingStmt();
        }

        if (Check(CsNodeKind.Keyword, "lock"))
        {
            return ParseLockStmt();
        }

        if (Check(CsNodeKind.Keyword, "yield"))
        {
            return ParseYieldStmt();
        }

        if (Check(CsNodeKind.Keyword, "switch"))
        {
            return ParseSwitchStmt();
        }

        return ParseLocalDeclarationOrExprStmt();
    }

    private CsAstNode ParseBlock()
    {
        var startToken = Consume(CsNodeKind.Delimiter, "OCS2038", "期望 '{'");
        var statements = new List<CsAstNode>();

        while (!Check(CsNodeKind.Delimiter, "}") && !IsAtEnd())
        {
            SkipNewLines();
            if (Check(CsNodeKind.Delimiter, "}"))
            {
                break;
            }

            statements.Add(ParseStatement());
        }

        Consume(CsNodeKind.Delimiter, "OCS2039", "期望 '}'");
        return new CsBlock(statements, default);
    }

    private CsAstNode ParseIfStmt()
    {
        var startToken = Advance();
        Consume(CsNodeKind.Delimiter, "OCS2040", "期望 '('");
        var condition = ParseExpression();
        Consume(CsNodeKind.Delimiter, "OCS2041", "期望 ')'");
        var thenBody = ParseStatement();
        CsAstNode? elseBody = null;
        if (Match(CsNodeKind.Keyword, "else"))
        {
            elseBody = ParseStatement();
        }

        return new CsIf(condition, thenBody, elseBody, default);
    }

    private CsAstNode ParseWhileStmt()
    {
        var startToken = Advance();
        Consume(CsNodeKind.Delimiter, "OCS2042", "期望 '('");
        var condition = ParseExpression();
        Consume(CsNodeKind.Delimiter, "OCS2043", "期望 ')'");
        var body = ParseStatement();
        return new CsWhile(condition, body, default);
    }

    private CsAstNode ParseDoWhileStmt()
    {
        var startToken = Advance();
        var body = ParseStatement();
        Consume(CsNodeKind.Keyword, "OCS2044", "期望 'while'");
        Consume(CsNodeKind.Delimiter, "OCS2045", "期望 '('");
        var condition = ParseExpression();
        Consume(CsNodeKind.Delimiter, "OCS2046", "期望 ')'");
        Consume(CsNodeKind.Delimiter, "OCS2047", "期望 ';'");
        return new CsDoWhile(body, condition, default);
    }

    private CsAstNode ParseForStmt()
    {
        var startToken = Advance();
        Consume(CsNodeKind.Delimiter, "OCS2048", "期望 '('");
        CsAstNode? init = null;
        if (!Check(CsNodeKind.Delimiter, ";"))
        {
            init = ParseLocalDeclarationOrExprStmt();
        }
        else
        {
            Consume(CsNodeKind.Delimiter, "OCS2049", "期望 ';'");
        }

        CsAstNode? condition = null;
        if (!Check(CsNodeKind.Delimiter, ";"))
        {
            condition = ParseExpression();
        }

        Consume(CsNodeKind.Delimiter, "OCS2050", "期望 ';'");

        CsAstNode? increment = null;
        if (!Check(CsNodeKind.Delimiter, ")"))
        {
            increment = ParseExpression();
        }

        Consume(CsNodeKind.Delimiter, "OCS2051", "期望 ')'");

        var body = ParseStatement();
        return new CsFor(init, condition, increment, body, default);
    }

    private CsAstNode ParseForEachStmt()
    {
        var startToken = Advance();
        Consume(CsNodeKind.Delimiter, "OCS2052", "期望 '('");
        var type = ParseType();
        var varName = ConsumeIdentifierOrKeyword("OCS2053", "期望变量名");
        Consume(CsNodeKind.Keyword, "OCS2054", "期望 'in'");
        var collection = ParseExpression();
        Consume(CsNodeKind.Delimiter, "OCS2055", "期望 ')'");
        var body = ParseStatement();
        return new CsForEach(type, varName, collection, body, default);
    }

    private CsAstNode ParseReturnStmt()
    {
        var startToken = Advance();
        CsAstNode? value = null;
        if (!Check(CsNodeKind.Delimiter, ";"))
        {
            value = ParseExpression();
        }

        Consume(CsNodeKind.Delimiter, "OCS2056", "期望 ';'");
        return new CsReturn(value, default);
    }

    private CsAstNode ParseBreakStmt()
    {
        var startToken = Advance();
        Consume(CsNodeKind.Delimiter, "OCS2057", "期望 ';'");
        return new CsBreak();
    }

    private CsAstNode ParseContinueStmt()
    {
        var startToken = Advance();
        Consume(CsNodeKind.Delimiter, "OCS2058", "期望 ';'");
        return new CsContinue();
    }

    private CsAstNode ParseThrowStmt()
    {
        var startToken = Advance();
        CsAstNode? expr = null;
        if (!Check(CsNodeKind.Delimiter, ";"))
        {
            expr = ParseExpression();
        }

        Consume(CsNodeKind.Delimiter, "OCS2059", "期望 ';'");
        return new CsThrow(expr, default);
    }

    private CsAstNode ParseTryStmt()
    {
        var startToken = Advance();
        var tryBlock = ParseBlock();
        var catches = new List<CsCatchClause>();

        while (Check(CsNodeKind.Keyword, "catch"))
        {
            Advance();
            CsAstNode? exceptionType = null;
            string? exceptionName = null;

            if (Match(CsNodeKind.Delimiter, "("))
            {
                exceptionType = ParseType();
                if (Check(CsNodeKind.Identifier))
                {
                    exceptionName = Advance().Text;
                }

                Consume(CsNodeKind.Delimiter, "OCS2060", "期望 ')'");
            }

            var catchBody = ParseBlock();
            catches.Add(new CsCatchClause(exceptionType, exceptionName, catchBody));
        }

        CsAstNode? finallyBlock = null;
        if (Check(CsNodeKind.Keyword, "finally"))
        {
            Advance();
            finallyBlock = ParseBlock();
        }

        return new CsTry(tryBlock, catches, finallyBlock, default);
    }

    private bool IsUsingStatement()
    {
        var saved = _current;
        Advance();
        var result = Check(CsNodeKind.Identifier) || (Check(CsNodeKind.Keyword) && TypeKeywords.Contains(Peek().Text));
        _current = saved;
        return result;
    }

    private CsAstNode ParseUsingStmt()
    {
        var startToken = Advance();
        var type = ParseType();
        var name = ConsumeIdentifierOrKeyword("OCS2061", "期望变量名");
        CsAstNode? init = null;
        if (Match(CsNodeKind.Operator, "="))
        {
            init = ParseExpression();
        }

        var resource = new CsVarDecl(type, name, init, default);
        var body = ParseBlock();
        return new CsUsingStmt(resource, body, default);
    }

    private CsAstNode ParseLockStmt()
    {
        var startToken = Advance();
        Consume(CsNodeKind.Delimiter, "OCS2062", "期望 '('");
        var expr = ParseExpression();
        Consume(CsNodeKind.Delimiter, "OCS2063", "期望 ')'");
        var body = ParseStatement();
        return new CsLock(expr, body, default);
    }

    private CsAstNode ParseYieldStmt()
    {
        var startToken = Advance();

        if (Check(CsNodeKind.Keyword, "return"))
        {
            Advance();
            var value = ParseExpression();
            Consume(CsNodeKind.Delimiter, "OCS2064", "期望 ';'");
            return new CsYieldReturn(value, default);
        }

        if (Check(CsNodeKind.Keyword, "break"))
        {
            Advance();
            Consume(CsNodeKind.Delimiter, "OCS2065", "期望 ';'");
            return new CsYieldBreak();
        }

        throw new ParseException("yield 后期望 return 或 break");
    }

    private CsAstNode ParseSwitchStmt()
    {
        var startToken = Advance();
        Consume(CsNodeKind.Delimiter, "OCS2066", "期望 '('");
        var expr = ParseExpression();
        Consume(CsNodeKind.Delimiter, "OCS2067", "期望 ')'");
        Consume(CsNodeKind.Delimiter, "OCS2068", "期望 '{'");

        var sections = new List<CsAstNode>();

        while (!Check(CsNodeKind.Delimiter, "}") && !IsAtEnd())
        {
            SkipNewLines();
            if (Check(CsNodeKind.Delimiter, "}"))
            {
                break;
            }

            var labels = new List<CsAstNode>();

            while (Check(CsNodeKind.Keyword, "case") || Check(CsNodeKind.Keyword, "default"))
            {
                if (Match(CsNodeKind.Keyword, "case"))
                {
                    var value = ParseExpression();
                    Consume(CsNodeKind.Operator, "OCS2069", "期望 ':'");
                    labels.Add(new CsCaseLabel(value));
                }
                else
                {
                    Advance();
                    Consume(CsNodeKind.Operator, "OCS2070", "期望 ':'");
                    labels.Add(new CsDefaultLabel());
                }
            }

            var stmts = new List<CsAstNode>();
            while (!Check(CsNodeKind.Keyword, "case") && !Check(CsNodeKind.Keyword, "default")
                   && !Check(CsNodeKind.Delimiter, "}") && !IsAtEnd())
                stmts.Add(ParseStatement());

            sections.Add(new CsSwitchSection(labels, stmts));
        }

        Consume(CsNodeKind.Delimiter, "OCS2071", "期望 '}'");
        return new CsSwitch(expr, sections, default);
    }

    private CsAstNode ParseLocalDeclarationOrExprStmt()
    {
        var startToken = Peek();

        if (IsLocalDeclaration())
        {
            var type = ParseType();
            var name = ConsumeIdentifierOrKeyword("OCS2072", "期望变量名");
            CsAstNode? init = null;
            if (Match(CsNodeKind.Operator, "="))
            {
                init = ParseVariableInitializer();
            }

            Consume(CsNodeKind.Delimiter, "OCS2073", "期望 ';'");
            return new CsVarDecl(type, name, init, default);
        }

        var expr = ParseExpression();
        Consume(CsNodeKind.Delimiter, "OCS2074", "期望 ';'");
        return new CsExprStmt(expr, default);
    }

    private bool IsLocalDeclaration()
    {
        var saved = _current;

        if (Check(CsNodeKind.Keyword, "var"))
        {
            _current = saved + 1;
            var result = Check(CsNodeKind.Identifier);
            _current = saved;
            return result;
        }

        if (Check(CsNodeKind.Keyword) && TypeKeywords.Contains(Peek().Text))
        {
            _current = saved + 1;
            var result = Check(CsNodeKind.Identifier);
            _current = saved;
            return result;
        }

        if (Check(CsNodeKind.Identifier))
        {
            _current = saved + 1;
            if (Check(CsNodeKind.Identifier))
            {
                _current = saved;
                return true;
            }

            if (Check(CsNodeKind.Operator, "<"))
            {
                var depth = 0;
                while (!IsAtEnd())
                {
                    if (Peek().Text == "<")
                    {
                        depth++;
                    }
                    else if (Peek().Text == ">") { depth--; if (depth == 0)
                        {
                            break;
                        }
                    }
                    _current++;
                }

                _current++;
                var result = Check(CsNodeKind.Identifier);
                _current = saved;
                return result;
            }

            _current = saved;
            return false;
        }

        _current = saved;
        return false;
    }

    private CsAstNode ParseVariableInitializer()
    {
        if (Check(CsNodeKind.Delimiter, "{"))
        {
            return ParseInitList();
        }

        return ParseExpression();
    }

    private CsAstNode ParseInitList()
    {
        var startToken = Consume(CsNodeKind.Delimiter, "OCS2075", "期望 '{'");
        var elements = new List<CsAstNode>();

        if (!Check(CsNodeKind.Delimiter, "}"))
        {
            do
            {
                SkipNewLines();
                if (Check(CsNodeKind.Delimiter, "}"))
                {
                    break;
                }

                elements.Add(ParseVariableInitializer());
            } while (Match(CsNodeKind.Delimiter, ","));
        }

        Consume(CsNodeKind.Delimiter, "OCS2076", "期望 '}'");
        return new CsInitList(elements, default);
    }

    #endregion

    #region Expressions

    private CsAstNode ParseExpression()
    {
        return ParseAssignment();
    }

    private CsAstNode ParseAssignment()
    {
        var left = ParseNullCoalesce();

        if (Check(CsNodeKind.Operator) &&
            Peek().Text is "=" or "+=" or "-=" or "*=" or "/=" or "%=" or "&=" or "|=" or "^="
                or "<<=" or ">>=" or "??=" or ">>>=")
        {
            var op = Advance().Text;
            var right = ParseAssignment();
            return new CsBinaryOp(left, op, right);
        }

        return left;
    }

    private CsAstNode ParseNullCoalesce()
    {
        var left = ParseConditional();

        while (Match(CsNodeKind.Operator, "??"))
        {
            var right = ParseConditional();
            left = new CsNullCoalesce(left, right);
        }

        return left;
    }

    private CsAstNode ParseConditional()
    {
        var condition = ParseOr();

        if (Match(CsNodeKind.Operator, "?"))
        {
            var thenExpr = ParseExpression();
            Consume(CsNodeKind.Operator, "OCS2077", "期望 ':'");
            var elseExpr = ParseConditional();
            return new CsTernaryOp(condition, thenExpr, elseExpr);
        }

        return condition;
    }

    private CsAstNode ParseOr()
    {
        var left = ParseAnd();
        while (Match(CsNodeKind.Operator, "||"))
        {
            var right = ParseAnd();
            left = new CsBinaryOp(left, "||", right);
        }
        return left;
    }

    private CsAstNode ParseAnd()
    {
        var left = ParseBitOr();
        while (Match(CsNodeKind.Operator, "&&"))
        {
            var right = ParseBitOr();
            left = new CsBinaryOp(left, "&&", right);
        }
        return left;
    }

    private CsAstNode ParseBitOr()
    {
        var left = ParseBitXor();
        while (Check(CsNodeKind.Operator, "|") && !Check(CsNodeKind.Operator, "||"))
        {
            Advance();
            var right = ParseBitXor();
            left = new CsBinaryOp(left, "|", right);
        }
        return left;
    }

    private CsAstNode ParseBitXor()
    {
        var left = ParseBitAnd();
        while (Match(CsNodeKind.Operator, "^"))
        {
            var right = ParseBitAnd();
            left = new CsBinaryOp(left, "^", right);
        }
        return left;
    }

    private CsAstNode ParseBitAnd()
    {
        var left = ParseEquality();
        while (Check(CsNodeKind.Operator, "&") && !Check(CsNodeKind.Operator, "&&"))
        {
            Advance();
            var right = ParseEquality();
            left = new CsBinaryOp(left, "&", right);
        }
        return left;
    }

    private CsAstNode ParseEquality()
    {
        var left = ParseRelational();
        while (Check(CsNodeKind.Operator) && Peek().Text is "==" or "!=")
        {
            var op = Advance().Text;
            var right = ParseRelational();
            left = new CsBinaryOp(left, op, right);
        }
        return left;
    }

    private CsAstNode ParseRelational()
    {
        var left = ParseShift();

        while (Check(CsNodeKind.Operator) && Peek().Text is "<" or ">" or "<=" or ">=")
        {
            var op = Advance().Text;
            var right = ParseShift();
            left = new CsBinaryOp(left, op, right);
        }

        if (Check(CsNodeKind.Keyword, "is"))
        {
            Advance();
            var type = ParseType();
            left = new CsIs(left, type);
        }
        else if (Check(CsNodeKind.Keyword, "as"))
        {
            Advance();
            var type = ParseType();
            left = new CsAs(left, type);
        }

        return left;
    }

    private CsAstNode ParseShift()
    {
        var left = ParseAdditive();
        while (Check(CsNodeKind.Operator) && Peek().Text is "<<" or ">>" or ">>>")
        {
            var op = Advance().Text;
            var right = ParseAdditive();
            left = new CsBinaryOp(left, op, right);
        }
        return left;
    }

    private CsAstNode ParseAdditive()
    {
        var left = ParseMultiplicative();
        while (Check(CsNodeKind.Operator) && Peek().Text is "+" or "-")
        {
            var op = Advance().Text;
            var right = ParseMultiplicative();
            left = new CsBinaryOp(left, op, right);
        }
        return left;
    }

    private CsAstNode ParseMultiplicative()
    {
        var left = ParseUnary();
        while (Check(CsNodeKind.Operator) && Peek().Text is "*" or "/" or "%")
        {
            var op = Advance().Text;
            var right = ParseUnary();
            left = new CsBinaryOp(left, op, right);
        }
        return left;
    }

    private CsAstNode ParseUnary()
    {
        if (Check(CsNodeKind.Operator) && Peek().Text is "+" or "-" or "!" or "~" or "++" or "--")
        {
            var op = Advance().Text;
            var operand = ParseUnary();
            return new CsUnaryOp(op, operand);
        }

        if (Check(CsNodeKind.Keyword, "await"))
        {
            Advance();
            var operand = ParseUnary();
            return new CsUnaryOp("await", operand);
        }

        if (Check(CsNodeKind.Keyword, "new"))
        {
            return ParseNewExpression();
        }

        if (Check(CsNodeKind.Keyword, "typeof"))
        {
            Advance();
            Consume(CsNodeKind.Delimiter, "OCS2078", "期望 '('");
            var type = ParseType();
            Consume(CsNodeKind.Delimiter, "OCS2079", "期望 ')'");
            return new CsTypeOf(type);
        }

        if (Check(CsNodeKind.Keyword, "default"))
        {
            Advance();
            CsAstNode? type = null;
            if (Match(CsNodeKind.Delimiter, "("))
            {
                if (!Check(CsNodeKind.Delimiter, ")"))
                {
                    type = ParseType();
                }

                Consume(CsNodeKind.Delimiter, "OCS2080", "期望 ')'");
            }
            return new CsDefault(type);
        }

        if (Check(CsNodeKind.Keyword, "sizeof"))
        {
            Advance();
            Consume(CsNodeKind.Delimiter, "OCS2081", "期望 '('");
            var type = ParseType();
            Consume(CsNodeKind.Delimiter, "OCS2082", "期望 ')'");
            return new CsSizeOf(type);
        }

        if (Check(CsNodeKind.Keyword, "nameof"))
        {
            Advance();
            Consume(CsNodeKind.Delimiter, "OCS2083", "期望 '('");
            var expr = ParseExpression();
            Consume(CsNodeKind.Delimiter, "OCS2084", "期望 ')'");
            return new CsNameOf(expr);
        }

        if (Check(CsNodeKind.Operator, "^"))
        {
            Advance();
            var expr = ParseUnary();
            return new CsIndexFromEnd(expr);
        }

        return ParseCastOrPostfix();
    }

    private CsAstNode ParseCastOrPostfix()
    {
        if (Check(CsNodeKind.Delimiter, "("))
        {
            var saved = _current;
            Advance();

            if (IsTypeLookahead())
            {
                try
                {
                    var type = ParseType();
                    if (Check(CsNodeKind.Delimiter, ")"))
                    {
                        Advance();
                        var expr = ParseUnary();
                        return new CsCast(type, expr);
                    }
                }
                catch (ParseException)
                {
                    // 回退
                }
            }

            _current = saved;
        }

        return ParsePostfix();
    }

    private bool IsTypeLookahead()
    {
        if (Check(CsNodeKind.Keyword) && TypeKeywords.Contains(Peek().Text))
        {
            return true;
        }

        if (Check(CsNodeKind.Identifier))
        {
            return true;
        }

        return false;
    }

    private CsAstNode ParseNewExpression()
    {
        var startToken = Advance();
        var type = ParseType();

        if (Check(CsNodeKind.Delimiter, "["))
        {
            var sizes = new List<CsAstNode>();
            Advance();
            if (!Check(CsNodeKind.Delimiter, "]"))
            {
                do { sizes.Add(ParseExpression()); } while (Match(CsNodeKind.Delimiter, ","));
            }
            Consume(CsNodeKind.Delimiter, "OCS2085", "期望 ']'");
            CsAstNode? init = null;
            if (Check(CsNodeKind.Delimiter, "{"))
            {
                init = ParseInitList();
            }

            return new CsNewArray(type, sizes, init, default);
        }

        var args = new List<CsAstNode>();
        if (Match(CsNodeKind.Delimiter, "("))
        {
            if (!Check(CsNodeKind.Delimiter, ")"))
            {
                do { args.Add(ParseExpression()); } while (Match(CsNodeKind.Delimiter, ","));
            }
            Consume(CsNodeKind.Delimiter, "OCS2086", "期望 ')'");
        }

        CsAstNode? initializer = null;
        if (Check(CsNodeKind.Delimiter, "{"))
        {
            initializer = ParseInitList();
        }

        return new CsNewObject(type, args, initializer, default);
    }

    private CsAstNode ParsePostfix()
    {
        var expr = ParsePrimary();

        while (true)
        {
            if (Match(CsNodeKind.Delimiter, "["))
            {
                var index = ParseExpression();
                Consume(CsNodeKind.Delimiter, "OCS2087", "期望 ']'");
                expr = new CsSubscript(expr, index);
            }
            else if (Match(CsNodeKind.Delimiter, "("))
            {
                var args = new List<CsAstNode>();
                if (!Check(CsNodeKind.Delimiter, ")"))
                {
                    do { args.Add(ParseExpression()); } while (Match(CsNodeKind.Delimiter, ","));
                }
                Consume(CsNodeKind.Delimiter, "OCS2088", "期望 ')'");
                expr = new CsCall(expr, args);
            }
            else if (Match(CsNodeKind.Operator, "."))
            {
                var member = ConsumeIdentifierOrKeyword("OCS2089", "期望成员名");
                expr = new CsMemberAccess(expr, member);
            }
            else if (Match(CsNodeKind.Operator, "?."))
            {
                var member = ConsumeIdentifierOrKeyword("OCS2090", "期望成员名");
                expr = new CsConditionalAccess(expr, member);
            }
            else if (Check(CsNodeKind.Operator) && Peek().Text is "++" or "--")
            {
                var op = Advance().Text;
                expr = new CsUnaryOp(op, expr);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private CsAstNode ParsePrimary()
    {
        if (Match(CsNodeKind.Number))
        {
            var token = Previous();
            return new CsLiteral("number", token.Text, default);
        }

        if (Match(CsNodeKind.String))
        {
            var token = Previous();
            return new CsLiteral("string", token.Text, default);
        }

        if (Match(CsNodeKind.Char))
        {
            var token = Previous();
            return new CsLiteral("char", token.Text, default);
        }

        if (Check(CsNodeKind.Keyword, "true") || Check(CsNodeKind.Keyword, "false"))
        {
            var token = Advance();
            return new CsLiteral("bool", token.Text, default);
        }

        if (Check(CsNodeKind.Keyword, "null"))
        {
            var token = Advance();
            return new CsLiteral("null", token.Text, default);
        }

        if (Check(CsNodeKind.Keyword, "this"))
        {
            Advance();
            return new CsThis();
        }

        if (Check(CsNodeKind.Keyword, "base"))
        {
            Advance();
            return new CsBase();
        }

        if (Check(CsNodeKind.Identifier))
        {
            var token = Advance();
            return new CsIdentifier(token.Text, default);
        }

        if (Match(CsNodeKind.Delimiter, "("))
        {
            var expr = ParseExpression();
            Consume(CsNodeKind.Delimiter, "OCS2091", "期望 ')'");
            return expr;
        }

        var errorToken = Peek();
        _diagnostics?.AddError(
            string.Empty,
            default,
            "OCS2092",
            $"意外的标记 '{errorToken.Text}'");

        throw new ParseException($"意外的标记 '{errorToken.Text}'");
    }

    #endregion

    #region Token Access

    private bool IsAtEnd()
    {
        return Peek().Kind == CsNodeKind.Eof;
    }

    private CsToken Peek()
    {
        return _current < _tokens.Count ? _tokens[_current] : _tokens[^1];
    }

    private CsToken PeekNext()
    {
        return _current + 1 < _tokens.Count ? _tokens[_current + 1] : _tokens[^1];
    }

    private CsToken Previous()
    {
        return _tokens[_current - 1];
    }

    private CsToken Advance()
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

    private CsToken Consume(NodeKind type, string errorCode, string message)
    {
        if (Check(type))
        {
            return Advance();
        }

        var token = Peek();
        _diagnostics?.AddError(string.Empty, default, errorCode, message);
        throw new ParseException(message);
    }

    private string ConsumeIdentifierOrKeyword(string errorCode, string message)
    {
        if (Check(CsNodeKind.Identifier) || Check(CsNodeKind.Keyword))
        {
            return Advance().Text;
        }

        var token = Peek();
        _diagnostics?.AddError(string.Empty, default, errorCode, message);
        throw new ParseException(message);
    }

    private void SkipNewLines()
    {
        while (Match(CsNodeKind.NewLine))
        {
        }
    }

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if (Previous().Kind == CsNodeKind.Delimiter && Previous().Text == ";")
            {
                return;
            }

            if (Check(CsNodeKind.Keyword))
            {
                var value = Peek().Text;
                if (value is "class" or "struct" or "interface" or "enum" or "record"
                    or "void" or "int" or "string" or "bool" or "var"
                    or "if" or "while" or "for" or "foreach" or "return"
                    or "using" or "namespace" or "public" or "private" or "protected"
                    or "internal" or "static" or "abstract" or "sealed" or "override")
                {
                    return;
                }
            }

            Advance();
        }
    }

    #endregion
}
