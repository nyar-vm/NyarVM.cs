using Core.Data.Storage;
using Std.DataStorage;

namespace Std.Data.Storage;

/// <summary>
///     基于内存字典的关系型存储适配器，提供基于实体映射器的表操作方法。
/// </summary>
public sealed class RelationalStorageAdapter : IRelationalStorage
{
    private readonly Dictionary<string, ITableStorage> _tables = new();

    /// <summary>
    ///     根据实体映射器创建存储表。
    /// </summary>
    /// <typeparam name="T">实体类型。</typeparam>
    /// <param name="mapper">实体映射器。</param>
    public void create_table<T>(IEntityMapper<T> mapper)
    {
        var tableName = mapper.table_name;

        if (_tables.ContainsKey(tableName)) return;

        _tables[tableName] = new TableStorage<T>(mapper);
    }

    /// <summary>
    ///     插入实体到存储表。
    /// </summary>
    /// <typeparam name="T">实体类型。</typeparam>
    /// <param name="mapper">实体映射器。</param>
    /// <param name="entity">要插入的实体。</param>
    public void insert<T>(IEntityMapper<T> mapper, in T entity)
    {
        var tableName = mapper.table_name;

        if (_tables.TryGetValue(tableName, out var table)) ((TableStorage<T>)table).insert(entity);
    }

    /// <summary>
    ///     更新存储表中的实体。
    /// </summary>
    /// <typeparam name="T">实体类型。</typeparam>
    /// <param name="mapper">实体映射器。</param>
    /// <param name="entity">要更新的实体。</param>
    public void update<T>(IEntityMapper<T> mapper, in T entity)
    {
        var tableName = mapper.table_name;

        if (_tables.TryGetValue(tableName, out var table)) ((TableStorage<T>)table).update(entity);
    }

    /// <summary>
    ///     从存储表中删除实体。
    /// </summary>
    /// <typeparam name="T">实体类型。</typeparam>
    /// <param name="mapper">实体映射器。</param>
    /// <param name="entity">要删除的实体。</param>
    public void delete<T>(IEntityMapper<T> mapper, in T entity)
    {
        var tableName = mapper.table_name;

        if (_tables.TryGetValue(tableName, out var table)) ((TableStorage<T>)table).delete(entity);
    }

    /// <summary>
    ///     根据主键查找实体。
    /// </summary>
    /// <typeparam name="T">实体类型。</typeparam>
    /// <param name="mapper">实体映射器。</param>
    /// <param name="key">主键值。</param>
    /// <returns>找到的实体，未找到时为 <c>null</c>。</returns>
    public T? find_by_key<T>(IEntityMapper<T> mapper, object key)
    {
        var tableName = mapper.table_name;

        if (_tables.TryGetValue(tableName, out var table)) return ((TableStorage<T>)table).find_by_key(key);

        return default;
    }

    private interface ITableStorage
    {
    }

    private sealed class TableStorage<T> : ITableStorage
    {
        private readonly IEntityMapper<T> _mapper;
        private readonly List<T> _rows = [];

        public TableStorage(IEntityMapper<T> mapper)
        {
            _mapper = mapper;
        }

        public void insert(T entity)
        {
            _rows.Add(entity);
        }

        public void update(T entity)
        {
            var keyIndices = _mapper.primary_keys;

            for (var i = 0; i < _rows.Count; i++)
                if (matches_key(_rows[i], entity))
                {
                    _rows[i] = entity;
                    return;
                }
        }

        public void delete(T entity)
        {
            for (var i = 0; i < _rows.Count; i++)
                if (matches_key(_rows[i], entity))
                {
                    _rows.RemoveAt(i);
                    return;
                }
        }

        public T? find_by_key(object key)
        {
            var type = typeof(T);
            var keyIndices = _mapper.primary_keys;

            if (keyIndices.Count == 0) return default;

            var primaryKeyColumn = _mapper.columns[keyIndices[0]];
            var prop = type.GetProperty(primaryKeyColumn.member_name);

            if (prop is null) return default;

            foreach (var row in _rows)
            {
                var value = prop.GetValue(row);

                if (value is not null && value.Equals(key)) return row;
            }

            return default;
        }

        private bool matches_key(T existing, T candidate)
        {
            var type = typeof(T);
            var keyIndices = _mapper.primary_keys;

            foreach (var idx in keyIndices)
            {
                if (idx < 0 || idx >= _mapper.columns.Count) continue;

                var col = _mapper.columns[idx];
                var prop = type.GetProperty(col.member_name);

                if (prop is null) continue;

                var existingValue = prop.GetValue(existing);
                var candidateValue = prop.GetValue(candidate);

                if (!Equals(existingValue, candidateValue)) return false;
            }

            return true;
        }
    }
}