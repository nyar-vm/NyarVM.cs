using System.Text;

namespace Hermes.YYDB.Query;

public sealed class QuerySerializer
{
    public string Serialize(QueryExpression query)
    {
        var sb = new StringBuilder();

        switch (query)
        {
            case FindQuery find:
                SerializeFind(find, sb);
                break;
            case CreateQuery create:
                SerializeCreate(create, sb);
                break;
            case UpdateQuery update:
                SerializeUpdate(update, sb);
                break;
            case DeleteQuery delete:
                SerializeDelete(delete, sb);
                break;
            case AggregateQuery aggregate:
                SerializeAggregate(aggregate, sb);
                break;
            default:
                sb.AppendLine($"// 不支持的查询类型: {query.QueryKind}");
                break;
        }

        return sb.ToString();
    }

    private void SerializeFind(FindQuery query, StringBuilder sb)
    {
        sb.Append($"find {query.TargetTypeName}");

        if (query.Predicate != null) sb.Append($" where {SerializePredicate(query.Predicate)}");

        if (query.Ordering != null)
        {
            var dir = query.Ordering.Descending ? " desc" : "";
            sb.Append($" order by {query.Ordering.FieldName}{dir}");
        }

        if (query.Pagination != null) sb.Append($" skip {query.Pagination.Offset} take {query.Pagination.Limit}");
    }

    private void SerializeCreate(CreateQuery query, StringBuilder sb)
    {
        sb.Append($"create {query.TargetTypeName} {{ ");
        sb.Append(string.Join(", ", query.Assignments.Select(a => $"{a.FieldName} = {SerializeValue(a.Value)}")));
        sb.Append(" }");
    }

    private void SerializeUpdate(UpdateQuery query, StringBuilder sb)
    {
        sb.Append($"update {query.TargetTypeName}");
        sb.Append($" where {SerializePredicate(query.Predicate)}");
        sb.Append(" set { ");
        sb.Append(string.Join(", ", query.Assignments.Select(a => $"{a.FieldName} = {SerializeValue(a.Value)}")));
        sb.Append(" }");
    }

    private void SerializeDelete(DeleteQuery query, StringBuilder sb)
    {
        sb.Append($"delete {query.TargetTypeName}");
        sb.Append($" where {SerializePredicate(query.Predicate)}");
    }

    private void SerializeAggregate(AggregateQuery query, StringBuilder sb)
    {
        sb.Append($"aggregate {query.TargetTypeName}");

        if (query.Predicate != null) sb.Append($" where {SerializePredicate(query.Predicate)}");

        sb.Append(" { ");
        sb.Append(string.Join(", ", query.Operations.Select(SerializeAggregateOp)));
        sb.Append(" }");

        if (query.GroupBy.Count > 0) sb.Append($" group by {string.Join(", ", query.GroupBy)}");
    }

    private string SerializeAggregateOp(AggregateOperation op)
    {
        var funcName = op.Function.ToString().ToLowerInvariant();
        var alias = op.Alias != null ? $" as {op.Alias}" : "";
        return $"{funcName}({op.FieldName}){alias}";
    }

    private string SerializePredicate(QueryPredicate predicate)
    {
        return predicate switch
        {
            FieldEquals eq => $"{eq.FieldName} == {SerializeValue(eq.Value)}",
            FieldGreaterThan gt => $"{gt.FieldName} > {SerializeValue(gt.Value)}",
            FieldLessThan lt => $"{lt.FieldName} < {SerializeValue(lt.Value)}",
            FieldContains ct => $"{ct.FieldName} contains {SerializeValue(ct.Value)}",
            FieldIn fin => $"{fin.FieldName} in [{string.Join(", ", fin.Values.Select(SerializeValue))}]",
            AndPredicate and => $"({SerializePredicate(and.Left)} && {SerializePredicate(and.Right)})",
            OrPredicate or => $"({SerializePredicate(or.Left)} || {SerializePredicate(or.Right)})",
            NotPredicate not => $"!{SerializePredicate(not.Inner)}",
            _ => $"/* unknown predicate: {predicate.PredicateKind} */"
        };
    }

    private static string SerializeValue(object value)
    {
        return value switch
        {
            string s => $"\"{s}\"",
            bool b => b ? "true" : "false",
            null => "null",
            _ => value.ToString() ?? "null"
        };
    }
}