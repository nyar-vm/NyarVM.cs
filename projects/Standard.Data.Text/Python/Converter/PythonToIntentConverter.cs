using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;

namespace Std.Data.Text.Python.Converter;


/// <summary>

///     将 Python AST 转换为 Nyar IR（IKun）意图节点


/// </summary>
public sealed class PythonToIntentConverter
{
    private int _symbolCounter;

    
/// <summary>
    
///     创建转换器
    

/// </summary>
    public PythonToIntentConverter(EGraph<IKun>? egraph = null)
    {
        EGraph = egraph ?? new EGraph<IKun>();
        _symbolCounter = 0;
    }

    
/// <summary>
    
///     获取内部 EGraph
    

/// </summary>
    public EGraph<IKun> EGraph { get; }

    
/// <summary>
    
///     转换 Python 模块为 Nyar IR
    

/// </summary>
    public Id ConvertModule(PyModule module)
    {
        var children = new List<Id>();

        foreach (var stmt in module.Body) children.Add(ConvertStatement(stmt));

        var moduleNode = new Module("__main__", [..children]);
        return EGraph.Add(moduleNode);
    }

    
/// <summary>
    
///     转换表达式
    

/// </summary>
    public Id ConvertExpression(PyAstNode expr)
    {
        return expr switch
        {
            PyLiteral literal => ConvertLiteral(literal),
            PyIdentifier identifier => ConvertIdentifier(identifier),
            PyBinaryOp binaryOp => ConvertBinaryOp(binaryOp),
            PyUnaryOp unaryOp => ConvertUnaryOp(unaryOp),
            PyCall call => ConvertCall(call),
            PyAttribute attr => ConvertAttribute(attr),
            PySubscript subscript => ConvertSubscript(subscript),
            PyList list => ConvertList(list),
            PyTuple tuple => ConvertTuple(tuple),
            PyLambda lambda => ConvertLambda(lambda),
            _ => throw new NotSupportedException($"不支持的表达式类型: {expr.GetType().Name}")
        };
    }

    
/// <summary>
    
///     转换语句
    

/// </summary>
    public Id ConvertStatement(PyAstNode stmt)
    {
        return stmt switch
        {
            PyExprStmt exprStmt => ConvertExpression(exprStmt.Expression),
            PyAssign assign => ConvertAssign(assign),
            PyAugAssign augAssign => ConvertAugAssign(augAssign),
            PyIf pyIf => ConvertIf(pyIf),
            PyWhile pyWhile => ConvertWhile(pyWhile),
            PyFor pyFor => ConvertFor(pyFor),
            PyReturn pyReturn => ConvertReturn(pyReturn),
            PyYield pyYield => ConvertYield(pyYield),
            PyBreak => EGraph.Add(new Trap(EGraph.Add(new Symbol("__break__")))),
            PyContinue => EGraph.Add(new Trap(EGraph.Add(new Symbol("__continue__")))),
            PyFunctionDef funcDef => ConvertFunctionDef(funcDef),
            PyClassDef classDef => ConvertClassDef(classDef),
            PyTry pyTry => ConvertTry(pyTry),
            PyRaise pyRaise => ConvertRaise(pyRaise),
            PyImport pyImport => ConvertImport(pyImport),
            PyFromImport pyFromImport => ConvertFromImport(pyFromImport),
            PyPass => EGraph.Add(new None()),
            _ => throw new NotSupportedException($"不支持的语句类型: {stmt.GetType().Name}")
        };
    }

    private Id ConvertLiteral(PyLiteral literal)
    {
        var node = literal.Kind switch
        {
            "number" => long.TryParse(literal.Value, out var intValue)
                ? (IKun)new Constant(intValue)
                : new FloatConstant(BitConverter.DoubleToUInt64Bits(double.Parse(literal.Value))),
            "string" => new StringConstant(literal.Value),
            "true" => new BooleanConstant(true),
            "false" => new BooleanConstant(false),
            "none" => new None(),
            _ => throw new NotSupportedException($"不支持的字面量类型: {literal.Kind}")
        };

        return EGraph.Add(node);
    }

    private Id ConvertIdentifier(PyIdentifier identifier)
    {
        return EGraph.Add(new Symbol(identifier.Name));
    }

    private Id ConvertBinaryOp(PyBinaryOp binaryOp)
    {
        var left = ConvertExpression(binaryOp.Left);
        var right = ConvertExpression(binaryOp.Right);
        return CreateOperatorApply(GetBinaryOperatorMemberName(binaryOp.Operator), left, right);
    }

    private Id ConvertUnaryOp(PyUnaryOp unaryOp)
    {
        var operand = ConvertExpression(unaryOp.Operand);
        return CreateOperatorApply(GetUnaryOperatorMemberName(unaryOp.Operator), operand);
    }

    private Id ConvertCall(PyCall call)
    {
        var func = ConvertExpression(call.Function);
        var args = call.Arguments.Select(ConvertExpression).ToImmutableArray();

        return EGraph.Add(new Apply(func, args));
    }

    private Id ConvertAttribute(PyAttribute attr)
    {
        var obj = ConvertExpression(attr.Object);
        return EGraph.Add(new PhysicalNode.Access(DispatchKind.Dynamic, obj, 0, attr.Name));
    }

    private Id ConvertSubscript(PySubscript subscript)
    {
        var obj = ConvertExpression(subscript.Object);
        var index = ConvertExpression(subscript.Index);
        return EGraph.Add(new GetOffsetIdx(obj, index));
    }

    private Id ConvertList(PyList list)
    {
        var elements = list.elements.Select(ConvertExpression).ToImmutableArray();
        return EGraph.Add(new ArrayLiteral(elements));
    }

    private Id ConvertTuple(PyTuple tuple)
    {
        var elements = tuple.elements.Select(ConvertExpression).ToImmutableArray();
        return EGraph.Add(new ArrayLiteral(elements));
    }

    private Id ConvertLambda(PyLambda lambda)
    {
        var body = ConvertExpression(lambda.Body);
        return EGraph.Add(new Lambda([..lambda.Parameters], body));
    }

    private Id ConvertAssign(PyAssign assign)
    {
        var value = ConvertExpression(assign.Value);

        if (assign.Target is PyIdentifier ident)
        {
            var symbol = EGraph.Add(new Symbol(ident.Name));
            return EGraph.Add(new StateUpdate(symbol, value));
        }

        if (assign.Target is PySubscript subscript)
        {
            var obj = ConvertExpression(subscript.Object);
            var index = ConvertExpression(subscript.Index);
            return EGraph.Add(new SetOffsetIdx(obj, index, value));
        }

        throw new NotSupportedException($"不支持的赋值目标类型: {assign.Target.GetType().Name}");
    }

    private Id ConvertAugAssign(PyAugAssign augAssign)
    {
        var value = ConvertExpression(augAssign.Value);

        if (augAssign.Target is PyIdentifier ident)
        {
            var symbol = EGraph.Add(new Symbol(ident.Name));
            var current = EGraph.Add(new Symbol(ident.Name));
            var opResult = CreateOperatorApply(GetAugmentedAssignmentMemberName(augAssign.Operator), current, value);
            return EGraph.Add(new StateUpdate(symbol, opResult));
        }

        if (augAssign.Target is PySubscript subscript)
        {
            var obj = ConvertExpression(subscript.Object);
            var index = ConvertExpression(subscript.Index);
            var current = EGraph.Add(new GetOffsetIdx(obj, index));
            var opResult = CreateOperatorApply(GetAugmentedAssignmentMemberName(augAssign.Operator), current, value);
            return EGraph.Add(new SetOffsetIdx(obj, index, opResult));
        }

        throw new NotSupportedException($"不支持的增量赋值目标类型: {augAssign.Target.GetType().Name}");
    }

    private Id ConvertIf(PyIf pyIf)
    {
        var condition = ConvertExpression(pyIf.Condition);
        var thenBody = ConvertBody(pyIf.ThenBody);
        var elseBody = pyIf.ElseBody is not null ? ConvertBody(pyIf.ElseBody) : EGraph.Add(new None());

        return EGraph.Add(new Choice(condition, thenBody, elseBody));
    }

    private Id ConvertWhile(PyWhile pyWhile)
    {
        var condition = ConvertExpression(pyWhile.Condition);
        var body = ConvertBody(pyWhile.Body);

        return EGraph.Add(new Repeat(condition, body));
    }

    private Id ConvertFor(PyFor pyFor)
    {
        var iterable = ConvertExpression(pyFor.Iterable);

        var lambdaBody = ConvertBody(pyFor.Body);
        var lambda = EGraph.Add(new Lambda([pyFor.Iterator], lambdaBody));

        return EGraph.Add(new Map(lambda, iterable));
    }

    private Id ConvertReturn(PyReturn pyReturn)
    {
        var value = pyReturn.Value is not null ? ConvertExpression(pyReturn.Value) : EGraph.Add(new None());
        return EGraph.Add(new Return(value));
    }

    private Id ConvertYield(PyYield pyYield)
    {
        var value = pyYield.Value is not null ? ConvertExpression(pyYield.Value) : EGraph.Add(new None());
        return EGraph.Add(new Trap(value));
    }

    private Id ConvertTry(PyTry pyTry)
    {
        var body = ConvertBody(pyTry.Body);

        var handlerNodes = new List<Id>();

        foreach (var handler in pyTry.Handlers)
        {
            var handlerBody = ConvertBody(handler.Body);

            var condition = handler.ExceptionType is not null
                ? ConvertExpression(handler.ExceptionType)
                : EGraph.Add(new BooleanConstant(true));

            var catchBlock = handler.Name is not null
                ? EGraph.Add(new Seq([
                    EGraph.Add(new StateUpdate(EGraph.Add(new Symbol(handler.Name)), condition)),
                    handlerBody
                ]))
                : handlerBody;

            handlerNodes.Add(EGraph.Add(new Choice(condition, catchBlock, EGraph.Add(new None()))));
        }

        var catchAll = handlerNodes.Count > 0
            ? handlerNodes.Aggregate((a, b) => EGraph.Add(new Seq([a, b])))
            : EGraph.Add(new None());

        var tryCatch = EGraph.Add(new Choice(EGraph.Add(new BooleanConstant(true)), body, catchAll));

        if (pyTry.ElseBody is not null)
        {
            var elseBody = ConvertBody(pyTry.ElseBody);
            tryCatch = EGraph.Add(new Seq([tryCatch, elseBody]));
        }

        if (pyTry.FinallyBody is not null)
        {
            var finallyBody = ConvertBody(pyTry.FinallyBody);
            tryCatch = EGraph.Add(new Seq([tryCatch, finallyBody]));
        }

        return tryCatch;
    }

    private Id ConvertRaise(PyRaise pyRaise)
    {
        if (pyRaise.Exception is not null)
        {
            var exception = ConvertExpression(pyRaise.Exception);
            return EGraph.Add(new Trap(exception));
        }

        return EGraph.Add(new Trap(EGraph.Add(new Symbol("__re_raise__"))));
    }

    private Id ConvertImport(PyImport pyImport)
    {
        var children = new List<Id>();

        foreach (var item in pyImport.Items) children.Add(EGraph.Add(new Import(item.Name, item.Alias ?? item.Name)));

        if (children.Count == 1) return children[0];

        return EGraph.Add(new Seq([..children]));
    }

    private Id ConvertFromImport(PyFromImport pyFromImport)
    {
        var children = new List<Id>();

        foreach (var item in pyFromImport.Items)
            children.Add(EGraph.Add(new Import(pyFromImport.Module, item.Alias ?? item.Name)));

        if (children.Count == 1) return children[0];

        return EGraph.Add(new Seq([..children]));
    }

    private Id ConvertFunctionDef(PyFunctionDef funcDef)
    {
        var body = ConvertBody(funcDef.Body);
        var lambda = EGraph.Add(new Lambda([..funcDef.Parameters], body));
        var symbol = EGraph.Add(new Symbol(funcDef.Name));
        var export = EGraph.Add(new Export(funcDef.Name, lambda));

        return export;
    }

    private Id ConvertClassDef(PyClassDef classDef)
    {
        var body = ConvertBody(classDef.Body);
        var classNode =
            EGraph.Add(new ClassDef(classDef.Name, ImmutableArray<Id>.Empty, ImmutableArray<Id>.Empty, body));
        var export = EGraph.Add(new Export(classDef.Name, classNode));

        return export;
    }

    private Id ConvertBody(IReadOnlyList<PyAstNode> body)
    {
        if (body.Count == 0) return EGraph.Add(new None());

        if (body.Count == 1) return ConvertStatement(body[0]);

        var children = body.Select(ConvertStatement).ToImmutableArray();
        return EGraph.Add(new Seq(children));
    }

    /// <summary>
    ///     将语法层运算符立即解糖为普通调用，避免在 IR 中保留通用运算符节点。
    /// </summary>
    private Id CreateOperatorApply(string memberName, params Id[] arguments)
    {
        var target = EGraph.Add(new Symbol(memberName));
        return EGraph.Add(new Apply(target, arguments));
    }

    /// <summary>
    ///     将 Python 二元运算符映射为统一的方法名。
    /// </summary>
    private static string GetBinaryOperatorMemberName(string @operator)
    {
        return @operator switch
        {
            "+" => "infix +",
            "-" => "infix -",
            "*" => "infix *",
            "/" => "infix /",
            "//" => "infix //",
            "%" => "infix %",
            "**" => "infix **",
            "==" => "infix ==",
            "!=" => "infix !=",
            "<" => "infix <",
            ">" => "infix >",
            "<=" => "infix <=",
            ">=" => "infix >=",
            "and" => "infix and",
            "or" => "infix or",
            _ => throw new NotSupportedException($"不支持的 Python 二元运算符: {@operator}")
        };
    }

    /// <summary>
    ///     将 Python 一元运算符映射为统一的方法名。
    /// </summary>
    private static string GetUnaryOperatorMemberName(string @operator)
    {
        return @operator switch
        {
            "+" => "prefix +",
            "-" => "prefix -",
            "not" => "prefix not",
            "~" => "prefix ~",
            _ => throw new NotSupportedException($"不支持的 Python 一元运算符: {@operator}")
        };
    }

    /// <summary>
    ///     将复合赋值运算符映射为其基础运算成员名。
    /// </summary>
    private static string GetAugmentedAssignmentMemberName(string @operator)
    {
        return @operator switch
        {
            "+=" => "infix +",
            "-=" => "infix -",
            "*=" => "infix *",
            "/=" => "infix /",
            "//=" => "infix //",
            "%=" => "infix %",
            "**=" => "infix **",
            "&=" => "infix &",
            "|=" => "infix |",
            "^=" => "infix ^",
            "<<=" => "infix <<",
            ">>=" => "infix >>",
            _ => throw new NotSupportedException($"不支持的 Python 复合赋值运算符: {@operator}")
        };
    }
}
