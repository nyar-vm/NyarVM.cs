using Core.Data.Storage;

namespace Std.DataStorage;

/// <summary>
///     关系型存储后端接口，提供基于实体映射器的表操作方法�?///
/// </summary>
public interface IRelationalStorage
{
    /// <summary>
    ///     根据实体映射器创建存储表�?    ///
    /// </summary>
    /// <typeparam name="T">
    ///     实体类型�?/typeparam>
    ///     <param name="mapper">实体映射器�?/param>
    void create_table<T>(IEntityMapper<T> mapper);

    /// <summary>
    ///     插入实体到存储表�?    ///
    /// </summary>
    /// <typeparam name="T">
    ///     实体类型�?/typeparam>
    ///     <param name="mapper">
    ///         实体映射器�?/param>
    ///         <param name="entity">要插入的实体�?/param>
    void insert<T>(IEntityMapper<T> mapper, in T entity);

    /// <summary>
    ///     更新存储表中的实体�?    ///
    /// </summary>
    /// <typeparam name="T">
    ///     实体类型�?/typeparam>
    ///     <param name="mapper">
    ///         实体映射器�?/param>
    ///         <param name="entity">要更新的实体�?/param>
    void update<T>(IEntityMapper<T> mapper, in T entity);

    /// <summary>
    ///     从存储表中删除实体�?    ///
    /// </summary>
    /// <typeparam name="T">
    ///     实体类型�?/typeparam>
    ///     <param name="mapper">
    ///         实体映射器�?/param>
    ///         <param name="entity">要删除的实体�?/param>
    void delete<T>(IEntityMapper<T> mapper, in T entity);

    /// <summary>
    ///     根据主键查找实体�?    ///
    /// </summary>
    /// <typeparam name="T">
    ///     实体类型�?/typeparam>
    ///     <param name="mapper">
    ///         实体映射器�?/param>
    ///         <param name="key">
    ///             主键值�?/param>
    ///             <returns>找到的实体，未找到时�?<c>null</c>�?/returns>
    T? find_by_key<T>(IEntityMapper<T> mapper, object key);
}