# Nyar.Zig — Zig 语言前端占位包

## 状态

占位阶段（Placeholder）。等待 Zig 编译器相关实现填充。

## 对 Nyar 体系的核心价值

### 1. `comptime` → 部分求值引擎验证

Zig 的 `comptime` 是工业级"部分求值"的成功案例。Nyar 的 PE 引擎需要与 Zig 的编译期计算对比验证特化策略的正确性与性能。

**测试重点：**

- Zig `comptime` 的求值边界（哪些表达式可在编译期求值）与 Nyar PE 的"静态已知"定义是否一致？
- `comptime` 的泛型特化（如 `ArrayList(T)`）与 Nyar 的"自动发现特化机会"的对比
- 编译期反射（`@Type`、`@field`）与 Nyar `type_of` / `sizeof` 内省原语的映射

### 2. 显式内存管理 → 无 GC 后端

Zig 没有 GC，但通过分配器接口（`std.mem.Allocator`）提供安全抽象。Nyar 的 GC 是"精确非移动"，Zig 可为 Nyar 提供无 GC 后端的参考实现。

**测试重点：**

- Zig 分配器接口与 Nyar Core `Alloc/Free` 的语义对接
- `defer` / `errdefer` 与 Nyar 效应处理器（特别是异常效应 `Throw`）的资源清理语义
- 栈分配与 Arena 分配在 Nyar 成本模型中的表示

### 3. C 互操作增强

Nyar.C 示例已存在，但 Zig 是更好的 C 替代前端（原生支持 C ABI，无需 FFI 胶水）。Zig 可以编译 C 代码，也可以被 C 代码调用。

**测试重点：**

- Zig 的 C 导入（`@cImport`）与 Nyar.C 的 `Oak.C` 解析器协同
- Zig 的 `export` / `extern` 与 Nyar 模块导出段的交互
- 用 Zig 替代 C 作为 Nyar 的"系统编程层"前端

## 对标 Nyar 需求

| Zig 特性     | Nyar 对应缺口            | 优先级 |
|:-------------|:-------------------------|:-------|
| `comptime`   | 部分求值引擎验证         | P0     |
| 显式内存管理 | 无 GC 后端参考           | P0     |
| C 互操作     | Nyar.C 增强替代          | P1     |
| 错误联合类型 | `Throw` 效应语义映射     | P1     |
| 编译期反射   | 内省原语扩展             | P2     |
| 交叉编译     | AOT 工作流目标三元组扩展 | P2     |

## 参考资源

- [Zig Documentation](https://ziglang.org/documentation/master/)
- [Zig comptime](https://ziglang.org/documentation/master/#comptime)
- [Zig Allocator Interface](https://ziglang.org/documentation/master/#Choosing-an-Allocator)
