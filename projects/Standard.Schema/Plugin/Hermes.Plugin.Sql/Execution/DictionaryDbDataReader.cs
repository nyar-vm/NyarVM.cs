using System.Collections;
using System.Data;
using System.Data.Common;

namespace Hermes.Plugin.Sql.Execution;

/// <summary>
///     基于字典结果集的 <see cref="DbDataReader" /> 实现，用于桥接 Hermes.Database 查询结果与 ADO.NET 抽象。
/// </summary>
internal sealed class DictionaryDbDataReader : DbDataReader
{
    private readonly IReadOnlyList<string> _columns;
    private readonly IReadOnlyList<IReadOnlyDictionary<string, object?>> _rows;
    private int _currentRow = -1;

    public DictionaryDbDataReader(IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        _columns = columns;
        _rows = rows;
    }

    public override int FieldCount => _columns.Count;

    public override int Depth => 0;

    public override bool HasRows => _rows.Count > 0;

    public override bool IsClosed => false;

    public override int RecordsAffected => -1;

    public override object this[int ordinal] => GetValue(ordinal);

    public override object this[string name] => GetValue(GetOrdinal(name));

    public override bool Read()
    {
        _currentRow++;

        return _currentRow < _rows.Count;
    }

    public override string GetName(int ordinal)
    {
        return _columns[ordinal];
    }

    public override int GetOrdinal(string name)
    {
        for (var i = 0; i < _columns.Count; i++)
            if (string.Equals(_columns[i], name, StringComparison.OrdinalIgnoreCase))
                return i;

        throw new IndexOutOfRangeException($"列名 '{name}' 不存在");
    }

    public override object GetValue(int ordinal)
    {
        var row = _rows[_currentRow];
        var colName = _columns[ordinal];

        return row.TryGetValue(colName, out var value) ? value ?? DBNull.Value : DBNull.Value;
    }

    public override int GetValues(object[] values)
    {
        var count = Math.Min(values.Length, _columns.Count);
        for (var i = 0; i < count; i++) values[i] = GetValue(i);

        return count;
    }

    public override bool IsDBNull(int ordinal)
    {
        return GetValue(ordinal) is DBNull;
    }

    public override bool GetBoolean(int ordinal)
    {
        var val = GetValue(ordinal);
        if (val is bool b) return b;

        return Convert.ToBoolean(val);
    }

    public override byte GetByte(int ordinal)
    {
        return Convert.ToByte(GetValue(ordinal));
    }

    public override char GetChar(int ordinal)
    {
        var val = GetValue(ordinal);
        if (val is string s && s.Length > 0) return s[0];

        return Convert.ToChar(val);
    }

    public override DateTime GetDateTime(int ordinal)
    {
        return Convert.ToDateTime(GetValue(ordinal));
    }

    public override decimal GetDecimal(int ordinal)
    {
        return Convert.ToDecimal(GetValue(ordinal));
    }

    public override double GetDouble(int ordinal)
    {
        return Convert.ToDouble(GetValue(ordinal));
    }

    public override float GetFloat(int ordinal)
    {
        return Convert.ToSingle(GetValue(ordinal));
    }

    public override Guid GetGuid(int ordinal)
    {
        var val = GetValue(ordinal);
        if (val is Guid g) return g;

        if (val is string s) return Guid.Parse(s);

        throw new InvalidCastException($"无法将 {val.GetType()} 转换为 Guid");
    }

    public override short GetInt16(int ordinal)
    {
        return Convert.ToInt16(GetValue(ordinal));
    }

    public override int GetInt32(int ordinal)
    {
        return Convert.ToInt32(GetValue(ordinal));
    }

    public override long GetInt64(int ordinal)
    {
        return Convert.ToInt64(GetValue(ordinal));
    }

    public override string GetString(int ordinal)
    {
        return GetValue(ordinal).ToString() ?? "";
    }

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        throw new NotSupportedException();
    }

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        throw new NotSupportedException();
    }

    public override string GetDataTypeName(int ordinal)
    {
        return typeof(string).Name;
    }

    public override Type GetFieldType(int ordinal)
    {
        return typeof(object);
    }

    public override IEnumerator GetEnumerator()
    {
        return new DbEnumerator(this);
    }

    public override bool NextResult()
    {
        return false;
    }

    public override DataTable GetSchemaTable()
    {
        throw new NotSupportedException();
    }

    public override void Close()
    {
    }
}