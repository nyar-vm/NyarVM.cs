# Witness Table

## 定位：NyarVM 的动态派发机制

Witness Table 是 **NyarVM** 的对象模型与动态派发机制，属于 NyarVM 运行时的特性，而非 Nyar 框架的通用能力。其他后端使用各自平台的派发机制（JVM 用 vtable，WASM 用间接函数表，Native 用直接调用）。

## 为什么需要 Witness Table

传统 vtable 要求类的布局固定，无法在运行时添加新接口或修改方法。这对于动态加载和热更新是致命障碍。Witness Table 将方法表从对象中分离出来，每个对象只存储一个 witness 指针，而 witness 可以被替换。

此外，Witness Table 支持**接口混合**：一个类型可以实现多个接口，每个接口的方法表可以独立存储，无需像 C++ 那样通过多重继承产生菱形问题。

## 统一对象模型

NyarVM 定义一种极简的通用对象模型：

- 对象头部：`{ Witness* witness, uint32_t hash, ... }`
- Witness 结构：`{ TypeInfo* type, InterfaceTable*[] interfaces, DispatchTable methods }`

接口调用：`call_interface(obj, interface_id, method_idx)` 通过 `obj->witness->interfaces[interface_id][method_idx]` 间接调用。这种间接性允许一个对象在运行时改变其行为（替换 witness），实现热更新。

## 与优化器的关系

优化器可以利用 Witness Table 的信息进行**去虚拟化**：如果某个对象的具体类型已知（通过 profiling 或静态分析），可以将 `call_interface` 直接替换为直接函数调用，甚至内联。

## JIT 去虚拟化策略

NyarVM 的 JIT 优化管线对 Witness Table 的去虚拟化采用激进策略：

| 技术 | 实现细节 | 与 WTable 的配合 |
|:---|:---|:---|
| 类型流分析 | 通过方法内联图和类层次分析，判断调用点是否单态 | 如果检测到 90% 的调用指向同一个 Witness，JIT 生成守护代码 |
| 推测性去虚拟化 | 基于 Profiling 数据，JIT 生成只针对一个 Witness 的快速路径 | 推测失败时通过去优化回退到 WTable 路径 |
| 内联缓存 | 在调用点缓存 (receiver_witness, method_ptr) 对 | WTable 仅作为未命中时的慢路径 |

在稳定负载下，Witness Table 引入的间接开销会被 JIT 完全消除。

## 热更新机制

当模块被替换时，新的模块包含更新的类型定义和方法表。运行时可以为旧对象分配新的 witness，逐步迁移。由于 witness 只是指针赋值，无需移动对象，因此热更新成本极低。

> 注意：Witness Table 和热更新是 NyarVM 专属特性。JVM 后端使用标准 vtable，WASM 后端使用间接函数表，Native 后端使用直接调用——这些后端不支持运行时热更新。
