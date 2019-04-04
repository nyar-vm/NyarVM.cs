using System.Text;
using Std.Data.Text.Diagnostics;

namespace Std.Data.Text.Sql;

public sealed class SqlLexer
{
    private static readonly Dictionary<string, SqlTokenType> _keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SELECT"] = SqlTokenType.select,
        ["FROM"] = SqlTokenType.from,
        ["WHERE"] = SqlTokenType.where,
        ["INSERT"] = SqlTokenType.insert,
        ["INTO"] = SqlTokenType.into,
        ["VALUES"] = SqlTokenType.values,
        ["UPDATE"] = SqlTokenType.update,
        ["SET"] = SqlTokenType.set,
        ["DELETE"] = SqlTokenType.delete,
        ["DROP"] = SqlTokenType.drop,
        ["CREATE"] = SqlTokenType.create,
        ["TABLE"] = SqlTokenType.table,
        ["INDEX"] = SqlTokenType.index,
        ["JOIN"] = SqlTokenType.join,
        ["INNER"] = SqlTokenType.inner,
        ["LEFT"] = SqlTokenType.left,
        ["RIGHT"] = SqlTokenType.right,
        ["OUTER"] = SqlTokenType.outer,
        ["FULL"] = SqlTokenType.full,
        ["CROSS"] = SqlTokenType.cross,
        ["NATURAL"] = SqlTokenType.natural,
        ["ON"] = SqlTokenType.on,
        ["AND"] = SqlTokenType.and,
        ["OR"] = SqlTokenType.or,
        ["NOT"] = SqlTokenType.not,
        ["AS"] = SqlTokenType.@as,
        ["ORDER"] = SqlTokenType.order,
        ["BY"] = SqlTokenType.by,
        ["GROUP"] = SqlTokenType.group,
        ["HAVING"] = SqlTokenType.having,
        ["LIMIT"] = SqlTokenType.limit,
        ["OFFSET"] = SqlTokenType.offset,
        ["DISTINCT"] = SqlTokenType.distinct,
        ["ALL"] = SqlTokenType.all,
        ["NULL"] = SqlTokenType.@null,
        ["IS"] = SqlTokenType.@is,
        ["IN"] = SqlTokenType.@in,
        ["BETWEEN"] = SqlTokenType.between,
        ["LIKE"] = SqlTokenType.like,
        ["ILIKE"] = SqlTokenType.i_like,
        ["EXISTS"] = SqlTokenType.exists,
        ["ASC"] = SqlTokenType.asc,
        ["DESC"] = SqlTokenType.desc,
        ["PRIMARY"] = SqlTokenType.primary,
        ["KEY"] = SqlTokenType.key,
        ["FOREIGN"] = SqlTokenType.foreign,
        ["REFERENCES"] = SqlTokenType.references,
        ["DEFAULT"] = SqlTokenType.@default,
        ["CONSTRAINT"] = SqlTokenType.constraint,
        ["UNIQUE"] = SqlTokenType.unique,
        ["CHECK"] = SqlTokenType.check,
        ["IF"] = SqlTokenType.@if,
        ["INTEGER"] = SqlTokenType.integer,
        ["INT"] = SqlTokenType.integer,
        ["REAL"] = SqlTokenType.real,
        ["TEXT"] = SqlTokenType.text,
        ["BLOB"] = SqlTokenType.blob,
        ["VARCHAR"] = SqlTokenType.varchar,
        ["BOOLEAN"] = SqlTokenType.boolean,
        ["BOOL"] = SqlTokenType.boolean,
        ["DATE"] = SqlTokenType.date,
        ["TIMESTAMP"] = SqlTokenType.timestamp,
        ["ALTER"] = SqlTokenType.alter,
        ["ADD"] = SqlTokenType.add,
        ["COLUMN"] = SqlTokenType.column,
        ["RENAME"] = SqlTokenType.rename,
        ["TO"] = SqlTokenType.to,
        ["REPLACE"] = SqlTokenType.replace,
        ["IGNORE"] = SqlTokenType.ignore,
        ["UNION"] = SqlTokenType.union,
        ["INTERSECT"] = SqlTokenType.intersect,
        ["EXCEPT"] = SqlTokenType.except,
        ["CASE"] = SqlTokenType.@case,
        ["WHEN"] = SqlTokenType.when,
        ["THEN"] = SqlTokenType.then,
        ["ELSE"] = SqlTokenType.@else,
        ["IF"] = SqlTokenType.@if,
        ["ELSIF"] = SqlTokenType.elsif,
        ["END"] = SqlTokenType.end,
        ["PREPARE"] = SqlTokenType.prepare,
        ["EXECUTE"] = SqlTokenType.execute,
        ["DEALLOCATE"] = SqlTokenType.deallocate,
        ["USING"] = SqlTokenType.@using,
        ["LIMIT"] = SqlTokenType.limit,
        ["OFFSET"] = SqlTokenType.offset,
        ["MATERIALIZED"] = SqlTokenType.materialized,
        ["VIEW"] = SqlTokenType.view,
        ["REFRESH"] = SqlTokenType.refresh,
        ["COMPLETE"] = SqlTokenType.complete,
        ["FAST"] = SqlTokenType.fast,
        ["CAST"] = SqlTokenType.cast,
        ["COLLATE"] = SqlTokenType.collate,
        ["AUTOINCREMENT"] = SqlTokenType.autoincrement,
        ["RETURNING"] = SqlTokenType.returning,
        ["TRUE"] = SqlTokenType.@true,
        ["FALSE"] = SqlTokenType.@false,
        ["BIGINT"] = SqlTokenType.big_int,
        ["SMALLINT"] = SqlTokenType.small_int,
        ["TINYINT"] = SqlTokenType.tiny_int,
        ["FLOAT"] = SqlTokenType.@float,
        ["DOUBLE"] = SqlTokenType.@double,
        ["NUMERIC"] = SqlTokenType.numeric,
        ["DECIMAL"] = SqlTokenType.@decimal,
        ["CHAR"] = SqlTokenType.@char,
        ["NCHAR"] = SqlTokenType.n_char,
        ["BINARY"] = SqlTokenType.binary,
        ["GLOB"] = SqlTokenType.glob,
        ["CONFLICT"] = SqlTokenType.conflict,
        ["DO"] = SqlTokenType.@do,
        ["NOTHING"] = SqlTokenType.nothing,
        ["UPSERT"] = SqlTokenType.upsert,
        ["BEGIN"] = SqlTokenType.begin,
        ["FUNCTION"] = SqlTokenType.function,
        ["PROCEDURE"] = SqlTokenType.procedure,
        ["CALL"] = SqlTokenType.call,
        ["RETURNS"] = SqlTokenType.returns,
        ["EXCLUDED"] = SqlTokenType.excluded,
        ["SHOW"] = SqlTokenType.show,
        ["TABLES"] = SqlTokenType.tables,
        ["DESCRIBE"] = SqlTokenType.describe,
        ["COLUMNS"] = SqlTokenType.columns
    };

    private int _column;
    private DiagnosticSink? _diagnostics;
    private int _line;
    private int _position;
    private string _source = string.Empty;

    public IReadOnlyList<SqlToken> tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<SqlToken>();

        while (!is_at_end())
        {
            skip_whitespace();

            if (is_at_end()) break;

            if (peek() == '-' && peek_next() == '-')
            {
                skip_line_comment();
                continue;
            }

            if (peek() == '/' && peek_next() == '*')
            {
                skip_block_comment();
                continue;
            }

            var token = scan_token();
            if (token.type != SqlTokenType.invalid) tokens.Add(token);
        }

        tokens.Add(new SqlToken(SqlTokenType.end_of_file, string.Empty, _line, _column));
        return tokens;
    }

    private bool is_at_end()
    {
        return _position >= _source.Length;
    }

    private char peek()
    {
        return is_at_end() ? '\0' : _source[_position];
    }

    private char peek_next()
    {
        return _position + 1 >= _source.Length ? '\0' : _source[_position + 1];
    }

    private char advance()
    {
        var c = _source[_position];
        _position++;

        if (c == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        return c;
    }

    private void skip_whitespace()
    {
        while (!is_at_end() && char.IsWhiteSpace(peek())) advance();
    }

    private void skip_line_comment()
    {
        while (!is_at_end() && peek() != '\n') advance();
    }

    private void skip_block_comment()
    {
        advance();
        advance();

        var depth = 1;

        while (!is_at_end() && depth > 0)
            if (peek() == '/' && peek_next() == '*')
            {
                advance();
                advance();
                depth++;
            }
            else if (peek() == '*' && peek_next() == '/')
            {
                advance();
                advance();
                depth--;
            }
            else
            {
                advance();
            }

        if (depth > 0)
            _diagnostics?.report_error(string.Empty, default,
                3, "未闭合的块注释");
    }

    private SqlToken scan_token()
    {
        var line = _line;
        var column = _column;
        var c = peek();

        switch (c)
        {
            case '*':
                advance();
                return new SqlToken(SqlTokenType.star, "*", line, column);
            case ',':
                advance();
                return new SqlToken(SqlTokenType.comma, ",", line, column);
            case '(':
                advance();
                return new SqlToken(SqlTokenType.left_paren, "(", line, column);
            case ')':
                advance();
                return new SqlToken(SqlTokenType.right_paren, ")", line, column);
            case ';':
                advance();
                return new SqlToken(SqlTokenType.semicolon, ";", line, column);
            case '.':
                advance();
                return new SqlToken(SqlTokenType.dot, ".", line, column);
            case '=':
                advance();
                return new SqlToken(SqlTokenType.equal, "=", line, column);
            case '+':
                advance();
                return new SqlToken(SqlTokenType.plus, "+", line, column);
            case '-':
                advance();
                if (peek() == '-')
                {
                    skip_line_comment();
                    return scan_token();
                }

                return new SqlToken(SqlTokenType.minus, "-", line, column);
            case '/':
                advance();
                if (peek() == '*')
                {
                    skip_block_comment();
                    return scan_token();
                }

                return new SqlToken(SqlTokenType.slash, "/", line, column);
            case '%':
                advance();
                return new SqlToken(SqlTokenType.percent, "%", line, column);
            case '&':
                advance();
                return new SqlToken(SqlTokenType.ampersand, "&", line, column);
            case '|':
                advance();
                if (peek() == '|')
                {
                    advance();
                    return new SqlToken(SqlTokenType.concat, "||", line, column);
                }

                return new SqlToken(SqlTokenType.pipe, "|", line, column);
            case '~':
                advance();
                return new SqlToken(SqlTokenType.tilde, "~", line, column);
            case '<':
                advance();
                if (peek() == '=')
                {
                    advance();
                    return new SqlToken(SqlTokenType.less_equal, "<=", line, column);
                }

                if (peek() == '>')
                {
                    advance();
                    return new SqlToken(SqlTokenType.not_equal, "<>", line, column);
                }

                if (peek() == '<')
                {
                    advance();
                    return new SqlToken(SqlTokenType.left_shift, "<<", line, column);
                }

                return new SqlToken(SqlTokenType.less_than, "<", line, column);
            case '>':
                advance();
                if (peek() == '=')
                {
                    advance();
                    return new SqlToken(SqlTokenType.greater_equal, ">=", line, column);
                }

                if (peek() == '>')
                {
                    advance();
                    return new SqlToken(SqlTokenType.right_shift, ">>", line, column);
                }

                return new SqlToken(SqlTokenType.greater_than, ">", line, column);
            case '!':
                advance();
                if (peek() == '=')
                {
                    advance();
                    return new SqlToken(SqlTokenType.not_equal, "!=", line, column);
                }

                _diagnostics?.report_error(string.Empty, default,
                    1, "意外的字符 '!'");
                return new SqlToken(SqlTokenType.invalid, "!", line, column);
            case '\'':
                return scan_string(line, column);
            case '"':
                return scan_quoted_identifier(line, column);
            case '`':
                return scan_backtick_identifier(line, column);
            default:
            {
                if (c == '-' || char.IsDigit(c)) return scan_number(line, column);

                if (c == '_' || char.IsLetter(c)) return scan_identifier_or_keyword(line, column);

                _diagnostics?.report_error(string.Empty, default,
                    2, $"意外的字符 '{c}'");
                advance();
                return new SqlToken(SqlTokenType.invalid, c.ToString(), line, column);
            }
        }
    }

    private SqlToken scan_string(int line, int column)
    {
        advance();

        var sb = new StringBuilder();

        while (!is_at_end() && peek() != '\'')
            if (peek() == '\'' && peek_next() == '\'')
            {
                sb.Append('\'');
                advance();
                advance();
            }
            else
            {
                sb.Append(advance());
            }

        if (!is_at_end()) advance();

        return new SqlToken(SqlTokenType.@string, sb.ToString(), line, column);
    }

    private SqlToken scan_quoted_identifier(int line, int column)
    {
        advance();

        var sb = new StringBuilder();

        while (!is_at_end() && peek() != '"') sb.Append(advance());

        if (!is_at_end()) advance();

        return new SqlToken(SqlTokenType.identifier, sb.ToString(), line, column);
    }

    private SqlToken scan_backtick_identifier(int line, int column)
    {
        advance();

        var sb = new StringBuilder();

        while (!is_at_end() && peek() != '`') sb.Append(advance());

        if (!is_at_end()) advance();

        return new SqlToken(SqlTokenType.identifier, sb.ToString(), line, column);
    }

    private SqlToken scan_number(int line, int column)
    {
        var start = _position;

        if (peek() == '-') advance();

        while (!is_at_end() && char.IsDigit(peek())) advance();

        if (!is_at_end() && peek() == '.')
        {
            advance();
            while (!is_at_end() && char.IsDigit(peek())) advance();
        }

        if (!is_at_end() && (peek() == 'e' || peek() == 'E'))
        {
            advance();
            if (!is_at_end() && (peek() == '+' || peek() == '-')) advance();

            while (!is_at_end() && char.IsDigit(peek())) advance();
        }

        var text = _source[start.._position];
        return new SqlToken(SqlTokenType.number, text, line, column);
    }

    private SqlToken scan_identifier_or_keyword(int line, int column)
    {
        var sb = new StringBuilder();

        while (!is_at_end() && (char.IsLetterOrDigit(peek()) || peek() == '_')) sb.Append(advance());

        var text = sb.ToString();

        if (_keywords.TryGetValue(text, out var keywordType)) return new SqlToken(keywordType, text, line, column);

        return new SqlToken(SqlTokenType.identifier, text, line, column);
    }
}
