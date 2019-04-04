using System.Linq.Expressions;
using Hermes.YYDB.Query;

namespace Hermes.ORM;

public static class PredicateExtractor
{
    public static QueryPredicate Extract(Expression expression)
    {
        return expression switch
        {
            BinaryExpression binary => ExtractBinary(binary),
            UnaryExpression { NodeType: ExpressionType.Not } unary => new NotPredicate(Extract(unary.Operand)),
            MethodCallExpression methodCall => ExtractMethodCall(methodCall),
            MemberExpression member when member.Type == typeof(bool) => new FieldEquals(GetMemberName(member), true),
            _ => throw new NotSupportedException($"不支持的表达式类型：{expression.NodeType}")
        };
    }

    private static QueryPredicate ExtractBinary(BinaryExpression binary)
    {
        var left = binary.Left;
        var right = binary.Right;

        switch (binary.NodeType)
        {
            case ExpressionType.AndAlso:
                return new AndPredicate(Extract(left), Extract(right));
            case ExpressionType.OrElse:
                return new OrPredicate(Extract(left), Extract(right));
            case ExpressionType.Equal:
                return ExtractComparison(left, right, true);
            case ExpressionType.NotEqual:
                return new NotPredicate(ExtractComparison(left, right, true));
            case ExpressionType.GreaterThan:
                return ExtractComparisonPredicate(left, right, "greater_than");
            case ExpressionType.GreaterThanOrEqual:
                return ExtractComparisonPredicate(left, right, "greater_equal");
            case ExpressionType.LessThan:
                return ExtractComparisonPredicate(left, right, "less_than");
            case ExpressionType.LessThanOrEqual:
                return ExtractComparisonPredicate(left, right, "less_equal");
            default:
                throw new NotSupportedException($"不支持的二元运算符：{binary.NodeType}");
        }
    }

    private static QueryPredicate ExtractComparison(Expression left, Expression right, bool FieldEquals)
    {
        if (TryExtractFieldAndValue(left, right, out var fieldName, out var value))
            return new FieldEquals(fieldName, value!);

        if (TryExtractFieldAndValue(right, left, out fieldName, out value)) return new FieldEquals(fieldName, value!);

        throw new NotSupportedException("无法从比较表达式中提取字段名和值");
    }

    private static QueryPredicate ExtractComparisonPredicate(Expression left, Expression right, string op)
    {
        if (!TryExtractFieldAndValue(left, right, out var fieldName, out var value) &&
            !TryExtractFieldAndValue(right, left, out fieldName, out value))
            throw new NotSupportedException("无法从比较表达式中提取字段名和值");

        return op switch
        {
            "greater_than" => new FieldGreaterThan(fieldName, value!),
            "greater_equal" => new AndPredicate(
                new FieldGreaterThan(fieldName, value!),
                new FieldEquals(fieldName, value!)
            ),
            "less_than" => new FieldLessThan(fieldName, value!),
            "less_equal" => new AndPredicate(
                new FieldLessThan(fieldName, value!),
                new FieldEquals(fieldName, value!)
            ),
            _ => throw new NotSupportedException($"不支持的比较运算符：{op}")
        };
    }

    private static QueryPredicate ExtractMethodCall(MethodCallExpression methodCall)
    {
        if (methodCall.Method.Name == "Contains")
        {
            if (methodCall.Object is MemberExpression member)
            {
                var fieldName = GetMemberName(member);
                var value = ExtractValue(methodCall.Arguments[0]);
                return new FieldContains(fieldName, value!);
            }

            if (methodCall.Arguments is [MemberExpression listMember, _])
            {
                var fieldName =
                    GetMemberName(methodCall.Arguments[1] as MemberExpression ?? throw new NotSupportedException());
                var values = ExtractValue(methodCall.Arguments[0]);
                return new FieldContains(fieldName, values!);
            }
        }

        throw new NotSupportedException($"不支持的方法调用：{methodCall.Method.Name}");
    }

    private static bool TryExtractFieldAndValue(Expression expr1, Expression expr2, out string fieldName,
        out object? value)
    {
        fieldName = "";
        value = null;

        if (expr1 is MemberExpression member1 && !IsParameterAccess(member1))
        {
            fieldName = GetMemberName(member1);
            value = ExtractValue(expr2);
            return true;
        }

        if (expr1 is UnaryExpression { NodeType: ExpressionType.Convert } convert1)
        {
            var inner = convert1.Operand;
            if (inner is MemberExpression innerMember && !IsParameterAccess(innerMember))
            {
                fieldName = GetMemberName(innerMember);
                value = ExtractValue(expr2);
                return true;
            }
        }

        if (expr1 is MemberExpression paramMember && IsParameterAccess(paramMember))
        {
            fieldName = GetMemberName(paramMember);
            value = ExtractValue(expr2);
            return true;
        }

        return false;
    }

    private static bool IsParameterAccess(MemberExpression member)
    {
        return member.Expression is ParameterExpression;
    }

    private static string GetMemberName(MemberExpression member)
    {
        return member.Member.Name;
    }

    private static object? ExtractValue(Expression expression)
    {
        return expression switch
        {
            ConstantExpression constant => constant.Value,
            MemberExpression member => Expression.Lambda(member).Compile().DynamicInvoke(),
            UnaryExpression { NodeType: ExpressionType.Convert } convert => ExtractValue(convert.Operand),
            MethodCallExpression methodCall => Expression.Lambda(methodCall).Compile().DynamicInvoke(),
            _ => Expression.Lambda(expression).Compile().DynamicInvoke()
        };
    }
}