namespace Std.Data.Text.Toml.Ast;

public enum TomlValueType
{
    @string,
    integer,
    @float,
    boolean,
    date_time,
    date,
    time,
    array,
    inline_table
}