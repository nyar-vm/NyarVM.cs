using System.Collections.Immutable;

namespace Std.Data.Text.C.Converter;


/// <summary>

///     将 C AST 转换为 Nyar IR（IKun）意图节点


/// </summary>
public sealed class CToIntentConverter
{
    
/// <summary>
    
///     创建转换器
    

/// </summary>
    public CToIntentConverter(EGraph<IKun>? egraph = null)
    {
        EGraph = egraph ?? new EGraph<IKun>();
    }

    
/// <summary>
    
///     获取内部 EGraph
    

/// </summary>
    public EGraph<IKun> EGraph { get; }

    
/// <summary>
    
///     转换 C 翻译单元为 Nyar IR
    

/// </summary>
    public Id ConvertTranslationUnit(CTranslationUnit unit)
    {
        var children = new List<Id>();

        foreach (var decl in unit.Declarations) children.Add(ConvertExternalDeclaration(decl));

        var moduleNode = new Mod("__main__", [..children]);
        return EGraph.Add(moduleNode);
    }

    
/// <summary>
    
///     转换外部声明
    

/// </summary>
    public Id ConvertExternalDeclaration(CAstNode decl)
    {
        return decl switch
        {
            CFunctionDef funcDef => ConvertFunctionDef(funcDef),
            CVarDecl varDecl => ConvertVarDecl(varDecl),
            CStructDef structDef => ConvertStructDef(structDef),
            CTypedef typedefDecl => ConvertTypedef(typedefDecl),
            CExprStmt exprStmt => ConvertExpression(exprStmt.Expression),
            _ => throw new NotSupportedException($"不支持的外部声明类型: {decl.GetType().Name}")
        };
    }

    
/// <summary>
    
///     转换表达式
    

/// </summary>
    public Id ConvertExpression(CAstNode expr)
    {
        return expr switch
        {
            CLiteral literal => ConvertLiteral(literal),
            CIdentifier identifier => ConvertIdentifier(identifier),
            CBinaryOp binaryOp => ConvertBinaryOp(binaryOp),
            CUnaryOp unaryOp => ConvertUnaryOp(unaryOp),
            CTernaryOp ternaryOp => ConvertTernaryOp(ternaryOp),
            CCall call => ConvertCall(call),
            CMemberAccess memberAccess => ConvertMemberAccess(memberAccess),
            CSubscript subscript => ConvertSubscript(subscript),
            CCast cast => ConvertCast(cast),
            CSizeOf sizeOf => ConvertSizeOf(sizeOf),
            CInitList initList => ConvertInitList(initList),
            _ => throw new NotSupportedException($"不支持的表达式类型: {expr.GetType().Name}")
        };
    }

    
/// <summary>
    
///     转换语句
    

/// </summary>
    public Id ConvertStatement(CAstNode stmt)
    {
        return stmt switch
        {
            CExprStmt exprStmt => ConvertExpression(exprStmt.Expression),
            CVarDecl varDecl => ConvertVarDecl(varDecl),
            CIf cIf => ConvertIf(cIf),
            CWhile cWhile => ConvertWhile(cWhile),
            CDoWhile cDoWhile => ConvertDoWhile(cDoWhile),
            CFor cFor => ConvertFor(cFor),
            CReturn cReturn => ConvertReturn(cReturn),
            CBreak => EGraph.Add(new Trap(EGraph.Add(new Literal<object?>(null)))),
            CContinue => EGraph.Add(new Trap(EGraph.Add(new Literal<object?>(null)))),
            CGoto cGoto => ConvertGoto(cGoto),
            CLabel cLabel => ConvertLabel(cLabel),
            CSwitch cSwitch => ConvertSwitch(cSwitch),
            CCompound compound => ConvertCompound(compound),
            _ => throw new NotSupportedException($"不支持的语句类型: {stmt.GetType().Name}")
        };
    }

    private Id ConvertLiteral(CLiteral literal)
    {
        var node = literal.Kind switch
        {
            "number" => long.TryParse(literal.Value, out var intValue)
                ? (IKun)new Literal<long>(intValue)
                : new Literal<double>(double.Parse(literal.Value)),
            "string" => new Literal<string>(literal.Value),
            "char" => new Literal<long>(literal.Value.Length > 0 ? literal.Value[0] : 0),
            _ => throw new NotSupportedException($"不支持的字面量类型: {literal.Kind}")
        };

        return EGraph.Add(node);
    }

    private Id ConvertIdentifier(CIdentifier identifier)
    {
        return EGraph.Add(new Sym(identifier.Name));
    }

    private Id ConvertBinaryOp(CBinaryOp binaryOp)
    {
        var left = ConvertExpression(binaryOp.Left);
        var right = ConvertExpression(binaryOp.Right);

        var op = binaryOp.Operator switch
        {
            "+" => "+",
            "-" => "-",
            "*" => "*",
            "/" => "/",
            "%" => "%",
            "==" => "==",
            "!=" => "!=",
            "<" => "<",
            ">" => ">",
            "<=" => "<=",
            ">=" => ">=",
            "&&" => "&&",
            "||" => "||",
            "&" => "&",
            "|" => "|",
            "^" => "^",
            "<<" => "<<",
            ">>" => ">>",
            "=" => "=",
            "+=" => "+=",
            "-=" => "-=",
            "*=" => "*=",
            "/=" => "/=",
            "%=" => "%=",
            "&=" => "&=",
            "|=" => "|=",
            "^=" => "^=",
            "<<=" => "<<=",
            ">>=" => ">>=",
            _ => binaryOp.Operator
        };

        if (op == "=")
        {
            return EGraph.Add(new StateUp(left, right));
        }

        return EGraph.Add(op switch
        {
            "+" => (IKun)new Add(left, right),
            "-" => new Sub(left, right),
            "*" => new Mul(left, right),
            "/" => new Div(left, right),
            "%" => new Rem(left, right),
            "==" => new Cmp(CompareOp.eq, left, right),
            "!=" => new Cmp(CompareOp.ne, left, right),
            "<" => new Cmp(CompareOp.lt, left, right),
            "<=" => new Cmp(CompareOp.le, left, right),
            ">" => new Cmp(CompareOp.gt, left, right),
            ">=" => new Cmp(CompareOp.ge, left, right),
            "&&" => (IKun)new And(left, right),
            "||" => new Or(left, right),
            _ => throw new NotSupportedException($"不支持的二元运算符: {op}")
        });
    }

    private Id ConvertUnaryOp(CUnaryOp unaryOp)
    {
        var operand = ConvertExpression(unaryOp.Operand);

        var op = unaryOp.Operator switch
        {
            "+" => "+",
            "-" => "-",
            "!" => "!",
            "~" => "~",
            "*" => "deref",
            "&" => "addrof",
            "++" => "++",
            "--" => "--",
            _ => unaryOp.Operator
        };

        if (op == "deref") return EGraph.Add(new PhysicalNode.Deref(operand));

        if (op == "addrof") return EGraph.Add(new PhysicalNode.AddrOf(operand));

        return EGraph.Add(op switch
        {
            "-" => (IKun)new Neg(operand),
            "!" => new Not(operand),
            _ => throw new NotSupportedException($"不支持的一元运算符: {op}")
        });
    }

    private Id ConvertTernaryOp(CTernaryOp ternaryOp)
    {
        var condition = ConvertExpression(ternaryOp.Condition);
        var thenExpr = ConvertExpression(ternaryOp.ThenExpr);
        var elseExpr = ConvertExpression(ternaryOp.ElseExpr);

        return EGraph.Add(new Choice(condition, thenExpr, elseExpr));
    }

    private Id ConvertCall(CCall call)
    {
        var func = ConvertExpression(call.Function);
        var args = call.Arguments.Select(ConvertExpression).ToImmutableArray();

        return EGraph.Add(new Apply(func, args));
    }

    private Id ConvertMemberAccess(CMemberAccess memberAccess)
    {
        var obj = ConvertExpression(memberAccess.Object);

        if (memberAccess.IsPointer)
        {
            var deref = EGraph.Add(new PhysicalNode.Deref(obj));
            return EGraph.Add(new PhysicalNode.Access(DispatchKind.Dynamic, deref, 0, memberAccess.Member));
        }

        return EGraph.Add(new PhysicalNode.Access(DispatchKind.Dynamic, obj, 0, memberAccess.Member));
    }

    private Id ConvertSubscript(CSubscript subscript)
    {
        var obj = ConvertExpression(subscript.Object);
        var index = ConvertExpression(subscript.Index);

        return EGraph.Add(new GetOffsetIdx(obj, index));
    }

    private Id ConvertCast(CCast cast)
    {
        var expr = ConvertExpression(cast.Expression);
        return EGraph.Add(new StrategyNode.WithConstraint(expr,
            EGraph.Add(new StrategyNode.TypeConstraint(cast.type.ToString() ?? "unknown"))));
    }

    private Id ConvertSizeOf(CSizeOf sizeOf)
    {
        return EGraph.Add(new Literal<long>(8));
    }

    private Id ConvertInitList(CInitList initList)
    {
        var elements = initList.elements.Select(ConvertExpression).ToImmutableArray();
        return EGraph.Add(new ArrayLit(elements));
    }

    private Id ConvertVarDecl(CVarDecl varDecl)
    {
        var symbol = EGraph.Add(new Sym(varDecl.Name));

        if (varDecl.Initializer is not null)
        {
            var value = ConvertExpression(varDecl.Initializer);
            return EGraph.Add(new StateUp(symbol, value));
        }

        return EGraph.Add(new StateUp(symbol, EGraph.Add(new Literal<object?>(null))));
    }

    private Id ConvertIf(CIf cIf)
    {
        var condition = ConvertExpression(cIf.Condition);
        var thenBody = ConvertStatement(cIf.ThenBody);
        var elseBody = cIf.ElseBody is not null ? ConvertStatement(cIf.ElseBody) : EGraph.Add(new Literal<object?>(null));

        return EGraph.Add(new Choice(condition, thenBody, elseBody));
    }

    private Id ConvertWhile(CWhile cWhile)
    {
        var condition = ConvertExpression(cWhile.Condition);
        var body = ConvertStatement(cWhile.Body);

        return EGraph.Add(new Repeat(condition, body));
    }

    private Id ConvertDoWhile(CDoWhile cDoWhile)
    {
        var body = ConvertStatement(cDoWhile.Body);
        var condition = ConvertExpression(cDoWhile.Condition);

        var seq = EGraph.Add(new Seq([body, EGraph.Add(new Repeat(condition, body))]));
        return seq;
    }

    private Id ConvertFor(CFor cFor)
    {
        var init = cFor.Init is not null ? ConvertStatement(cFor.Init) : EGraph.Add(new Literal<object?>(null));
        var condition = cFor.Condition is not null
            ? ConvertExpression(cFor.Condition)
            : EGraph.Add(new Literal<bool>(true));
        var increment = cFor.Increment is not null ? ConvertExpression(cFor.Increment) : EGraph.Add(new Literal<object?>(null));
        var body = ConvertStatement(cFor.Body);

        var loopBody = EGraph.Add(new Seq([body, increment]));
        var loop = EGraph.Add(new Repeat(condition, loopBody));

        return EGraph.Add(new Seq([init, loop]));
    }

    private Id ConvertReturn(CReturn cReturn)
    {
        var value = cReturn.Value is not null ? ConvertExpression(cReturn.Value) : EGraph.Add(new Literal<object?>(null));
        return EGraph.Add(new Ret(value));
    }

    private Id ConvertGoto(CGoto cGoto)
    {
        return EGraph.Add(new Trap(EGraph.Add(new Sym($"goto_{cGoto.Label}"))));
    }

    private Id ConvertLabel(CLabel cLabel)
    {
        var stmt = ConvertStatement(cLabel.Statement);
        return EGraph.Add(new StrategyNode.WithContext(stmt,
            EGraph.Add(new Meta(EGraph.Add(new Sym($"label_{cLabel.Name}"))))));
    }

    private Id ConvertSwitch(CSwitch cSwitch)
    {
        var expr = ConvertExpression(cSwitch.Expression);
        var result = EGraph.Add(new Literal<object?>(null));

        foreach (var caseNode in cSwitch.Cases.Reverse())
        {
            if (caseNode is CCase cCase)
            {
                var body = ConvertBody(cCase.Body);

                if (cCase.Value is not null)
                {
                    var caseValue = ConvertExpression(cCase.Value);
                    var condition = EGraph.Add(new Cmp(CompareOp.eq, expr, caseValue));
                    result = EGraph.Add(new Choice(condition, body, result));
                }
                else
                {
                    result = body;
                }
            }
        }

        return result;
    }

    private Id ConvertCompound(CCompound compound)
    {
        if (compound.Statements.Count == 0)
        {
            return EGraph.Add(new Literal<object?>(null));
        }

        if (compound.Statements.Count == 1)
        {
            return ConvertStatement(compound.Statements[0]);
        }

        var children = compound.Statements.Select(ConvertStatement).ToImmutableArray();
        return EGraph.Add(new Seq(children));
    }

    private Id ConvertFunctionDef(CFunctionDef funcDef)
    {
        var body = ConvertStatement(funcDef.Body);
        var lambda = EGraph.Add(new Lambda([..funcDef.Parameters.Select(p => p.Name)], body));
        var export = EGraph.Add(new Export(funcDef.Name, lambda));

        return export;
    }

    private Id ConvertStructDef(CStructDef structDef)
    {
        var fields = structDef.fields.Select(f => EGraph.Add(new Sym(f.Name))).ToImmutableArray();
        var body = EGraph.Add(new Literal<object?>(null));
        var classNode = EGraph.Add(new ClassDef(structDef.Name ?? $"anon_struct_{Guid.NewGuid():N}", ImmutableArray<Id>.Empty,
            fields, body));
        var export = EGraph.Add(new Export(structDef.Name ?? "", classNode));

        return export;
    }

    private Id ConvertTypedef(CTypedef typedefDecl)
    {
        var symbol = EGraph.Add(new Sym(typedefDecl.Name));
        var value = EGraph.Add(new Meta(EGraph.Add(new Sym(typedefDecl.type.ToString() ?? "unknown"))));

        return EGraph.Add(new StateUp(symbol, value));
    }

    private Id ConvertBody(IReadOnlyList<CAstNode> body)
    {
        if (body.Count == 0)
        {
            return EGraph.Add(new Literal<object?>(null));
        }

        if (body.Count == 1)
        {
            return ConvertStatement(body[0]);
        }

        var children = body.Select(ConvertStatement).ToImmutableArray();
        return EGraph.Add(new Seq(children));
    }
}
