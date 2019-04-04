# Schedule 方言

## 概述

Schedule 方言面向任务调度、资源分配与约束优化领域，覆盖从 Kubernetes 容器编排到生产线调度的各类调度问题。它将调度意图从具体算法中解耦，使优化器能够根据问题规模和约束特征自动选择最优调度策略。

## 节点定义

### 任务定义节点

| 节点 | 描述 | 示例 |
|------|------|------|
| Task | 任务定义 | `(Task "job1" duration [cpu:2] 5)` |
| TaskSet | 任务集合 | `(TaskSet [task1 task2 task3])` |
| ResourceRequirement | 资源需求 | `(ResourceRequirement "GPU" 1 Exact)` |

### 调度约束节点

| 节点 | 描述 | 示例 |
|------|------|------|
| Precedence | 前置依赖 | `(Precedence taskA taskB)` |
| TimeWindow | 时间窗口 | `(TimeWindow task1 0 100)` |
| MutualExclusion | 互斥约束 | `(MutualExclusion taskA taskB)` |
| CoLocation | 共置约束 | `(CoLocation taskA taskB)` |
| SoftConstraint | 软约束 | `(SoftConstraint affinity 0.5)` |

### 资源节点

| 节点 | 描述 | 示例 |
|------|------|------|
| ResourcePool | 资源池 | `(ResourcePool "node1" "CPU" 16 [])` |
| ResourceAssignment | 资源分配 | `(ResourceAssignment task1 "node1" 2)` |

### 调度目标节点

| 节点 | 描述 | 示例 |
|------|------|------|
| MinimizeMakespan | 最小化完成时间 | `(MinimizeMakespan schedule)` |
| MinimizeTotalDelay | 最小化总延迟 | `(MinimizeTotalDelay schedule)` |
| MinimizeResourceCost | 最小化资源成本 | `(MinimizeResourceCost schedule costs)` |
| BalanceLoad | 负载均衡 | `(BalanceLoad schedule)` |
| MultiObjective | 多目标优化 | `(MultiObjective [makespan cost] [0.6 0.4])` |

### 调度策略节点

| 节点 | 描述 | 示例 |
|------|------|------|
| ScheduleStrategy | 调度策略 | `(ScheduleStrategy CriticalPath problem)` |
| ScheduleResult | 调度结果 | `(ScheduleResult assignments)` |

## 等价规则

| 规则 | 模式 | 重写 | 条件 |
|------|------|------|------|
| 任务集扁平化 | `TaskSet([TaskSet(a,b), c])` | `TaskSet(a, b, c)` | |
| 前置传递性 | `A -> B 且 B -> C` | `A -> C` | 传递闭包 |
| 互斥对称性 | `MutualExclusion(A, B)` | `MutualExclusion(B, A)` | |
| 多目标合并 | `MultiObjective([MultiObjective(a,b)], w)` | 权重重新分配 | |
| 软约束硬化 | `SoftConstraint(hard, inf)` | 转为硬约束 | 权重趋于无穷 |

## 降级路径

```
Schedule HIR → 约束图构建 → 算法选择 → 求解器调用 → Core 控制流
```

### 后端选择

| 后端 | 输出 | 适用场景 |
|------|------|----------|
| ILP 求解器 | 整数规划模型 | 精确解，小规模问题 |
| 启发式 | 贪心/列表调度 | 大规模问题，实时性要求 |
| 元启发式 | GA/SA 参数 | 复杂约束，NP-hard 问题 |
| CP-SAT | 约束规划 | 组合约束丰富的问题 |
| Kubernetes API | 部署配置 | 云原生容器编排 |

## 与部分求值协同

若任务执行时间在编译时已知（如固定时长的批处理作业），PE 可将调度问题特化为静态分配表，消除运行时调度开销。若资源池规模固定，PE 可预计算资源分配矩阵，生成无分支的分配代码。
