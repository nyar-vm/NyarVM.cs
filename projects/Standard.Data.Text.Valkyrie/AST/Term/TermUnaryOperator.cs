namespace Std.Data.Text.Valkyrie.AST.Term;

public enum TermUnaryOperator
{
    logical_not,
    bitwise_not,
    negate,
    increment,
    decrement,
    /// <summary>
    ///     <c>expr?</c> 后缀运算符，等价于 <c>match expr { case Fine(v) => v; case Fail(e) => return Fail(e) }</c>
    /// </summary>
    try_operator
}