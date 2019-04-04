using System.Collections.Immutable;

namespace Std.Data.Text.Rust.Converter;


/// <summary>

///     将 Rust AST 转换为 Nyar IR（IKun）意图节点


/// </summary>
public sealed class RustToIkunConverter
{
    
/// <summary>
    
///     创建转换器
    

/// </summary>
    public RustToIkunConverter(EGraph<IKun>? egraph = null)
    {
        EGraph = egraph ?? new EGraph<IKun>();
    }

    
/// <summary>
    
///     获取内部 EGraph
    

/// </summary>
    public EGraph<IKun> EGraph { get; }

    
/// <summary>
    
///     转换 Rust Crate 为 Nyar IR
    

/// </summary>
    public Id ConvertCrate(RustCrate crate)
    {
        var children = new List<Id>();

        foreach (var item in crate.Items) children.Add(ConvertItem(item));

        var moduleNode = new Mod("__rust_crate__", [..children]);
        return EGraph.Add(moduleNode);
    }

    
/// <summary>
    
///     转换顶层项
    

/// </summary>
    public Id ConvertItem(RustAstNode item)
    {
        return item switch
        {
            RustFunctionDef fn => ConvertFunctionDef(fn),
            RustStructDef structDef => ConvertStructDef(structDef),
            RustEnumDef enumDef => ConvertEnumDef(enumDef),
            RustImplDef implDef => ConvertImplDef(implDef),
            RustTraitDef traitDef => ConvertTraitDef(traitDef),
            RustTypeAlias typeAlias => ConvertTypeAlias(typeAlias),
            RustUseDecl useDecl => ConvertUseDecl(useDecl),
            RustModDecl modDecl => ConvertModDecl(modDecl),
            _ => ConvertStatement(item)
        };
    }

    
/// <summary>
    
///     转换表达式
    

/// </summary>
    public Id ConvertExpression(RustAstNode expr)
    {
        return expr switch
        {
            RustLiteral literal => ConvertLiteral(literal),
            RustIdentifier identifier => ConvertIdentifier(identifier),
            RustBinaryOp binaryOp => ConvertBinaryOp(binaryOp),
            RustUnaryOp unaryOp => ConvertUnaryOp(unaryOp),
            RustCall call => ConvertCall(call),
            RustMethodCall methodCall => ConvertMethodCall(methodCall),
            RustFieldAccess fieldAccess => ConvertFieldAccess(fieldAccess),
            RustIndex index => ConvertIndex(index),
            RustCast cast => ConvertCast(cast),
            RustIfExpr ifExpr => ConvertIfExpr(ifExpr),
            RustBlockExpr blockExpr => ConvertBlockExpr(blockExpr),
            RustArrayExpr arrayExpr => ConvertArrayExpr(arrayExpr),
            RustTupleExpr tupleExpr => ConvertTupleExpr(tupleExpr),
            RustRange range => ConvertRange(range),
            RustClosure closure => ConvertClosure(closure),
            RustMatchExpr matchExpr => ConvertMatchExpr(matchExpr),
            RustMacroCall macroCall => ConvertMacroCall(macroCall),
            _ => throw new NotSupportedException($"不支持的表达式类型: {expr.GetType().Name}")
        };
    }

    
/// <summary>
    
///     转换语句
    

/// </summary>
    public Id ConvertStatement(RustAstNode stmt)
    {
        return stmt switch
        {
            RustExprStmt exprStmt => ConvertExpression(exprStmt.Expression),
            RustLetStmt letStmt => ConvertLetStmt(letStmt),
            RustReturnStmt returnStmt => ConvertReturnStmt(returnStmt),
            RustBreakStmt breakStmt => EGraph.Add(new Trap(EGraph.Add(new Sym("break")))),
            RustContinueStmt => EGraph.Add(new Trap(EGraph.Add(new Sym("continue")))),
            RustWhileStmt whileStmt => ConvertWhileStmt(whileStmt),
            RustLoopStmt loopStmt => ConvertLoopStmt(loopStmt),
            RustForStmt forStmt => ConvertForStmt(forStmt),
            RustIfExpr ifExpr => ConvertIfExpr(ifExpr),
            RustBlockExpr blockExpr => ConvertBlockExpr(blockExpr),
            _ => ConvertExpression(stmt)
        };
    }

    private Id ConvertLiteral(RustLiteral literal)
    {
        var node = literal.Kind switch
        {
            "number" => long.TryParse(literal.Value, out var intValue)
                ? (IKun)new Literal<long>(intValue)
                : new Literal<double>(double.Parse(literal.Value)),
            "string" => new Literal<string>(literal.Value),
            "char" => new Literal<long>(literal.Value.Length > 0 ? literal.Value[0] : 0),
            "bool" => new Literal<bool>(literal.Value == "true"),
            _ => throw new NotSupportedException($"不支持的字面量类型: {literal.Kind}")
        };

        return EGraph.Add(node);
    }

    private Id ConvertIdentifier(RustIdentifier identifier)
    {
        return EGraph.Add(new Sym(identifier.Name));
    }

    private Id ConvertBinaryOp(RustBinaryOp binaryOp)
    {
        var left = ConvertExpression(binaryOp.Left);
        var right = ConvertExpression(binaryOp.Right);

        if (binaryOp.Operator == "=")
        {
            return EGraph.Add(new StateUp(left, right));
        }

        return EGraph.Add(binaryOp.Operator switch
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
            _ => throw new NotSupportedException($"不支持的二元运算符: {binaryOp.Operator}")
        });
    }

    private Id ConvertUnaryOp(RustUnaryOp unaryOp)
    {
        var operand = ConvertExpression(unaryOp.Operand);

        if (unaryOp.Operator == "*")
        {
            return EGraph.Add(new PhysicalNode.Deref(operand));
        }

        if (unaryOp.Operator == "&")
        {
            return EGraph.Add(new PhysicalNode.AddrOf(operand));
        }

        return EGraph.Add(unaryOp.Operator switch
        {
            "-" => (IKun)new Neg(operand),
            "!" => new Not(operand),
            _ => throw new NotSupportedException($"不支持的一元运算符: {unaryOp.Operator}")
        });
    }

    private Id ConvertCall(RustCall call)
    {
        var func = ConvertExpression(call.Function);
        var args = call.Arguments.Select(ConvertExpression).ToImmutableArray();

        return EGraph.Add(new Apply(func, args));
    }

    private Id ConvertMethodCall(RustMethodCall methodCall)
    {
        var receiver = ConvertExpression(methodCall.Receiver);
        var method = EGraph.Add(new Sym(methodCall.Method));
        var args = methodCall.Arguments.Select(ConvertExpression).ToImmutableArray();

        var allArgs = ImmutableArray.Create(receiver).AddRange(args);
        return EGraph.Add(new Apply(method, allArgs));
    }

    private Id ConvertFieldAccess(RustFieldAccess fieldAccess)
    {
        var obj = ConvertExpression(fieldAccess.Object);
        return EGraph.Add(new PhysicalNode.Access(DispatchKind.Dynamic, obj, 0, fieldAccess.Field));
    }

    private Id ConvertIndex(RustIndex index)
    {
        var obj = ConvertExpression(index.Object);
        var idx = ConvertExpression(index.Index);
        return EGraph.Add(new GetOffsetIdx(obj, idx));
    }

    private Id ConvertCast(RustCast cast)
    {
        var expr = ConvertExpression(cast.Expression);
        return EGraph.Add(new StrategyNode.WithConstraint(expr,
            EGraph.Add(new StrategyNode.TypeConstraint(cast.type.ToString() ?? "unknown"))));
    }

    private Id ConvertIfExpr(RustIfExpr ifExpr)
    {
        var condition = ConvertExpression(ifExpr.Condition);
        var thenBranch = ConvertStatement(ifExpr.ThenBranch);
        var elseBranch = ifExpr.ElseBranch is not null
            ? ConvertStatement(ifExpr.ElseBranch)
            : EGraph.Add(new Literal<object?>(null));

        return EGraph.Add(new Choice(condition, thenBranch, elseBranch));
    }

    private Id ConvertBlockExpr(RustBlockExpr blockExpr)
    {
        if (blockExpr.Statements.Count == 0)
        {
            return EGraph.Add(new Literal<object?>(null));
        }

        if (blockExpr.Statements.Count == 1)
        {
            return ConvertStatement(blockExpr.Statements[0]);
        }

        var children = blockExpr.Statements.Select(ConvertStatement).ToImmutableArray();
        return EGraph.Add(new Seq(children));
    }

    private Id ConvertArrayExpr(RustArrayExpr arrayExpr)
    {
        var elements = arrayExpr.elements.Select(ConvertExpression).ToImmutableArray();
        return EGraph.Add(new ArrayLit(elements));
    }

    private Id ConvertTupleExpr(RustTupleExpr tupleExpr)
    {
        var elements = tupleExpr.elements.Select(ConvertExpression).ToImmutableArray();
        return EGraph.Add(new ArrayLit(elements));
    }

    private Id ConvertRange(RustRange range)
    {
        var start = range.Start is not null ? ConvertExpression(range.Start) : EGraph.Add(new Literal<long>(0));
        var end = range.End is not null ? ConvertExpression(range.End) : EGraph.Add(new Literal<object?>(null));

        return EGraph.Add(new Range(start, end));
    }

    private Id ConvertClosure(RustClosure closure)
    {
        var body = ConvertExpression(closure.Body);
        return EGraph.Add(new Lambda([..closure.Parameters], body));
    }

    private Id ConvertMatchExpr(RustMatchExpr matchExpr)
    {
        var scrutinee = ConvertExpression(matchExpr.Scrutinee);
        var result = EGraph.Add(new Literal<object?>(null));

        foreach (var arm in matchExpr.Arms.Reverse())
        {
            var body = ConvertExpression(arm.Body);
            var condition = EGraph.Add(new Cmp(CompareOp.eq, scrutinee, ConvertExpression(arm.Pattern)));
            result = EGraph.Add(new Choice(condition, body, result));
        }

        return result;
    }

    private Id ConvertMacroCall(RustMacroCall macroCall)
    {
        return EGraph.Add(new Trap(EGraph.Add(new Sym($"macro_{macroCall.Name}"))));
    }

    private Id ConvertLetStmt(RustLetStmt letStmt)
    {
        var symbol = ConvertExpression(letStmt.Pattern);

        if (letStmt.Initializer is not null)
        {
            var value = ConvertExpression(letStmt.Initializer);
            return EGraph.Add(new StateUp(symbol, value));
        }

        return EGraph.Add(new StateUp(symbol, EGraph.Add(new Literal<object?>(null))));
    }

    private Id ConvertReturnStmt(RustReturnStmt returnStmt)
    {
        var value = returnStmt.Value is not null ? ConvertExpression(returnStmt.Value) : EGraph.Add(new Literal<object?>(null));
        return EGraph.Add(new Ret(value));
    }

    private Id ConvertWhileStmt(RustWhileStmt whileStmt)
    {
        var condition = ConvertExpression(whileStmt.Condition);
        var body = ConvertStatement(whileStmt.Body);

        return EGraph.Add(new Repeat(condition, body));
    }

    private Id ConvertLoopStmt(RustLoopStmt loopStmt)
    {
        var body = ConvertStatement(loopStmt.Body);
        var condition = EGraph.Add(new Literal<bool>(true));

        return EGraph.Add(new Repeat(condition, body));
    }

    private Id ConvertForStmt(RustForStmt forStmt)
    {
        var iterator = ConvertExpression(forStmt.Iterator);
        var body = ConvertStatement(forStmt.Body);

        return EGraph.Add(new Repeat(iterator, body));
    }

    private Id ConvertFunctionDef(RustFunctionDef funcDef)
    {
        var body = ConvertStatement(funcDef.Body);
        var lambda = EGraph.Add(new Lambda([..funcDef.Parameters.Select(p => p.Name)], body));
        var export = EGraph.Add(new Export(funcDef.Name, lambda));

        return export;
    }

    private Id ConvertStructDef(RustStructDef structDef)
    {
        var fields = structDef.fields.Select(f => EGraph.Add(new Sym(f.Name))).ToImmutableArray();
        var body = EGraph.Add(new Literal<object?>(null));
        var classNode = EGraph.Add(new ClassDef(structDef.Name, ImmutableArray<Id>.Empty, fields, body));
        var export = EGraph.Add(new Export(structDef.Name, classNode));

        return export;
    }

    private Id ConvertEnumDef(RustEnumDef enumDef)
    {
        var fields = enumDef.Variants.Select(v => EGraph.Add(new Sym(v.Name))).ToImmutableArray();
        var body = EGraph.Add(new Literal<object?>(null));
        var classNode = EGraph.Add(new ClassDef(enumDef.Name, ImmutableArray<Id>.Empty, fields, body));
        var export = EGraph.Add(new Export(enumDef.Name, classNode));

        return export;
    }

    private Id ConvertImplDef(RustImplDef implDef)
    {
        var members = implDef.Members.Select(ConvertItem).ToImmutableArray();
        return EGraph.Add(new Seq(members));
    }

    private Id ConvertTraitDef(RustTraitDef traitDef)
    {
        var symbol = EGraph.Add(new Sym(traitDef.Name));
        var value = EGraph.Add(new Meta(EGraph.Add(new Sym("trait"))));

        return EGraph.Add(new StateUp(symbol, value));
    }

    private Id ConvertTypeAlias(RustTypeAlias typeAlias)
    {
        var symbol = EGraph.Add(new Sym(typeAlias.Name));
        var value = EGraph.Add(new Meta(EGraph.Add(new Sym(typeAlias.type.ToString() ?? "unknown"))));

        return EGraph.Add(new StateUp(symbol, value));
    }

    private Id ConvertUseDecl(RustUseDecl useDecl)
    {
        return EGraph.Add(new Import(useDecl.Path, useDecl.Alias ?? useDecl.Path, Array.Empty<string>(), null, null));
    }

    private Id ConvertModDecl(RustModDecl modDecl)
    {
        if (modDecl.Body is not null)
        {
            return ConvertStatement(modDecl.Body);
        }

        return EGraph.Add(new Import(modDecl.Name, modDecl.Name, Array.Empty<string>(), null, null));
    }
}
