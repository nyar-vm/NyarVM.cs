# Game 方言

## 概述

Game 方言面向游戏 AI、行为树、决策逻辑与游戏状态管理领域，将 NPC 行为、效用决策、规则匹配等游戏开发中常见模式提升为意图节点，使优化器能够跨游戏子系统进行全局优化。

## 节点定义

### 行为树节点

| 节点 | 描述 | 示例 |
|------|------|------|
| Sequence | 顺序执行，任一失败则整体失败 | `(Sequence [MoveTo Patrol LookAround])` |
| Selector | 选择执行，任一成功则整体成功 | `(Selector [Attack Flee Hide])` |
| Parallel | 并行执行所有子节点 | `(Parallel [Move Attack] AllSuccess)` |
| Decorator | 修饰子节点行为 | `(Decorator Invert IsVisible)` |
| Condition | 条件判断 | `(Condition (Health < 0.2))` |
| Action | 执行动作 | `(Action Attack {target: enemy})` |

### 决策逻辑节点

| 节点 | 描述 | 示例 |
|------|------|------|
| UtilityScore | 效用评估 | `(UtilityScore context scoring_fn)` |
| UtilitySelect | 选择效用最高选项 | `(UtilitySelect [flee_score attack_score])` |
| RuleMatch | 规则匹配 | `(RuleMatch low_health flee_action)` |
| RuleSet | 规则集合 | `(RuleSet [rule1 rule2] Priority)` |

### 游戏状态节点

| 节点 | 描述 | 示例 |
|------|------|------|
| StateQuery | 查询游戏状态 | `(StateQuery player health)` |
| StateUpdate | 更新游戏状态 | `(StateUpdate enemy position new_pos)` |
| EventTrigger | 触发事件 | `(EventTrigger on_damage {amount: 10})` |
| EventListen | 监听事件 | `(EventListen on_death handler)` |

### 导航与感知节点

| 节点 | 描述 | 示例 |
|------|------|------|
| PathFind | 寻路请求 | `(PathFind start goal obstacles)` |
| PerceptionQuery | 感知查询 | `(PerceptionQuery npc Visual 10.0)` |

## 等价规则

| 规则 | 模式 | 重写 | 条件 |
|------|------|------|------|
| 序列扁平化 | `Sequence(a, Sequence(b, c))` | `Sequence(a, b, c)` | |
| 选择扁平化 | `Selector(a, Selector(b, c))` | `Selector(a, b, c)` | |
| 双重否定消除 | `Invert(Invert(a))` | `a` | |
| 效用选择扁平化 | `UtilitySelect([UtilitySelect(a,b), c])` | `UtilitySelect(a, b, c)` | |
| 条件提前 | `Sequence(Condition(c), Action(a))` | `If(c, Action(a))` | 条件可静态求值 |
| 动作合并 | `Sequence(Action(a), Action(b))` | `Action(composite)` | 动作无副作用冲突 |

## 降级路径

```
Game HIR → 行为树扁平化 → 决策表生成 → 状态机转换 → Core 控制流
```

### 后端选择

| 后端 | 输出 | 适用场景 |
|------|------|----------|
| 决策表 | 查找表 | 简单规则集，追求极致性能 |
| 状态机 | 状态转移表 | 复杂行为，需要可视化调试 |
| JIT 解释器 | 字节码 | 需要热更新的动态行为 |
| Core 方言 | 通用控制流 | 与其他领域融合 |

## 与部分求值协同

若游戏状态在编译时部分已知（如关卡布局、敌人配置），PE 可将行为树特化为针对特定场景的最优决策路径。例如，若某区域无掩体，则 `Hide` 动作可被 PE 消除，直接从选择器中移除。
