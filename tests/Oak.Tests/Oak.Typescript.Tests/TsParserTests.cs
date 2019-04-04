using Oak.Typescript.AST;
using Oak.Typescript.Lexer;
using Oak.Typescript.Parsing;

namespace Oak.Typescript.Tests;


/// <summary>

///     TsParser 语法分析器单元测试


/// </summary>
public sealed class TsParserTests
{
    private readonly TsLexer _lexer = new();
    private readonly TsParser _parser = new();

    #region 辅助方法

    
/// <summary>
    
///     词法分析 + 语法分析的快捷方法
    

/// </summary>
    private TsAstNode Parse(string source)
    {
        var tokens = _lexer.Tokenize(source);
        return _parser.Parse(tokens);
    }

    #endregion

    #region 变量声明测试

    [Fact]
    public void Parse_ConstDeclaration_ShouldReturnVariableDecl()
    {
        var ast = Parse("const x = 42;");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var varDecl = Assert.IsType<TsVariableDecl>(decl);
        Assert.Equal("x", varDecl.Name);
        Assert.True(varDecl.IsConst);
        Assert.NotNull(varDecl.Initializer);
    }

    [Fact]
    public void Parse_LetDeclaration_ShouldReturnVariableDecl()
    {
        var ast = Parse("let y = 10;");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var varDecl = Assert.IsType<TsVariableDecl>(decl);
        Assert.Equal("y", varDecl.Name);
        Assert.False(varDecl.IsConst);
    }

    [Fact]
    public void Parse_VarDeclaration_ShouldReturnVariableDecl()
    {
        var ast = Parse("var z = 5;");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var varDecl = Assert.IsType<TsVariableDecl>(decl);
        Assert.Equal("z", varDecl.Name);
        Assert.False(varDecl.IsConst);
    }

    [Fact]
    public void Parse_VariableWithTypeAnnotation_ShouldReturnVariableDecl()
    {
        var ast = Parse("const x: number = 42;");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var varDecl = Assert.IsType<TsVariableDecl>(decl);
        Assert.Equal("x", varDecl.Name);
        Assert.NotNull(varDecl.TypeAnnotation);
    }

    [Fact]
    public void Parse_VariableWithoutInitializer_ShouldReturnVariableDecl()
    {
        var ast = Parse("let x;");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var varDecl = Assert.IsType<TsVariableDecl>(decl);
        Assert.Null(varDecl.Initializer);
    }

    #endregion

    #region 函数声明测试

    [Fact]
    public void Parse_FunctionDeclaration_ShouldReturnFunctionDecl()
    {
        var ast = Parse("function add(a, b) { return a; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var funcDecl = Assert.IsType<TsFunctionDecl>(decl);
        Assert.Equal("add", funcDecl.Name);
        Assert.Equal(2, funcDecl.Parameters.Count);
        Assert.False(funcDecl.IsAsync);
        Assert.False(funcDecl.IsGenerator);
    }

    [Fact]
    public void ParseFunctionDeclaration_ShouldSetAsyncFlag()
    {
        var ast = Parse("function fetch() { return 1; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var funcDecl = Assert.IsType<TsFunctionDecl>(decl);
        Assert.False(funcDecl.IsAsync);
    }

    [Fact]
    public void Parse_GeneratorFunctionDeclaration_ShouldSetGeneratorFlag()
    {
        var ast = Parse("function* gen() { yield 1; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var funcDecl = Assert.IsType<TsFunctionDecl>(decl);
        Assert.True(funcDecl.IsGenerator);
    }

    [Fact]
    public void Parse_FunctionWithReturnType_ShouldReturnFunctionDecl()
    {
        var ast = Parse("function add(a: number, b: number): number { return a; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var funcDecl = Assert.IsType<TsFunctionDecl>(decl);
        Assert.NotNull(funcDecl.ReturnType);
    }

    [Fact]
    public void Parse_FunctionWithDefaultParameter_ShouldReturnFunctionDecl()
    {
        var ast = Parse("function greet(name, greeting = 'hello') { return name; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var funcDecl = Assert.IsType<TsFunctionDecl>(decl);
        Assert.Equal(2, funcDecl.Parameters.Count);
        Assert.NotNull(funcDecl.Parameters[1].DefaultValue);
    }

    #endregion

    #region 类声明测试

    [Fact]
    public void Parse_ClassDeclaration_ShouldReturnClassDecl()
    {
        var ast = Parse("class Foo { x; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var classDecl = Assert.IsType<TsClassDecl>(decl);
        Assert.Equal("Foo", classDecl.Name);
    }

    [Fact]
    public void Parse_ClassWithMethod_ShouldReturnClassDecl()
    {
        var ast = Parse("class Foo { bar() { return 1; } }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var classDecl = Assert.IsType<TsClassDecl>(decl);
        Assert.NotEmpty(classDecl.Members);
    }

    [Fact]
    public void Parse_ClassWithPropertyAndMethod_ShouldReturnClassDecl()
    {
        var ast = Parse("class Foo { name: string; greet() { return 1; } }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var classDecl = Assert.IsType<TsClassDecl>(decl);
        Assert.Equal(2, classDecl.Members.Count);
    }

    #endregion

    #region if 语句测试

    [Fact]
    public void Parse_IfStatement_ShouldReturnIfStmt()
    {
        var ast = Parse("if (true) { 1; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var ifStmt = Assert.IsType<TsIfStmt>(decl);
        Assert.NotNull(ifStmt.Condition);
        Assert.Null(ifStmt.ElseBlock);
    }

    [Fact]
    public void Parse_IfElseStatement_ShouldReturnIfStmtWithElse()
    {
        var ast = Parse("if (true) { 1; } else { 2; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var ifStmt = Assert.IsType<TsIfStmt>(decl);
        Assert.NotNull(ifStmt.ElseBlock);
    }

    [Fact]
    public void Parse_IfElseIfElseStatement_ShouldReturnNestedIf()
    {
        var ast = Parse("if (a) { 1; } else if (b) { 2; } else { 3; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var ifStmt = Assert.IsType<TsIfStmt>(decl);
        Assert.NotNull(ifStmt.ElseBlock);
        var elseIf = Assert.IsType<TsIfStmt>(ifStmt.ElseBlock);
        Assert.NotNull(elseIf.ElseBlock);
    }

    #endregion

    #region for/while 语句测试

    [Fact]
    public void Parse_ForStatement_ShouldReturnForStmt()
    {
        var ast = Parse("for (let i = 0; i < 10; i = i + 1) { 1; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var forStmt = Assert.IsType<TsForStmt>(decl);
        Assert.NotNull(forStmt.Init);
        Assert.NotNull(forStmt.Condition);
        Assert.NotNull(forStmt.Increment);
    }

    [Fact]
    public void Parse_WhileStatement_ShouldReturnWhileStmt()
    {
        var ast = Parse("while (true) { 1; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var whileStmt = Assert.IsType<TsWhileStmt>(decl);
        Assert.NotNull(whileStmt.Condition);
    }

    #endregion

    #region do-while 语句测试

    [Fact]
    public void Parse_DoWhileStatement_ShouldReturnDoWhileStmt()
    {
        var ast = Parse("do { 1; } while (true);");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var doWhileStmt = Assert.IsType<TsDoWhileStmt>(decl);
        Assert.NotNull(doWhileStmt.Body);
        Assert.NotNull(doWhileStmt.Condition);
    }

    #endregion

    #region switch 语句测试

    [Fact]
    public void Parse_SwitchStatement_ShouldReturnSwitchStmt()
    {
        var ast = Parse("switch (x) { case 1: break; default: break; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var switchStmt = Assert.IsType<TsSwitchStmt>(decl);
        Assert.Equal(2, switchStmt.Cases.Count);
    }

    [Fact]
    public void Parse_SwitchWithMultipleCases_ShouldReturnSwitchStmt()
    {
        var ast = Parse("switch (x) { case 1: break; case 2: break; default: break; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var switchStmt = Assert.IsType<TsSwitchStmt>(decl);
        Assert.Equal(3, switchStmt.Cases.Count);
    }

    #endregion

    #region try-catch-finally 语句测试

    [Fact]
    public void Parse_TryCatchStatement_ShouldReturnTryStmt()
    {
        var ast = Parse("try { 1; } catch (e) { 2; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var tryStmt = Assert.IsType<TsTryStmt>(decl);
        Assert.NotNull(tryStmt.CatchClause);
        Assert.Null(tryStmt.FinallyBlock);
    }

    [Fact]
    public void Parse_TryFinallyStatement_ShouldReturnTryStmt()
    {
        var ast = Parse("try { 1; } finally { 2; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var tryStmt = Assert.IsType<TsTryStmt>(decl);
        Assert.Null(tryStmt.CatchClause);
        Assert.NotNull(tryStmt.FinallyBlock);
    }

    [Fact]
    public void Parse_TryCatchFinallyStatement_ShouldReturnTryStmt()
    {
        var ast = Parse("try { 1; } catch (e) { 2; } finally { 3; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var tryStmt = Assert.IsType<TsTryStmt>(decl);
        Assert.NotNull(tryStmt.CatchClause);
        Assert.NotNull(tryStmt.FinallyBlock);
    }

    [Fact]
    public void Parse_CatchWithParameterType_ShouldReturnTryStmt()
    {
        var ast = Parse("try { 1; } catch (e: Error) { 2; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var tryStmt = Assert.IsType<TsTryStmt>(decl);
        Assert.NotNull(tryStmt.CatchClause);
        Assert.Equal("e", tryStmt.CatchClause.ParameterName);
        Assert.NotNull(tryStmt.CatchClause.ParameterType);
    }

    #endregion

    #region throw/break/continue 语句测试

    [Fact]
    public void Parse_ThrowStatement_ShouldReturnThrowStmt()
    {
        var ast = Parse("throw new Error();");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var throwStmt = Assert.IsType<TsThrowStmt>(decl);
        Assert.NotNull(throwStmt.Value);
    }

    [Fact]
    public void Parse_BreakStatement_ShouldReturnBreakStmt()
    {
        var ast = Parse("while (true) { break; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var whileStmt = Assert.IsType<TsWhileStmt>(unit.Declarations[0]);
        var block = Assert.IsType<TsBlockStmt>(whileStmt.Body);
        var breakStmt = Assert.IsType<TsBreakStmt>(block.Statements[0]);
        Assert.Null(breakStmt.Label);
    }

    [Fact]
    public void Parse_ContinueStatement_ShouldReturnContinueStmt()
    {
        var ast = Parse("while (true) { continue; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var whileStmt = Assert.IsType<TsWhileStmt>(unit.Declarations[0]);
        var block = Assert.IsType<TsBlockStmt>(whileStmt.Body);
        var continueStmt = Assert.IsType<TsContinueStmt>(block.Statements[0]);
        Assert.Null(continueStmt.Label);
    }

    #endregion

    #region for-in/for-of 语句测试

    [Fact]
    public void Parse_ForInStatement_ShouldReturnForInStmt()
    {
        var ast = Parse("for (const key in obj) { 1; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var forInStmt = Assert.IsType<TsForInStmt>(decl);
        Assert.NotNull(forInStmt.Left);
        Assert.NotNull(forInStmt.Right);
        Assert.NotNull(forInStmt.Body);
    }

    [Fact]
    public void Parse_ForOfStatement_ShouldReturnForOfStmt()
    {
        var ast = Parse("for (const item of arr) { 1; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var forOfStmt = Assert.IsType<TsForOfStmt>(decl);
        Assert.NotNull(forOfStmt.Left);
        Assert.NotNull(forOfStmt.Right);
        Assert.NotNull(forOfStmt.Body);
        Assert.False(forOfStmt.IsAwait);
    }

    [Fact]
    public void Parse_ForAwaitOfStatement_ShouldSetAwaitFlag()
    {
        var ast = Parse("for await (const item of stream) { 1; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var forOfStmt = Assert.IsType<TsForOfStmt>(decl);
        Assert.True(forOfStmt.IsAwait);
    }

    #endregion

    #region this/super 表达式测试

    [Fact]
    public void Parse_ThisExpression_ShouldReturnThisExpr()
    {
        var ast = Parse("this.value");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var exprStmt = Assert.IsType<TsExprStmt>(unit.Declarations[0]);
        Assert.IsType<TsPropertyAccess>(exprStmt.Expression);
        var propAccess = (TsPropertyAccess)exprStmt.Expression;
        Assert.IsType<TsThisExpr>(propAccess.Object);
    }

    [Fact]
    public void Parse_SuperExpression_ShouldReturnSuperExpr()
    {
        var ast = Parse("super.method()");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var exprStmt = Assert.IsType<TsExprStmt>(unit.Declarations[0]);
        var callExpr = Assert.IsType<TsCallExpr>(exprStmt.Expression);
        var propAccess = Assert.IsType<TsPropertyAccess>(callExpr.Callee);
        Assert.IsType<TsSuperExpr>(propAccess.Object);
    }

    #endregion

    #region 函数表达式测试

    [Fact]
    public void Parse_FunctionExpression_ShouldReturnFunctionExpr()
    {
        var ast = Parse("const fn = function(x) { return x; };");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var varDecl = Assert.IsType<TsVariableDecl>(unit.Declarations[0]);
        var funcExpr = Assert.IsType<TsFunctionExpr>(varDecl.Initializer);
        Assert.Single(funcExpr.Parameters);
    }

    [Fact]
    public void Parse_NamedFunctionExpression_ShouldReturnFunctionExpr()
    {
        var ast = Parse("const fn = function myFunc(x) { return x; };");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var varDecl = Assert.IsType<TsVariableDecl>(unit.Declarations[0]);
        var funcExpr = Assert.IsType<TsFunctionExpr>(varDecl.Initializer);
        Assert.Equal("myFunc", funcExpr.Name);
    }

    #endregion

    #region new 表达式测试

    [Fact]
    public void Parse_NewExpression_ShouldReturnNewExpr()
    {
        var ast = Parse("new Foo()");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var exprStmt = Assert.IsType<TsExprStmt>(unit.Declarations[0]);
        var newExpr = Assert.IsType<TsNewExpr>(exprStmt.Expression);
        Assert.IsType<TsIdentifier>(newExpr.Callee);
        Assert.Empty(newExpr.Arguments);
    }

    [Fact]
    public void Parse_NewExpressionWithArgs_ShouldReturnNewExpr()
    {
        var ast = Parse("new Foo(1, 2)");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var exprStmt = Assert.IsType<TsExprStmt>(unit.Declarations[0]);
        var newExpr = Assert.IsType<TsNewExpr>(exprStmt.Expression);
        Assert.Equal(2, newExpr.Arguments.Count);
    }

    #endregion

    #region 箭头函数测试

    [Fact]
    public void Parse_ArrowFunctionWithParens_ShouldReturnArrowFunctionExpr()
    {
        var ast = Parse("(x) => x + 1");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var exprStmt = Assert.IsType<TsExprStmt>(unit.Declarations[0]);
        var arrowExpr = Assert.IsType<TsArrowFunctionExpr>(exprStmt.Expression);
        Assert.Single(arrowExpr.Parameters);
    }

    [Fact]
    public void Parse_ArrowFunctionWithBlockBody_ShouldReturnArrowFunctionExpr()
    {
        var ast = Parse("(x) => { return x; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var exprStmt = Assert.IsType<TsExprStmt>(unit.Declarations[0]);
        var arrowExpr = Assert.IsType<TsArrowFunctionExpr>(exprStmt.Expression);
        Assert.IsType<TsBlockStmt>(arrowExpr.Body);
    }

    [Fact]
    public void Parse_ArrowFunctionSingleParam_ShouldReturnArrowFunctionExpr()
    {
        var ast = Parse("x => x + 1");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var exprStmt = Assert.IsType<TsExprStmt>(unit.Declarations[0]);
        var arrowExpr = Assert.IsType<TsArrowFunctionExpr>(exprStmt.Expression);
        Assert.Single(arrowExpr.Parameters);
        Assert.Equal("x", arrowExpr.Parameters[0].Name);
    }

    [Fact]
    public void Parse_ArrowFunctionWithTypedParam_ShouldReturnArrowFunctionExpr()
    {
        var ast = Parse("(x: number) => x + 1");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var exprStmt = Assert.IsType<TsExprStmt>(unit.Declarations[0]);
        var arrowExpr = Assert.IsType<TsArrowFunctionExpr>(exprStmt.Expression);
        Assert.Single(arrowExpr.Parameters);
        Assert.NotNull(arrowExpr.Parameters[0].TypeAnnotation);
    }

    #endregion

    #region 条件表达式测试

    [Fact]
    public void Parse_ConditionalExpression_ShouldReturnConditionalExpr()
    {
        var ast = Parse("x ? 1 : 2");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var exprStmt = Assert.IsType<TsExprStmt>(unit.Declarations[0]);
        var condExpr = Assert.IsType<TsConditionalExpr>(exprStmt.Expression);
        Assert.NotNull(condExpr.Condition);
        Assert.NotNull(condExpr.ThenBranch);
        Assert.NotNull(condExpr.ElseBranch);
    }

    [Fact]
    public void Parse_NestedConditionalExpression_ShouldReturnConditionalExpr()
    {
        var ast = Parse("a ? b ? 1 : 2 : 3");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var exprStmt = Assert.IsType<TsExprStmt>(unit.Declarations[0]);
        var condExpr = Assert.IsType<TsConditionalExpr>(exprStmt.Expression);
        Assert.IsType<TsConditionalExpr>(condExpr.ThenBranch);
    }

    #endregion

    #region 综合测试

    [Fact]
    public void Parse_MultipleDeclarations_ShouldReturnAll()
    {
        var ast = Parse("const x = 1; let y = 2;");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        Assert.Equal(2, unit.Declarations.Count);
    }

    [Fact]
    public void Parse_ImportDeclaration_ShouldReturnImportDecl()
    {
        var ast = Parse("import { Foo } from 'bar';");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var importDecl = Assert.IsType<TsImportDecl>(decl);
        Assert.Equal("bar", importDecl.ModulePath);
    }

    [Fact]
    public void Parse_ExportDeclaration_ShouldReturnExportDecl()
    {
        var ast = Parse("export const x = 42;");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var exportDecl = Assert.IsType<TsExportDecl>(decl);
        Assert.Equal("x", exportDecl.Name);
    }

    [Fact]
    public void Parse_InterfaceDeclaration_ShouldReturnInterfaceDecl()
    {
        var ast = Parse("interface IFoo { name: string; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var ifaceDecl = Assert.IsType<TsInterfaceDecl>(decl);
        Assert.Equal("IFoo", ifaceDecl.Name);
        Assert.NotEmpty(ifaceDecl.Members);
    }

    [Fact]
    public void Parse_EnumDeclaration_ShouldReturnEnumDecl()
    {
        var ast = Parse("enum Color { Red, Green, Blue }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var enumDecl = Assert.IsType<TsEnumDecl>(decl);
        Assert.Equal("Color", enumDecl.Name);
        Assert.Equal(3, enumDecl.Members.Count);
    }

    [Fact]
    public void Parse_NamespaceDeclaration_ShouldReturnNamespaceDecl()
    {
        var ast = Parse("namespace NS { const x = 1; }");

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        var decl = Assert.Single(unit.Declarations);
        var nsDecl = Assert.IsType<TsNamespaceDecl>(decl);
        Assert.Equal("NS", nsDecl.Name);
        Assert.NotEmpty(nsDecl.Members);
    }

    [Fact]
    public void Parse_ComplexProgram_ShouldReturnCompilationUnit()
    {
        var source = @"
function fibonacci(n: number): number {
    if (n <= 1) {
        return n;
    }
    return fibonacci(n - 1) + fibonacci(n - 2);
}
const result = fibonacci(10);
";

        var ast = Parse(source);

        var unit = Assert.IsType<TsCompilationUnit>(ast);
        Assert.Equal(2, unit.Declarations.Count);

        var funcDecl = Assert.IsType<TsFunctionDecl>(unit.Declarations[0]);
        Assert.Equal("fibonacci", funcDecl.Name);
        Assert.Single(funcDecl.Parameters);

        var varDecl = Assert.IsType<TsVariableDecl>(unit.Declarations[1]);
        Assert.Equal("result", varDecl.Name);
    }

    #endregion
}
