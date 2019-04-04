using System.Text;

namespace Std.Data.Text.Valkyrie.Query;

public sealed class QueryLexer
{
    private static readonly Dictionary<string, QueryTokenType> _keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["find"] = QueryTokenType.keyword_find,
        ["create"] = QueryTokenType.keyword_create,
        ["update"] = QueryTokenType.keyword_update,
        ["delete"] = QueryTokenType.keyword_delete,
        ["aggregate"] = QueryTokenType.keyword_aggregate,
        ["where"] = QueryTokenType.keyword_where,
        ["set"] = QueryTokenType.keyword_set,
        ["order"] = QueryTokenType.keyword_order_by,
        ["by"] = QueryTokenType.keyword_order_by,
        ["skip"] = QueryTokenType.keyword_skip,
        ["take"] = QueryTokenType.keyword_take,
        ["group"] = QueryTokenType.keyword_group_by,
        ["as"] = QueryTokenType.keyword_as,
        ["in"] = QueryTokenType.keyword_in,
        ["contains"] = QueryTokenType.keyword_contains,
        ["desc"] = QueryTokenType.keyword_desc,
        ["asc"] = QueryTokenType.keyword_asc,
        ["and"] = QueryTokenType.and,
        ["or"] = QueryTokenType.or,
        ["not"] = QueryTokenType.not
    };

    public IReadOnlyList<QueryToken> tokenize(string source)
    {
        var tokens = new List<QueryToken>();
        var pos = 0;

        while (pos < source.Length)
        {
            var c = source[pos];

            if (char.IsWhiteSpace(c))
            {
                pos++;
                continue;
            }

            if (c == '/' && pos + 1 < source.Length && source[pos + 1] == '/')
            {
                while (pos < source.Length && source[pos] != '\n') pos++;

                continue;
            }

            if (c == '"')
            {
                var sb = new StringBuilder();
                pos++;
                while (pos < source.Length && source[pos] != '"')
                {
                    if (source[pos] == '\\' && pos + 1 < source.Length)
                    {
                        pos++;
                        sb.Append(source[pos] switch
                        {
                            'n' => '\n',
                            't' => '\t',
                            'r' => '\r',
                            _ => source[pos]
                        });
                    }
                    else
                    {
                        sb.Append(source[pos]);
                    }

                    pos++;
                }

                pos++;
                tokens.Add(new QueryToken(QueryTokenType.@string, sb.ToString(), pos));
                continue;
            }

            if (char.IsDigit(c) || (c == '-' && pos + 1 < source.Length && char.IsDigit(source[pos + 1])))
            {
                var start = pos;
                if (c == '-') pos++;

                while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '.')) pos++;

                tokens.Add(new QueryToken(QueryTokenType.number, source[start..pos], start));
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                var start = pos;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_')) pos++;

                var word = source[start..pos];
                var tokenType = _keywords.GetValueOrDefault(word, QueryTokenType.identifier);
                tokens.Add(new QueryToken(tokenType, word, start));
                continue;
            }

            var token = c switch
            {
                '(' => new QueryToken(QueryTokenType.left_paren, "(", pos),
                ')' => new QueryToken(QueryTokenType.right_paren, ")", pos),
                '{' => new QueryToken(QueryTokenType.left_brace, "{", pos),
                '}' => new QueryToken(QueryTokenType.right_brace, "}", pos),
                '[' => new QueryToken(QueryTokenType.left_bracket, "[", pos),
                ']' => new QueryToken(QueryTokenType.right_bracket, "]", pos),
                ',' => new QueryToken(QueryTokenType.comma, ",", pos),
                '.' => new QueryToken(QueryTokenType.dot, ".", pos),
                ';' => new QueryToken(QueryTokenType.semicolon, ";", pos),
                '=' when pos + 1 < source.Length && source[pos + 1] == '=' => new QueryToken(QueryTokenType.equals,
                    "==", pos),
                '!' when pos + 1 < source.Length && source[pos + 1] == '=' => new QueryToken(QueryTokenType.not_equals,
                    "!=", pos),
                '<' when pos + 1 < source.Length && source[pos + 1] == '=' => new QueryToken(QueryTokenType.less_equal,
                    "<=", pos),
                '>' when pos + 1 < source.Length && source[pos + 1] == '=' => new QueryToken(
                    QueryTokenType.greater_equal, ">=", pos),
                '<' => new QueryToken(QueryTokenType.less_than, "<", pos),
                '>' => new QueryToken(QueryTokenType.greater_than, ">", pos),
                '=' => new QueryToken(QueryTokenType.equals, "=", pos),
                _ => null
            };

            if (token != null)
            {
                pos += token.value.Length;
                tokens.Add(token);
            }
            else
            {
                pos++;
            }
        }

        tokens.Add(new QueryToken(QueryTokenType.eof, "", pos));
        return tokens;
    }
}