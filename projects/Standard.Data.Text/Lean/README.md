# Nyar.Language.Proof — Proof 方言占位包

## 状态

占位阶段（Placeholder）。等待 Proof 方言相关实现填充。

## 对 Nyar 体系的核心价值

### 1. 形式化验证

Proof 方言将数学证明与定理证明，支持依赖类型系统。

## 验证重点：

- 定理声明与证明项如何映射到 IKun 意图
- 重写规则系统与定理证明策略
- 从证明提取代码功能
- 与 Coq/Lean 等外部求解器集成

### 2. 依赖类型

Proof 方言提供轻量级依赖类型系统，增强类型安全。

## 对标 Nyar 需求

| Proof 方言节点 | 对应核心功能   | 优先级 |
|:---------------|:---------------|:-------|
| Theorem        | 定理声明与证明 | P0     |
| Induction      | 归纳法证明     | P1     |
| Check          | 外部求解器集成 | P1     |
| Extract        | 从证明提取代码 | P2     |

## 参考资源

- [Documentation](../documentation/zh-hans/dialects/proof-dialect.md)
