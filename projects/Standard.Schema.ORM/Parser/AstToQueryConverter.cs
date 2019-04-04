using Std.Data.Text.Valkyrie.Query;
using Hermes.YYDB.Query;

namespace Hermes.YYDB.Parser;

public sealed class AstToQueryConverter
{
    public QueryExpression Convert(QueryAstNode ast)
    {
        return ast switch
        {
            FindQueryAst find => ConvertFind(find),
            CreateQueryAst create => ConvertCreate(create),
            UpdateQueryAst update => ConvertUpdate(update),
            DeleteQueryAst delete => ConvertDelete(delete),
            AggregateQueryAst aggregate => ConvertAggregate(aggregate),
            _ => throw new InvalidOperationException($"未知的查询 AST 节点类型：{ast.node_kind}")
        };
    }

    private FindQuery ConvertFind(FindQueryAst ast)
    {
        var predicate = ast.predicate != null ? ConvertPredicate(ast.predicate) : null;
        var ordering = ast.ordering != null
            ? new QueryOrdering(ast.ordering.field_name, ast.ordering.descending)
            : null;
        var pagination = ast.pagination != null
            ? new QueryPagination(ast.pagination.offset, ast.pagination.limit)
            : null;

        return new FindQuery(ast.target_type_name, predicate, ordering, pagination);
    }

    private CreateQuery ConvertCreate(CreateQueryAst ast)
    {
        var assignments = ast.assignments.Select(a => new FieldAssignment(a.field_name, a.value)).ToList();

        return new CreateQuery(ast.target_type_name, assignments);
    }

    private UpdateQuery ConvertUpdate(UpdateQueryAst ast)
    {
        var predicate = ConvertPredicate(ast.predicate);
        var assignments = ast.assignments.Select(a => new FieldAssignment(a.field_name, a.value)).ToList();

        return new UpdateQuery(ast.target_type_name, predicate, assignments);
    }

    private DeleteQuery ConvertDelete(DeleteQueryAst ast)
    {
        var predicate = ConvertPredicate(ast.predicate);

        return new DeleteQuery(ast.target_type_name, predicate);
    }

    private AggregateQuery ConvertAggregate(AggregateQueryAst ast)
    {
        var operations = ast.operations.Select(ConvertAggregateOperation).ToList();
        var predicate = ast.predicate != null ? ConvertPredicate(ast.predicate) : null;

        return new AggregateQuery(ast.source_type_name, operations, predicate, ast.group_by);
    }

    private QueryPredicate ConvertPredicate(QueryPredicateAst ast)
    {
        return ast switch
        {
            FieldEqualsAst eq => new FieldEquals(eq.field_name, eq.value),
            FieldGreaterThanAst gt => new FieldGreaterThan(gt.field_name, gt.value),
            FieldLessThanAst lt => new FieldLessThan(lt.field_name, lt.value),
            FieldContainsAst ct => new FieldContains(ct.field_name, ct.value),
            FieldInAst fin => new FieldIn(fin.field_name, fin.values),
            AndPredicateAst and => new AndPredicate(ConvertPredicate(and.left), ConvertPredicate(and.right)),
            OrPredicateAst or => new OrPredicate(ConvertPredicate(or.left), ConvertPredicate(or.right)),
            NotPredicateAst not => new NotPredicate(ConvertPredicate(not.inner)),
            _ => throw new InvalidOperationException($"未知的谓词 AST 节点类型：{ast.predicate_kind}")
        };
    }

    private AggregateOperation ConvertAggregateOperation(AggregateOperationAst ast)
    {
        var function = ast.function switch
        {
            AggregateFunctionAst.count => AggregateFunction.Count,
            AggregateFunctionAst.sum => AggregateFunction.Sum,
            AggregateFunctionAst.avg => AggregateFunction.Avg,
            AggregateFunctionAst.min => AggregateFunction.Min,
            AggregateFunctionAst.max => AggregateFunction.Max,
            AggregateFunctionAst.first => AggregateFunction.First,
            AggregateFunctionAst.last => AggregateFunction.Last,
            AggregateFunctionAst.distinct => AggregateFunction.Distinct,
            _ => throw new InvalidOperationException($"未知的聚合函数：{ast.function}")
        };

        return new AggregateOperation(function, ast.field_name, ast.alias);
    }
}