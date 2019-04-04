using Std.Data.Text.Syntax;

namespace Std.Data.Text.Sql.Syntax;

/// <summary>
///     SQL 语法高亮器
/// </summary>
public sealed class SqlSyntaxHighlighter
{
    private static readonly HashSet<string> _sql_types = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "int", "varchar", "text", "boolean", "integer", "float", "double",
        "decimal", "date", "timestamp", "blob", "bigint", "smallint", "char",
        "nvarchar", "uuid"
    };

    /// <summary>
    ///     对 SQL 源码进行语法高亮
    /// </summary>
    public IReadOnlyList<HighlightSpan> highlight(string source)
    {
        var lexer = new SqlLexer();
        var tokens = lexer.tokenize(source);
        var spans = new List<HighlightSpan>(tokens.Count);
        var offset = 0;

        foreach (var token in tokens)
        {
            if (token.type == SqlTokenType.end_of_file) break;

            // 跳过空白字符直到下一个 Token
            while (offset < source.Length && char.IsWhiteSpace(source[offset])) offset++;

            var kind = token.type switch
            {
                SqlTokenType.number => HighlightKind.number,
                SqlTokenType.@string => HighlightKind.@string,
                SqlTokenType.identifier => _sql_types.Contains(token.text)
                    ? HighlightKind.type_name
                    : HighlightKind.identifier,
                SqlTokenType.plus or SqlTokenType.minus or SqlTokenType.star or SqlTokenType.slash
                    or SqlTokenType.percent or SqlTokenType.equal or SqlTokenType.not_equal
                    or SqlTokenType.less_than or SqlTokenType.greater_than or SqlTokenType.less_equal
                    or SqlTokenType.greater_equal or SqlTokenType.ampersand or SqlTokenType.pipe
                    or SqlTokenType.concat or SqlTokenType.tilde or SqlTokenType.left_shift
                    or SqlTokenType.right_shift => HighlightKind.@operator,
                SqlTokenType.left_paren or SqlTokenType.right_paren or SqlTokenType.comma
                    or SqlTokenType.semicolon or SqlTokenType.dot => HighlightKind.delimiter,
                _ => is_keyword_type(token.type) ? HighlightKind.keyword : HighlightKind.other
            };

            spans.Add(new HighlightSpan
            {
                kind = kind,
                offset = offset,
                length = token.text.Length
            });

            offset += token.text.Length;
        }

        return spans;
    }

    /// <summary>
    ///     判断是否为关键字类型的 Token
    /// </summary>
    private static bool is_keyword_type(SqlTokenType type)
    {
        return type is SqlTokenType.select or SqlTokenType.from or SqlTokenType.where
            or SqlTokenType.insert or SqlTokenType.into or SqlTokenType.values
            or SqlTokenType.update or SqlTokenType.set or SqlTokenType.delete
            or SqlTokenType.drop or SqlTokenType.create or SqlTokenType.table
            or SqlTokenType.index or SqlTokenType.join or SqlTokenType.inner
            or SqlTokenType.left or SqlTokenType.right or SqlTokenType.outer
            or SqlTokenType.full or SqlTokenType.cross or SqlTokenType.natural
            or SqlTokenType.on or SqlTokenType.and or SqlTokenType.or
            or SqlTokenType.not or SqlTokenType.@as or SqlTokenType.order
            or SqlTokenType.by or SqlTokenType.group or SqlTokenType.having
            or SqlTokenType.limit or SqlTokenType.offset or SqlTokenType.distinct
            or SqlTokenType.all or SqlTokenType.@null or SqlTokenType.@is
            or SqlTokenType.@in or SqlTokenType.between or SqlTokenType.like
            or SqlTokenType.i_like or SqlTokenType.exists or SqlTokenType.asc
            or SqlTokenType.desc or SqlTokenType.primary or SqlTokenType.key
            or SqlTokenType.foreign or SqlTokenType.references or SqlTokenType.@default
            or SqlTokenType.constraint or SqlTokenType.unique or SqlTokenType.check
            or SqlTokenType.@if or SqlTokenType.alter or SqlTokenType.add
            or SqlTokenType.column or SqlTokenType.rename or SqlTokenType.to
            or SqlTokenType.replace or SqlTokenType.ignore or SqlTokenType.union
            or SqlTokenType.intersect or SqlTokenType.except or SqlTokenType.@case
            or SqlTokenType.when or SqlTokenType.then or SqlTokenType.@else
            or SqlTokenType.elsif or SqlTokenType.end or SqlTokenType.prepare
            or SqlTokenType.execute or SqlTokenType.deallocate or SqlTokenType.@using
            or SqlTokenType.materialized or SqlTokenType.view or SqlTokenType.refresh
            or SqlTokenType.complete or SqlTokenType.fast or SqlTokenType.cast
            or SqlTokenType.collate or SqlTokenType.autoincrement or SqlTokenType.returning
            or SqlTokenType.@true or SqlTokenType.@false or SqlTokenType.big_int
            or SqlTokenType.small_int or SqlTokenType.tiny_int or SqlTokenType.@float
            or SqlTokenType.@double or SqlTokenType.numeric or SqlTokenType.@decimal
            or SqlTokenType.@char or SqlTokenType.n_char or SqlTokenType.binary
            or SqlTokenType.glob or SqlTokenType.conflict or SqlTokenType.@do
            or SqlTokenType.nothing or SqlTokenType.upsert or SqlTokenType.begin
            or SqlTokenType.function or SqlTokenType.procedure or SqlTokenType.call
            or SqlTokenType.returns or SqlTokenType.excluded or SqlTokenType.show
            or SqlTokenType.tables or SqlTokenType.describe or SqlTokenType.columns
            or SqlTokenType.integer or SqlTokenType.real or SqlTokenType.text
            or SqlTokenType.blob or SqlTokenType.varchar or SqlTokenType.boolean
            or SqlTokenType.date or SqlTokenType.timestamp;
    }
}