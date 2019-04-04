// TODO: 待实现 - IKunTypeMapper 使用的 DatabaseValue.FromString / ToObject 方法不存在，
// 实际 API 为 DatabaseValue.from_object / to_object（snake_case 命名）。
// Oa 为判别联合类型，无法通过简单 JsonSerializer.Deserialize 反序列化。
// 待 Oa 序列化基础设施完善后恢复。
//
// 原始设计：Oa IR 节点与 DatabaseValue 之间的类型映射

