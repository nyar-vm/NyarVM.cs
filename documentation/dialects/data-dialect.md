# Data 方言

## 概述

Data 方言面向数据库查询与数据处理领域，将关系代数提升为可优化的意图表示。

## 节点定义

### HIR 层（关系代数）

| 节点 | 描述 | 示例 |
|------|------|------|
| Scan | 表扫描 | `(Scan "users")` |
| Filter | 条件过滤 | `(Filter pred data)` |
| Project | 列投影 | `(Project cols data)` |
| Join | 表连接 | `(Join left right condition)` |
| Agg | 聚合 | `(Agg Sum col data)` |
| Sort | 排序 | `(Sort key data)` |
| Limit | 结果限制 | `(Limit n data)` |

### MIR 层（物理算子）

| 节点 | 描述 |
|------|------|
| IndexScan | 索引扫描 |
| IndexJoin | 索引连接 |
| HashJoin | 哈希连接 |
| MergeJoin | 归并连接 |
| TableStats | 统计元数据（用于成本模型） |

## 等价规则

| 规则 | 模式 | 重写 | 说明 |
|------|------|------|------|
| Join 交换律 | `Join(A, B, cond)` | `Join(B, A, cond)` | |
| 谓词下推 | `Filter(Join(A,B,cond1), cond2)` | `Join(A, Filter(B, cond2), cond1)` | B 满足条件 |
| 投影下推 | `Project(cols, Join(A,B))` | `Join(Project(colsA, A), Project(colsB, B))` | |
| 聚合合并 | `Agg(f, Agg(g, data))` | `Agg(compose(f,g), data)` | |

## 降级路径

```
Data HIR → Data MIR（物理算子选择） → Core 方言（循环和条件） → 后端
```

### 后端选择

| 后端 | 输出 | 适用场景 |
|------|------|----------|
| SQL | `.sql` 文本 | 关系型数据库 |
| DataFrame | 内存表操作 | 本地分析 |
| Flink/Spark | 流处理作业 | 大数据处理 |
| Core 循环 | CPU 执行 | 嵌入式场景 |

## 跨领域协同

Data 方言可与 Tensor 方言融合：如果查询中包含深度学习模型调用（UDF），优化器可以尝试将模型推入 scan 算子，实现**数据库内推理**。

例如：

```
(Join (Scan "users") (Tensor.Detect model (Scan "images")) "users.id = images.user_id")
```

优化器可将 `Tensor.Detect` 下推到 scan 阶段，实现过滤下推与算子融合。
