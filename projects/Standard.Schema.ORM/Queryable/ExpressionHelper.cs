using System.Linq.Expressions;

namespace Hermes.ORM;

internal static class ExpressionHelper
{
    public static string GetFieldName(Expression expression)
    {
        return expression switch
        {
            MemberExpression member => member.Member.Name,
            UnaryExpression { NodeType: ExpressionType.Convert } convert => GetFieldName(convert.Operand),
            _ => throw new NotSupportedException($"无法从表达式中提取字段名：{expression.NodeType}")
        };
    }
}