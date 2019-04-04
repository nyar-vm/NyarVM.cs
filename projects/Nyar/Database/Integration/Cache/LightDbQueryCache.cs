// TODO: 待实现 - LightDbQueryCache 依赖 LightDb 的 internal API（seek、get、put、delete），
// 当前 IDatabase 公开接口为 GetAsync/PutAsync/DeleteAsync(ReadOnlyMemory<byte>)。
// 待 Sonic.Standard.Database 提供高层泛型 API 后恢复。
//
// 原始设计：基于 LightDB 的查询缓存实现，支持持久化查询结果

