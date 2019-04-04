# Nyar.Erlang — Erlang/Elixir 语言前端占位包

## 状态

占位阶段（Placeholder）。等待 Erlang/Elixir 编译器相关实现填充。

## 对 Nyar 体系的核心价值

### 1. Actor 模型 → 代数效应扩展

Erlang 的 Actor 模型（进程隔离、消息传递、监督树）与 Nyar VM 的 `Fork` 效应和轻量级线程有可比性。Nyar 当前效应系统缺少分布式和容错语义。

**测试重点：**

- Erlang 进程隔离模型能否用 Nyar 的效应处理器实现（每个 Actor 作为一个独立的效应处理上下文）？
- `let it crash` 哲学与 Nyar `Throw` 效应处理的监督策略（supervision strategy）映射
- 消息邮箱（mailbox）与 Nyar 代数效应的异步 `Perform` 队列的语义差异

### 2. 热更新 → Witness Table 动态替换

Nyar VM 明确将"模块热加载"作为设计目标，而 Erlang/OTP 是工业界热更新的标杆。Witness Table 的动态替换机制需要与 Erlang
的代码替换语义对比验证。

**测试重点：**

- Erlang 的 `code:load_binary/3` 与 Nyar 模块热加载的 `Witness` 指针替换机制对比
- 热更新时的状态迁移（Erlang `code_change` 回调）与 Nyar 对象模型的一致性维护
- 旧版本代码的逐步淘汰（Erlang `purge`）与 Nyar 模块生命周期的协同

### 3. 分布式计算 → Network 效应扩展

Standard 方言有 Network 效应（TcpConnect / HttpRequest），但没有分布式计算方言。Erlang 的分布式原语可为 Nyar 提供分布式效应的参考设计。

**测试重点：**

- Erlang 的 `rpc:call` / `spawn_link(Node, ...)` 与 Nyar `Perform` 节点的跨节点语义扩展
- 分布式一致性（Erlang `global` 模块）与 Nyar 效应处理器的全局状态管理
- 集群拓扑变化（节点上下线）与 Nyar 模块热加载在分布式环境下的协同

## 对标 Nyar 需求

| Erlang 特性      | Nyar 对应缺口              | 优先级 |
|:-----------------|:---------------------------|:-------|
| Actor 模型       | 并发效应处理器扩展         | P1     |
| 热更新           | Witness Table 动态替换验证 | P1     |
| 监督树           | 效应处理容错策略           | P2     |
| 分布式原语       | Network 效应扩展           | P2     |
| Pattern Matching | Core 方言模式匹配扩展      | P2     |

## 参考资源

- [Erlang Documentation](https://www.erlang.org/doc/)
- [OTP Design Principles](https://www.erlang.org/doc/design_principles/des_princ.html)
- [Elixir Documentation](https://hexdocs.pm/elixir/introduction.html)
