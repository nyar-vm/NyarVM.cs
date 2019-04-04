namespace Nyar.Database.Integration.Query;

// TODO: 待实现 - PersistentQueryCache 依赖 LightDb 的 internal API（get、put、delete、seek），
// 当前 IDatabase 公开接口为 GetAsync/PutAsync/DeleteAsync(ReadOnlyMemory<byte>)，
// 缺乏泛型序列化辅助层。待 Sonic.Standard.Database 提供高层泛型 API 后恢复完整实现。
//
// 原始设计：基于 LightDB 的持久化查询缓存，在内存缓存的基础上同步写入 LightDB，
// 支持跨进程重启的缓存持久化和冷启动加速