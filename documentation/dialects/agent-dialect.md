# Agent 方言

## 概述

Agent 方言面向多智能体规划、协调与分布式决策领域，支持从单智能体规划到大规模多智能体系统的完整工作流。它将 BDI（信念-愿望-意图）模型、HTN（分层任务网络）规划和智能体通信原语提升为意图节点。

## 节点定义

### 智能体定义节点

| 节点 | 描述 | 示例 |
|------|------|------|
| Agent | 智能体定义 | `(Agent "agent1" [move grab] beliefs goals)` |
| AgentSet | 智能体集合 | `(AgentSet [agent1 agent2] Cooperative)` |
| BeliefState | 信念状态 | `(BeliefState {location: room1})` |
| Goal | 目标定义 | `(Goal target_state Critical false)` |

### 规划节点

| 节点 | 描述 | 示例 |
|------|------|------|
| ActionDef | 动作定义 | `(ActionDef "grab" [reachable] [holding] cost)` |
| Plan | 计划 | `(Plan [move grab] [move<grab])` |
| PlanRequest | 规划请求 | `(PlanRequest init goals actions)` |
| HtnTask | HTN 任务 | `(HtnTask "navigate" [method1 method2])` |
| HtnMethod | HTN 方法 | `(HtnMethod "walk" [clear] [step step])` |

### 协调与通信节点

| 节点 | 描述 | 示例 |
|------|------|------|
| SendMessage | 消息发送 | `(SendMessage a1 a2 "proposal" content)` |
| ReceiveMessage | 消息接收 | `(ReceiveMessage a2 "proposal" handler)` |
| Broadcast | 广播 | `(Broadcast a1 "alert" danger_level)` |
| Commitment | 承诺 | `(Commitment a1 obligation deadline)` |
| Negotiation | 协商 | `(Negotiation [a1 a2] resource ContractNet)` |

### 感知与推理节点

| 节点 | 描述 | 示例 |
|------|------|------|
| Observation | 观察 | `(Observation a1 sensor 0.95)` |
| BeliefUpdate | 信念更新 | `(BeliefUpdate beliefs obs bayesian)` |
| IntentionFormation | 意图形成 | `(IntentionFormation goals beliefs plans)` |

## 等价规则

| 规则 | 模式 | 重写 | 条件 |
|------|------|------|------|
| 智能体集扁平化 | `AgentSet([AgentSet(a,b), c])` | `AgentSet(a, b, c)` | |
| 计划扁平化 | `Plan([Plan(a,b), c])` | `Plan(a, b, c)` | |
| HTN 分解 | `HtnTask` | `Plan(subtasks)` | 方法前置条件满足 |
| 信念合并 | `BeliefUpdate(BeliefState, obs)` | 更新后信念状态 | |
| 通信消除 | `SendMessage(a, b, m); ReceiveMessage(b, m)` | 直接传递 | 同一进程内 |

## 降级路径

```
Agent HIR → 规划求解 → 通信协议生成 → 状态机转换 → Core 控制流
```

### 后端选择

| 后端 | 输出 | 适用场景 |
|------|------|----------|
| PDDL 求解器 | 经典规划问题 | 确定性环境，完全可观察 |
| HTN 规划器 | 层次化计划 | 结构化任务，领域知识丰富 |
| BDI 运行时 | 信念-意图循环 | 动态环境，反应式行为 |
| 消息中间件 | 通信协议 | 分布式多智能体系统 |
| Core 方言 | 通用控制流 | 与其他领域融合 |

## 与部分求值协同

若环境模型在编译时部分已知（如地图布局、动作效果确定），PE 可将规划问题特化为预计算的策略表，消除运行时规划开销。若智能体间通信拓扑固定，PE 可内联消息路由，生成直接的函数调用链。
