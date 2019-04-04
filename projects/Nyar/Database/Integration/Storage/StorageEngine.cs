namespace Nyar.Database.Integration.Storage;

// TODO: 待实现 - StorageEngine 依赖 LightDb 的 internal API（seek、put、delete、check_point）
// 以及 ICursor.Current 返回的是 (ReadOnlyMemory<byte>, ReadOnlyMemory<byte>) 元组而非强类型 API。
// IStorageEngine 接口定义已迁移至 Nyar.Database.Storage.IStorageEngine。
// 待 Sonic.Standard.Database 提供高层泛型 API 后恢复完整实现。
//
// 原始设计：基于 LightDB 的存储引擎实现，支持事务批处理与懒加载冷启动优化