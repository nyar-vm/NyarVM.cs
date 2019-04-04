namespace Std.Data.Text.Valkyrie.Query;

public sealed class QueryParser
{
    private int _current;
    private IReadOnlyList<QueryToken> _tokens = [];

    public QueryAstNode parse(string source)
    {
        var lexer = new QueryLexer();
        _tokens = lexer.tokenize(source);
        _current = 0;

        return parse_query();
    }

    private QueryAstNode parse_query()
    {
        if (is_at_end()) throw new QueryParseException(0, "查询不能为空");

        return current().type switch
        {
            QueryTokenType.keyword_find => parse_find(),
            QueryTokenType.keyword_create => parse_create(),
            QueryTokenType.keyword_update => parse_update(),
            QueryTokenType.keyword_delete => parse_delete(),
            QueryTokenType.keyword_aggregate => parse_aggregate(),
            _ => throw new QueryParseException(current().position,
                $"期望查询关键字（find/create/update/delete/aggregate），得到 {current().value}")
        };
    }

    private FindQueryAst parse_find()
    {
        advance();
        var targetTypeName = expect_identifier();

        QueryPredicateAst? predicate = null;
        if (match(QueryTokenType.keyword_where)) predicate = parse_predicate();

        QueryOrderingAst? ordering = null;
        if (match(QueryTokenType.keyword_order_by))
        {
            var fieldName = expect_identifier();
            var descending = false;
            if (match(QueryTokenType.keyword_desc))
                descending = true;
            else
                match(QueryTokenType.keyword_asc);

            ordering = new QueryOrderingAst(fieldName, descending);
        }

        QueryPaginationAst? pagination = null;
        if (match(QueryTokenType.keyword_skip))
        {
            var offset = expect_number();
            var limit = 100;
            if (match(QueryTokenType.keyword_take)) limit = expect_number();

            pagination = new QueryPaginationAst(offset, limit);
        }
        else if (match(QueryTokenType.keyword_take))
        {
            var limit = expect_number();
            pagination = new QueryPaginationAst(0, limit);
        }

        return new FindQueryAst(targetTypeName, predicate, ordering, pagination);
    }

    private CreateQueryAst parse_create()
    {
        advance();
        var targetTypeName = expect_identifier();
        expect(QueryTokenType.left_brace);
        var assignments = parse_assignments();
        expect(QueryTokenType.right_brace);

        return new CreateQueryAst(targetTypeName, assignments);
    }

    private UpdateQueryAst parse_update()
    {
        advance();
        var targetTypeName = expect_identifier();
        expect(QueryTokenType.keyword_where);
        var predicate = parse_predicate();
        expect(QueryTokenType.keyword_set);
        expect(QueryTokenType.left_brace);
        var assignments = parse_assignments();
        expect(QueryTokenType.right_brace);

        return new UpdateQueryAst(targetTypeName, predicate, assignments);
    }

    private DeleteQueryAst parse_delete()
    {
        advance();
        var targetTypeName = expect_identifier();
        expect(QueryTokenType.keyword_where);
        var predicate = parse_predicate();

        return new DeleteQueryAst(targetTypeName, predicate);
    }

    private AggregateQueryAst parse_aggregate()
    {
        advance();
        var sourceTypeName = expect_identifier();

        QueryPredicateAst? predicate = null;
        if (match(QueryTokenType.keyword_where)) predicate = parse_predicate();

        expect(QueryTokenType.left_brace);
        var operations = parse_aggregate_operations();
        expect(QueryTokenType.right_brace);

        List<string> groupBy = [];
        if (match(QueryTokenType.keyword_group_by))
        {
            expect(QueryTokenType.keyword_order_by);
            groupBy.Add(expect_identifier());
            while (match(QueryTokenType.comma)) groupBy.Add(expect_identifier());
        }

        return new AggregateQueryAst(sourceTypeName, operations, predicate, groupBy);
    }

    private QueryPredicateAst parse_predicate()
    {
        return parse_or_predicate();
    }

    private QueryPredicateAst parse_or_predicate()
    {
        var left = parse_and_predicate();
        while (match(QueryTokenType.or))
        {
            var right = parse_and_predicate();
            left = new OrPredicateAst(left, right);
        }

        return left;
    }

    private QueryPredicateAst parse_and_predicate()
    {
        var left = parse_not_predicate();
        while (match(QueryTokenType.and) || match(QueryTokenType.comma))
        {
            var right = parse_not_predicate();
            left = new AndPredicateAst(left, right);
        }

        return left;
    }

    private QueryPredicateAst parse_not_predicate()
    {
        if (match(QueryTokenType.not)) return new NotPredicateAst(parse_primary_predicate());

        return parse_primary_predicate();
    }

    private QueryPredicateAst parse_primary_predicate()
    {
        if (match(QueryTokenType.left_paren))
        {
            var inner = parse_predicate();
            expect(QueryTokenType.right_paren);
            return inner;
        }

        var fieldName = expect_identifier();

        if (match(QueryTokenType.equals)) return new FieldEqualsAst(fieldName, parse_value());

        if (match(QueryTokenType.not_equals))
        {
            var value = parse_value();
            return new NotPredicateAst(new FieldEqualsAst(fieldName, value));
        }

        if (match(QueryTokenType.greater_than)) return new FieldGreaterThanAst(fieldName, parse_value());

        if (match(QueryTokenType.less_than)) return new FieldLessThanAst(fieldName, parse_value());

        if (match(QueryTokenType.greater_equal))
            return new NotPredicateAst(new FieldLessThanAst(fieldName, parse_value()));

        if (match(QueryTokenType.less_equal))
            return new NotPredicateAst(new FieldGreaterThanAst(fieldName, parse_value()));

        if (match(QueryTokenType.keyword_contains)) return new FieldContainsAst(fieldName, parse_value());

        if (match(QueryTokenType.keyword_in))
        {
            expect(QueryTokenType.left_bracket);
            var values = new List<object> { parse_value() };
            while (match(QueryTokenType.comma)) values.Add(parse_value());

            expect(QueryTokenType.right_bracket);
            return new FieldInAst(fieldName, values);
        }

        throw new QueryParseException(current().position, $"字段 '{fieldName}' 后期望比较运算符，得到 {current().value}");
    }

    private object parse_value()
    {
        if (current().type == QueryTokenType.@string)
        {
            var val = current().value;
            advance();
            return val;
        }

        if (current().type == QueryTokenType.number)
        {
            var val = current().value;
            advance();
            if (val.Contains('.')) return double.Parse(val);

            return int.Parse(val);
        }

        if (current().type == QueryTokenType.identifier)
        {
            var val = current().value;
            if (val.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                advance();
                return true;
            }

            if (val.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                advance();
                return false;
            }

            if (val.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                advance();
                return null!;
            }

            advance();
            return val;
        }

        throw new QueryParseException(current().position, $"期望值，得到 {current().value}");
    }

    private List<FieldAssignmentAst> parse_assignments()
    {
        var assignments = new List<FieldAssignmentAst> { parse_assignment() };
        while (match(QueryTokenType.comma)) assignments.Add(parse_assignment());

        return assignments;
    }

    private FieldAssignmentAst parse_assignment()
    {
        var fieldName = expect_identifier();
        expect(QueryTokenType.equals);
        var value = parse_value();
        return new FieldAssignmentAst(fieldName, value);
    }

    private List<AggregateOperationAst> parse_aggregate_operations()
    {
        var operations = new List<AggregateOperationAst> { parse_aggregate_operation() };
        while (match(QueryTokenType.comma)) operations.Add(parse_aggregate_operation());

        return operations;
    }

    private AggregateOperationAst parse_aggregate_operation()
    {
        var funcName = expect_identifier().ToLowerInvariant();
        var function = funcName switch
        {
            "count" => AggregateFunctionAst.count,
            "sum" => AggregateFunctionAst.sum,
            "avg" => AggregateFunctionAst.avg,
            "min" => AggregateFunctionAst.min,
            "max" => AggregateFunctionAst.max,
            "first" => AggregateFunctionAst.first,
            "last" => AggregateFunctionAst.last,
            "distinct" => AggregateFunctionAst.distinct,
            _ => throw new QueryParseException(current().position, $"未知的聚合函数: {funcName}")
        };

        expect(QueryTokenType.left_paren);
        var fieldName = expect_identifier();
        expect(QueryTokenType.right_paren);

        string? alias = null;
        if (match(QueryTokenType.keyword_as)) alias = expect_identifier();

        return new AggregateOperationAst(function, fieldName, alias);
    }

    private QueryToken current()
    {
        return _current < _tokens.Count ? _tokens[_current] : _tokens[^1];
    }

    private bool is_at_end()
    {
        return current().type == QueryTokenType.eof;
    }

    private bool match(QueryTokenType type)
    {
        if (current().type == type)
        {
            advance();
            return true;
        }

        return false;
    }

    private QueryToken advance()
    {
        if (!is_at_end()) _current++;

        return _tokens[_current - 1];
    }

    private QueryToken expect(QueryTokenType type)
    {
        if (current().type != type)
            throw new QueryParseException(current().position, $"期望 {type}，得到 {current().type}({current().value})");

        return advance();
    }

    private string expect_identifier()
    {
        if (current().type != QueryTokenType.identifier)
            throw new QueryParseException(current().position, $"期望标识符，得到 {current().type}({current().value})");

        var val = current().value;
        advance();
        return val;
    }

    private int expect_number()
    {
        if (current().type != QueryTokenType.number)
            throw new QueryParseException(current().position, $"期望数字，得到 {current().type}({current().value})");

        var val = int.Parse(current().value);
        advance();
        return val;
    }
}