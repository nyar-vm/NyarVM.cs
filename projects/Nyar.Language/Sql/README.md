# Nyar.Language.Sql — Data/SQL 方言占位包

## 状态

占位阶段（Placeholder）。等待 Data/SQL 方言相关实现填充。

## 对 Nyar 体系的核心价值

### 1. 数据库查询与数据处理

Data/SQL 方言提供完整的数据库查询优化，包括扫描、过滤、投影、连接、聚合、排序、限制。

## 验证重点：

- SQL 查询 AST 如何映射到 Data 方言的 IKun 意图
- 连接交换律与谓词下推优化
- 表统计信息驱动的查询优化
- 与现有数据库引擎集成

## 对标 Nyar 需求

| Data 方言节点 | 对应核心功能 | 优先级 |
|:--------------|:-------------|:-------|
| Scan          | 表扫描       | P0     |
| Filter        | 条件过滤     | P0     |
| Join          | 表连接       | P1     |
| Aggregate     | 聚合函数     | P1     |

## 参考资源

- [Documentation](../documentation/zh-hans/dialects/data-dialect.md)
