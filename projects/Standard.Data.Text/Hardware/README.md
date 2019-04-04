# Nyar.Language.Hardware — Hardware 方言占位包

## 状态

占位阶段（Placeholder）。等待 Hardware 方言相关实现填充。

## 对 Nyar 体系的核心价值

### 1. 芯片设计与高层次综合（HLS）

Hardware 方言提供完整的芯片设计与高层次综合（HLS）支持，包括硬件模块、时序/组合逻辑、微架构意图（流水线、循环展开、脉动阵列、）。

## 验证重点：

- Verilog/FIRRTL 后端代码生成
- 资源共享优化
- 寄存器重定时优化
- 流水线插入优化
- 与部分求值引擎协同（特化固定参数生成纯组合逻辑

## 对标 Nyar 需求

| Hardware 方言节点 | 对应核心功能   | 优先级 |
|:------------------|:---------------|:-------|
| Module            | 硬件模块定义   | P0     |
| Pipeline          | 流水线结构意图 | P0     |
| Unroll            | 循环展开       | P1     |
| SystolicArray     | 脉动阵列       | P2     |

## 参考资源

- [Documentation](../documentation/zh-hans/dialects/hardware-dialect.md)
