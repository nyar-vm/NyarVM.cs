using System.Text;

namespace Std.Data.Text.Valkyrie.Query;

public sealed class QueryFormatter
{
    public string format(QueryAstNode node)
    {
        var sb = new StringBuilder();

        switch (node)
        {
            case FindQueryAst find:
                format_find(find, sb);
                break;
            case CreateQueryAst create:
                format_create(create, sb);
                break;
            case UpdateQueryAst update:
                format_update(update, sb);
                break;
            case DeleteQueryAst delete:
                format_delete(delete, sb);
                break;
            case AggregateQueryAst aggregate:
                format_aggregate(aggregate, sb);
                break;
            default:
                sb.AppendLine($"// 不支持的查询类型: {node.node_kind}");
                break;
        }

        return sb.ToString();
    }

    private void format_find(FindQueryAst query, StringBuilder sb)
    {
        sb.Append($"find {query.target_type_name}");

        if (query.predicate != null) sb.Append($" where {format_predicate(query.predicate)}");

        if (query.ordering != null)
        {
            var dir = query.ordering.descending ? " desc" : "";
            sb.Append($" order by {query.ordering.field_name}{dir}");
        }

        if (query.pagination != null) sb.Append($" skip {query.pagination.offset} take {query.pagination.limit}");
    }

    private void format_create(CreateQueryAst query, StringBuilder sb)
    {
        sb.Append($"create {query.target_type_name} {{ ");
        sb.Append(string.Join(", ", query.assignments.Select(a => $"{a.field_name} = {format_value(a.value)}")));
        sb.Append(" }");
    }

    private void format_update(UpdateQueryAst query, StringBuilder sb)
    {
        sb.Append($"update {query.target_type_name}");
        sb.Append($" where {format_predicate(query.predicate)}");
        sb.Append(" set { ");
        sb.Append(string.Join(", ", query.assignments.Select(a => $"{a.field_name} = {format_value(a.value)}")));
        sb.Append(" }");
    }

    private void format_delete(DeleteQueryAst query, StringBuilder sb)
    {
        sb.Append($"delete {query.target_type_name}");
        sb.Append($" where {format_predicate(query.predicate)}");
    }

    private void format_aggregate(AggregateQueryAst query, StringBuilder sb)
    {
        sb.Append($"aggregate {query.source_type_name}");

        if (query.predicate != null) sb.Append($" where {format_predicate(query.predicate)}");

        sb.Append(" { ");
        sb.Append(string.Join(", ", query.operations.Select(format_aggregate_op)));
        sb.Append(" }");

        if (query.group_by.Count > 0) sb.Append($" group by {string.Join(", ", query.group_by)}");
    }

    private string format_aggregate_op(AggregateOperationAst op)
    {
        var funcName = op.function.ToString().ToLowerInvariant();
        var alias = op.alias != null ? $" as {op.alias}" : "";
        return $"{funcName}({op.field_name}){alias}";
    }

    private string format_predicate(QueryPredicateAst predicate)
    {
        return predicate switch
        {
            FieldEqualsAst eq => $"{eq.field_name} == {format_value(eq.value)}",
            FieldGreaterThanAst gt => $"{gt.field_name} > {format_value(gt.value)}",
            FieldLessThanAst lt => $"{lt.field_name} < {format_value(lt.value)}",
            FieldContainsAst ct => $"{ct.field_name} contains {format_value(ct.value)}",
            FieldInAst fin => $"{fin.field_name} in [{string.Join(", ", fin.values.Select(format_value))}]",
            AndPredicateAst and => $"({format_predicate(and.left)} && {format_predicate(and.right)})",
            OrPredicateAst or => $"({format_predicate(or.left)} || {format_predicate(or.right)})",
            NotPredicateAst not => $"!{format_predicate(not.inner)}",
            _ => $"/* unknown predicate: {predicate.predicate_kind} */"
        };
    }

    private static string format_value(object value)
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