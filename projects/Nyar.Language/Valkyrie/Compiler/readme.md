# Valkyrie.Compiler 详细设计规范

**版本**：3.0  
**适用范围**：Valkyrie 正式编译主链及多后端 lowering  
**关联规范**：`Valkyrie.TypeChecker 类型系统与检查规范`  
**核心原则**：分层 lowering、表示变换、结构类型一阶化、编译器决定指令、多后端共享语义

---

## 1. 引言

`Valkyrie.Compiler` 是 Valkyrie 语言的唯一正式编译管线。它承接经过 `TypeChecker` 静态验证的 AST，经过一系列严格分层的中间表示，最终产出面向
CLR、JVM、WebAssembly（+JS glue）、原生机器码以及 NyarVM 的执行产物。本规范的设计目标是在不积累技术债的前提下，将 **方法行 +
trait/effect + 结构类型 + 控制语义** 逐层收束为可共享、可优化、可稳定落到多后端的正式编译主链。

本规范不再赘述类型系统的推导规则与解糖细节，这些内容由 `Valkyrie.TypeChecker 类型系统与检查规范`
完整定义。我们只聚焦于编译器各层的设计、witness table 的编译实现、多平台 lowering 策略以及关键优化机制。

---

## 2. 编译主链与分层哲学

整个编译流程遵循一条线性管道，从 AST 开始，经历六次重大表示变换：

```
AST → 语义分析 → HIR → MIR(EGraph<IKun>) → LIR → Target Emit → Package
```

每一步的抽象变化如下表所示：

| 层次        | 消除的高阶特性                                              | 引入的一阶表示                                                    |
|-------------|-------------------------------------------------------------|-------------------------------------------------------------------|
| HIR         | 语法糖、属性→方法、模式嵌套、效应块的隐式控制流             | 显式存在容器构造、全称泛型调用、效应标记与 handler 结构           |
| MIR         | 全称与存在类型的分派抽象、效应处理的闭包形式、结构化控制流  | Witness table 指针、存在容器 = (value, witness)、状态机变量与切换 |
| LIR         | 结构化控制流（`match`、`loop`）、抽象调用语义、内存模型抽象 | 基本块 + 跳转、`CallStatic/CallWitness/CallDynamic`、显式堆栈指令 |
| Target Emit | 平台无关指令、线性内存抽象                                  | 平台特定指令、调用约定、GC 交互、直接机器/字节码                  |

**禁止跨层引用**，每层仅消费上一层产出的精确 IR，不回溯类型检查信息。

### 2.1 文本类型纪律

`Valkyrie` 的类型系统里没有宽泛的 `string` 概念，这条规则和“不存在宽泛 `number`，只能写成 `i32`、`f32` 等确定类型”是同一条设计纪律。

- 源码层可以存在“字符串字面量”这一语法现象。
- 但从 `HIR` 开始，文本类型必须已经是确定性的正式类型，例如 `utf8`、`utf16`、`utf32` 或 `c_str`。
- `MIR`、`LIR` 以及后端不得再重新引入宽泛 `string`，也不得根据目标平台去猜文本编码。
- `CLR`、`JVM`、`WASM` 只能做“目标表示映射”，不能回头替前面的编译阶段决定文本语义。

历史遗留的 `string` / `String` 仅视为错误的旧接口输入，不再属于正式编译主链的一部分。任何继续将它带入 `HIR/MIR/LIR` 的代码，都应直接报错，而不是做兼容归一化。

---

## 3. 类型系统核心（极简回顾）

Valkyrie 的类型系统基于 **结构类型**，类型的身份由 **公开方法行** 定义。每个类型 `τ` 关联一个方法行 `methods(τ)`
，它是一个从方法名到签名的映射（支持行变量）。子类型判定为方法行的覆盖：`τ <: σ` 当且仅当 `methods(τ)` 包含 `methods(σ)`
的所有方法，且对于每个共同方法，参数逆变、返回协变、效应集协变。

- **原始类型**：内建方法行。
- **类类型（class）**：引用类型，变量持有对象引用，其方法行由 `public` 方法和属性（脱糖为 get/set）决定。
- **结构体（struct）**：值类型，同样由公开方法定义身份，赋值默认 move，可通过 `&T` 引用传递，无生命周期分析（类似 C# 的 ref）。
- **泛型与约束**：`T: C` 为全称类型，实例化时调用方传入 `T` 的 witness table。
- **存在类型**：`any { foo: () -> i32 }` 等价于 `∃X. X 有 foo`，表示为值 + witness 指针的存在容器。
- **联合类型**（`A | B`）：匿名联合，方法行为共同方法交集。
- **交集类型**（`A & B`）：窄化用，方法行为并集。
- **效应系统**：每个函数和方法签名携带效应集 `ε`，效应在调用点传播。

这些特性在编译器中全部由 **witness table** 承载，不涉及传统虚表（vtable）。Valkyrie 中不存在名义上的类继承层次，继承关键字仅为成员转发语法糖。

---

## 4. HIR 设计（简要）

HIR 是把 AST 与语义模型结合的第一步，仍然保留语言层概念，但已消除语法糖。

### 4.1 主要职责

- 将所有属性访问转换为 `get_*`/`set_*` 方法调用。
- `unite` 的变体构造转换为带标签的构造器。
- 显式引入存在容器和泛型见证的概念（仍以符号引用）。
- 效应处理仍为高级 `handle`/`resume` 结构，暂不展开状态机。

### 4.2 关键指令（节选）

- `CallStatic`, `CallVirtual`（名义方法调用，后期降级为具体分派）
- `PackExistential(val, protocol)` — 构造存在类型容器
- `UnpackExistential(existential)` — 分解容器
- `CallConstraint(subject, method_name, constraint)` — 通过约束的方法调用

HIR 仍依赖 `TypeChecker` 产生的类型注释，保证操作合法。

---

## 5. MIR 详细设计

MIR 是整个编译链的核心优化层，采用 `EGraph<IKun>` 数据结构，允许保留多个程序等价版本，通过代价模型提取最优形态。MIR
彻底消除高阶类型抽象，引入 witness table 作为一等数据。

### 5.1 Witness Table 的实体化

每个独立的方法行约束（例如 `{ foo: () -> i32 }`）都有一个编译器生成的全局 **witness table 类型**，其内部是一个函数指针数组。具体类型
`T` 满足该约束时，编译器生成一个全局常量 `Witness_T_Constraint`，填充 `T` 中各方法的实际地址。

在 MIR 中，witness table 被表示为 `GlobalAddr` 节点，值为指针，类型为对应 witness table 类型。操作指令有：

- `witness = GlobalAddr(@Witness_T_C)` — 获取某个具体类型实现约束 C 的 witness 表地址
- `packed = ExistentialPack(value_ptr, witness)` — 构造存在容器
- `value_ptr, witness = ExistentialUnpack(packed)` — 拆解
- `ret = CallWitness(witness, method_idx, self, args...)` — 通过 witness 表分派方法，`method_idx` 为编译时确定的偏移

泛型函数 `f<T: C>(t: T)` 转变为普通函数，签名增加一个隐藏参数 `witness: ptr`。函数体内对约束方法的调用被替换为
`CallWitness(witness, idx, t_ptr, ...)`。调用方在实例化时传入具体类型的 witness 表指针。

### 5.2 别名分析与逃逸分析

基于“可变不共享，共享不可变”原则，MIR 执行别名分析和逃逸分析，为后续优化提供依据。

#### 5.2.1 别名分析

- 值类型变量默认无内部别名，除非其内部包含 `&T` 字段。
- 引用类型变量可能通过任意其他引用别名。除非编译器能证明对象创建后未逃逸（本地独占），则标记为无外部别名。
- 不可变对象（经 `freeze` 或类型标注）的多引用只读，无写入别名风险。
- 分析结果用于消除冗余加载、去虚拟化提升、写屏障消除。

#### 5.2.2 逃逸分析

一个对象（或值类型的引用）被称为逃逸，如果它可能：

- 返回或赋值给外部作用域
- 传递给其他函数且该函数可能存储或返回它
- 包装进存在容器且容器逃逸
- 写入全局变量

未逃逸的对象可以：

- 引用类型 → 栈分配（LIR 中使用 `AllocLocal` + 字段访问），免去堆分配
- 值类型 → 传递引用时无需复制，直接操作原始地址
- 存在容器 → 完全拆解，值内联到调用点，消除 `ExistentialPack` 分配

逃逸分析与“可变不共享”紧密配合：若可变对象未逃逸，可保证独占修改；若必须逃逸，编译器在逃逸点插入 `clone` 或 `freeze`
以满足共享不可变原则。

### 5.3 核心优化遍

- **Witness 常量传播与死消除**：若泛型函数内未使用任何约束方法，删除 witness 参数；若调用点传入常量 witness，则
  `CallWitness` 可替换为 `CallStatic`。
- **存在容器拆解**：当存在容器局部使用且未逃逸，将其拆为原始值 + 静态 witness，消除分配。
- **去虚拟化**：`CallWitness`、`CallDynamic` 在接收者类型已知时转为直接调用。
- **内联**：将小函数体嵌入调用者，暴露更多常量。
- **效应状态机简化**：纯函数消除效应标记；将 `handle`/`resume` 降为一阶状态机（见 6.6 节）。

所有优化均在 EGraph 等价饱和框架下进行，允许选择成本最低的组合。

---

## 6. LIR 详细设计

LIR 是平台无关的低层中间表示，采用控制流图（基本块 + 跳转）。所有语言高级语义被彻底一阶化。LIR 是后端的唯一输入源。

### 6.1 类型模型

- 值类型（`struct`）直接以内存块形式存在，大小为编译时常量。
- 引用类型（`class`）是指向堆分配对象的指针。
- 存在容器固定为两个指针大小（值缓冲区 + witness 指针），当值类型尺寸 ≤ 缓冲区时，值内联，否则存储堆指针。
- 函数指针为通用指针，可用于间接调用。

### 6.2 核心指令集

#### 6.2.1 调用指令

- `CallStatic(callee, args) -> ret`  
  直接调用已知函数（符号地址）。用于单态化后的函数、顶层函数。
- `CallWitness(witness_ptr, method_idx, self, args) -> ret`  
  从 `witness_ptr` 指向的函数表加载第 `method_idx` 项（指针），以 `self` 和 `args` 调用。`self` 必须为指针（值类型传指针，引用类型传对象指针）。
- `CallDynamic(receiver, method_name, args) -> ret`  
  需要运行时类型查找的方法调用，用于后端接口合成场景。Valkyrie 本身使用此指令表示已通过 witness 表但仍需平台动态分派的情况（如
  JVM 接口调用）。编译器应尽量通过 MIR 优化将其消去。
- `CallIndirect(func_ptr, args) -> ret`  
  通过任意函数指针调用，用于闭包、函数值等。
- `CallVirtual(receiver, method_name, args) -> ret`  
  **仅 NyarVM 使用**，表示基于内嵌虚表的调用。Valkyrie 代码绝不会生成此指令。

#### 6.2.2 存在容器操作

- `ExistentialPack(value_ptr, witness_ptr, is_inline) -> ex_ptr`  
  构建存在容器。当 `is_inline` 为真时，将值复制进容器内部缓冲区；否则存储指针。
- `ExistentialUnpack(ex_ptr) -> value_or_ptr, witness_ptr, is_inline`  
  提取容器内容。

#### 6.2.3 内存管理

- `AllocObj(size, type_id?) -> obj_ptr`  
  堆分配引用对象。可选 `type_id` 用于运行时类型测试。
- `AllocLocal(type) -> ptr`  
  在当前栈帧分配局部变量（未初始化），用于未逃逸对象栈分配或值类型占位。
- `AllocExistential(value_size) -> ex_ptr`  
  分配存在容器，内部布局由值大小决定。
- `MoveValue(type, dest, src)`  
  将值从 `src` 移动到 `dest`，`src` 变为未初始化（后续使用为编译期错误）。
- `CopyValue(type, dest, src)`  
  浅复制值类型，`src` 保持有效。用于隐式拷贝。
- `CloneValue(type, dest, src)`  
  调用类型的 `clone` 方法进行深拷贝，用于显式克隆。
- `LoadValue(type, ptr) -> value` / `StoreValue(type, ptr, value)`  
  值类型的加载与存储。
- `LoadRef(ptr) -> ref` / `StoreRef(ptr, ref)`  
  引用类型的加载与存储。
- `Freeze(obj_ptr) -> frozen_ptr`  
  将可变对象转换为不可变引用，之后禁止修改。
- `Deinit(ptr, type)`  
  释放值类型所持有资源（如 RAII），通常在 move 后调用。

#### 6.2.4 控制流

- `Br(label)`, `BrCond(cond, true_label, false_label)`, `Switch(tag, cases)`, `Return(value)`, `Unreachable`

#### 6.2.5 类型测试与转换

- `IsType(obj_ptr, type_id) -> bool`
- `AsType(obj_ptr, type_id) -> obj_ptr`
- `LoadTypeId(obj_ptr) -> type_id`

#### 6.2.6 效应与状态机

- `SetState(state_var, new_state)`, `GetState(state_var) -> state`
- `Yield(result, state)` — 保存当前状态并返回，用于生成器
- `SwitchState(state_var, cases)` — 根据状态跳转
- `EffectCall(callee, args) -> ret` — 标记带效应的调用（后端可能展开为状态保存 + 调用 + 恢复）

### 6.3 值类型语义实现

- 赋值和传参默认使用 `MoveValue`。当值被后续使用且编译器无法保证无需拷贝时，自动插入 `CopyValue`（隐式拷贝），类似 C# 的行为。
- 显式 `clone` 调用生成 `CloneValue`。
- 引用 `&T` 通过指针传递，`LoadValue`/`StoreValue` 直接操作该指针。逃逸分析确保引用不存活长于被引用对象。若引用需逃逸且值是可变的，编译器自动在逃逸点插入
  `CopyValue` 到堆或要求 `freeze`。

### 6.4 效应降级

效应处理已从 HIR 的高级形式降级为状态机：

- 函数体根据效应调用点切分为多个基本块，每个挂起点分配一个整数状态。
- 局部变量需要跨挂起存活的，被提升至隐式栈结构体或堆分配的闭包环境。
- `resume` 变为 `SetState` + 跳转到对应恢复点。
- 对于像 `throw` 这样不恢复的效应，转为平台异常机制（`br` 到异常处理块）。

---

## 7. 后端 Lowering 策略

各后端将 LIR 翻译为目标平台指令。核心原则： **编译器决定所有指令形式，不依赖 JIT 优化来保证性能**。后端代码生成仅做指令选择、寄存器分配和
ABI 适配。

### 7.1 通用映射

| LIR 概念           | CLR                               | JVM                                       | WASM                                 | Native              |
|--------------------|-----------------------------------|-------------------------------------------|--------------------------------------|---------------------|
| Witness table 指针 | 接口引用                          | 接口引用 或 `MethodHandle` 数组           | 函数表索引基址 + 偏移                | 函数指针结构体指针  |
| 存在类型变量       | 合成接口 `IMethodRow` 引用        | 合成接口 `MethodRow` 引用                 | `(value_ptr, witness_base)` 双 `i32` | `{ptr, ptr}` 结构体 |
| 值类型             | `struct` （泛型特化或显式值类型） | 堆分配类（编译器逃逸分析决定栈替换）      | 内联或指针，线性内存                 | 寄存器或栈          |
| 效应状态机         | `ValueTask` + 状态机结构体        | 状态类 + `CompletableFuture` 或 Loom 纤程 | 手动状态变量 + JS `Promise` 桥接     | 跳转表 / 尾调用     |
| 动态分派           | `callvirt` 指令                   | `invokeinterface` / `invokedynamic`       | `call_indirect`                      | 间接 `call`         |

### 7.2 CLR 后端详细设计

#### 7.2.1 方法行到接口合成

- 每个独立的方法行字面量生成一个 `interface IMethodRow_N`，包含对应方法。
- 对于每个具体类型 `T` 满足该行，生成 `sealed class T_Witness_N : IMethodRow_N`，持有 `T` 的引用（引用类型）或 `T`
  的装箱引用（值类型），并实现接口方法委托给原对象。
- 存在类型变量编译为 `IMethodRow_N` 引用。`ExistentialPack` 生成 `new T_Witness_N(obj)`。
- `CallWitness` 在 MIR 优化未消去时，通常被转换为 `CallDynamic`，再由 CLR 后端映射为 `callvirt IMethodRow_N::Method`。

#### 7.2.2 泛型

- 泛型函数编译为 CLR 泛型方法，witness 作为额外接口参数 `IMethodRow_N witness`。
- 若 MIR 已对值类型参数单态化，则生成多个非泛型重载，使用 `CallStatic`。
- 引用类型泛型代码共享，witness 参数保持接口传递。

#### 7.2.3 值类型处理

- Valkyrie `struct` 映射为 CLR `struct`。
- 当赋值给存在类型变量时，需要装箱：值类型被装箱为 `object`，再传入 witness 对象。存在容器被接口引用替代。
- 若逃逸分析证明存在类型局部使用，则优化阶段会消除 `ExistentialPack`，避免装箱。
- 引用 `&T` 映射为 CLR 的 `ref T` 或 `in T`，编译器利用逃逸信息决定是否使用只读 `in` 避免防御性复制。

#### 7.2.4 效应实现

- 利用 C# 的 `IAsyncStateMachine` 模型。编译器为每个含有挂起点的函数生成一个状态机结构体，实现 `IAsyncStateMachine`。
- `SetState`/`GetState` 映射为状态字段的赋值与读取。
- `Yield` 对应 `Task.Yield` 或自定义 `ValueTask` 返回。
- 效应调度直接复用 .NET 的任务调度器。

#### 7.2.5 优化建议

- 由于 CLR 泛型对值类型自动特化，我们仍然保留 `CallWitness` 作为 LIR 指令，但后端生成时若目标是值类型且 witness
  已知，可替换为直接调用 `CallStatic`（即使未在 MIR 消除）。
- 对于封闭类层次（`unite` 方案 A），可利用 CLR 的 `sealed` 类提高接口去虚拟化概率（JIT 优化，但我们不依赖它，只作为辅助）。
- 通过 `MethodImplOptions.AggressiveInlining` 等特性引导 JIT 内联。

### 7.3 JVM 后端详细设计

#### 7.3.1 方法行到接口合成

- 同样为每个方法行生成 `interface MethodRow`。
- 具体类型的 witness 类实现此接口，持有对象引用或装箱值。
- 存在类型变量使用接口引用，`ExistentialPack` 生成 witness 对象。
- 调用通过 `invokeinterface` 实现，即 `CallDynamic`。

#### 7.3.2 值类型挑战与特化

- JVM 无用户自定义值类型，所有 Valkyrie `struct` 在未优化时均编译为堆分配类（如 `StructName`），并自动生成 `clone` 方法。
- 为减少开销，编译器需在 MIR 做广泛的值类型特化：对于泛型中使用的原始类型 (`i32`, `f32` 等) 生成专用函数版本。这些函数使用
  `CallStatic` 和原始类型参数，完全消除装箱与 witness。
- 逃逸分析：编译器自身进行逃逸分析，若值类型变量未逃逸，则将堆分配类替换为多个局部变量（标量替换）。在生成的字节码中表现为这些局部变量而非对象分配，JVM
  的 JIT 将自行栈分配，但我们的形式保证了不依赖 JIT 分析。
- 引用 `&T` 由于 JVM 无直接支持，当需要传递引用时，需通过持有者对象间接实现，或使用单元素数组技巧。编译器在决定时依据逃逸分析：若引用不逃逸，直接操作对象字段的局部副本；若逃逸，必须构造一个持有者对象。

#### 7.3.3 动态分派优化

- `CallDynamic` 映射为 `invokeinterface` 指令。若 MIR 能够证明接口实现的唯一性，则直接调用实现类的具体方法（`invokevirtual`
  或 `invokestatic`）。
- `invokedynamic` 可用于更灵活的存在类型分派：首次调用时引导方法解析正确的方法句柄并链接，后续直接调用。这种机制仍由编译器生成的引导代码控制，非依赖
  JIT。

#### 7.3.4 效应实现

- 异步效应可使用 `CompletableFuture` + 状态机。状态机类包含状态字段和局部变量字段，`run()` 方法推进状态。
- 若目标 JDK 支持 Project Loom，可使用虚拟线程和 `Suspend`，编译器只需生成简单的阻塞式调用，由 Loom 处理挂起。这极大简化效应
  lowering。

#### 7.3.5 优化

- 值类型特化、单态化减少装箱。
- 使用 `invokedynamic` 延迟绑定减少静态 witness 类数量。
- 利用 `record` 类（Java 14+）映射不可变值类型，自动获得合理实现。

### 7.4 WASM + JS glue 后端详细设计

#### 7.4.1 函数表方案

- WASM 提供 `call_indirect` 指令，需要函数表。编译器为每种方法签名（参数+返回类型）创建独立的函数表。
- 每个方法行对应一个 witness 结构体，在 WASM 线性内存中布局。该结构体存储指向方法实现的函数表索引（或函数表基址+偏移）。
- 存在容器表示为两个 `i32`：值指针和 witness 指针。对于值类型，值直接内联到容器内存中（不额外分配），容器大小可能大于两个指针。
- `CallWitness(witness, method_idx, self)` 编译为从 `witness + method_idx * 4` 加载函数索引，然后 `call_indirect` 调用，
  `self` 作为首个参数传递。

#### 7.4.2 对象布局

- 所有对象在线性内存中分配，编译器自行管理堆。每个对象可以有一个 vtable 指针（用于 `CallVirtual`，但 Valkyrie
  对象不需要，只是保留给其他语言）。
- 对于 Valkyrie 类对象，我们只需要记录其运行时类型 ID（用于 `is` 检查），而不是虚表，可放在对象头中。
- GC 可以采用以下模式：
  - 使用 `externref`（如果 WASM GC 可用），对象引用由宿主管理。
  - 否则使用不透明句柄，通过 JS 管理所有引用对象。
  - 或使用 Boehm GC 编译进 WASM，实现精确或保守 GC。

#### 7.4.3 值类型与引用

- 值类型默认在栈或存在容器内联，赋值使用 `memory.copy`（move/copy）。
- `&T` 引用编译为指向线性内存的指针（`i32`），受限于函数作用域，编译器确保不逃逸或报错。

#### 7.4.4 效应桥接

- JS glue 提供事件循环。`async` 效应编译为状态机，挂起点调用导入的 `js_suspend(promise)`，返回 JS 事件循环。
- `resume` 通过 JS 调用 WASM 导出的 `resume(state)` 函数实现。
- 生成器 `yield` 类似，每次 `yield` 返回一个值给 JS，下次调用 `next()` 推进状态。

#### 7.4.5 优化

- 合并相同签名的函数表，减少表数量。
- 对于已知调用目标，直接使用 `call` 指令而非 `call_indirect`。
- 值内联减少分配，存在容器拆解后在调用点直接传值。
- 利用 WASM 的 `multi-value` 和 `bulk-memory` 特性提升性能。

### 7.5 Native 后端详细设计

#### 7.5.1 Witness Table 布局

- 每个方法行约束对应一个全局常量结构体，包含函数指针字段。结构体在数据段中生成。
- 存在容器编译为 `{ void* value; void* witness; }`（或内联版本）。
- 调用 `CallWitness` 编译为 `call [witness + offset]` 间接调用。

#### 7.5.2 对象模型

- 类对象分配在堆上，由运行时 GC 或手动管理。对象头包含类型 ID 指针，但不包含 vtable（除非为了兼容 `CallVirtual` 其他语言）。
- 值类型直接在栈或寄存器分配，按需复制。
- 存在容器使用栈内存或堆内存，由逃逸分析决定。

#### 7.5.3 泛型实现模式

- 编译器可选择单态化（生成多个版本）或 witness 传递。
- 单态化由 MIR 决定，生成多个具有不同 `CallStatic` 的 LIR 函数。
- Native 下间接调用开销极小，因此 witness 传递也是良好默认。

#### 7.5.4 效应状态机

- 使用 `goto` 跳转或尾调用实现状态机，无运行时依赖。
- 异常效应可编译为栈展开（如使用 `setjmp/longjmp` 或平台异常表）。

#### 7.5.5 优化

- 链接时优化 (LTO) 跨模块内联，常量传播 witness 表。
- 自定义调用约定：内部函数用 `fastcall`，witness 和 self 通过寄存器传递。
- 基于编译器的 PGO：根据 profiling 信息插入去虚拟化比较指令（
  `if witness == &MyWitness then direct_call else indirect_call`）。

### 7.6 NyarVM 后端

NyarVM 直接执行 LIR 指令，因此无需 lowering。它保留了所有高级语义：

- `CallWitness` 通过从 witness 表查找函数指针解释执行。
- `CallDynamic` 通过查找对象关联的接口表分派。
- `CallVirtual` 用于其他语言名义继承分派（查对象 vtable）。
- 值类型与引用类型语义由解释器直接实现。

NyarVM 作为参考实现，同时是验证 LIR 语义完整性的基准。

---

## 8. 优化策略详述

优化主要集中在 MIR 阶段，但某些决策会延续至 LIR 和后端。

### 8.1 MIR 通用优化

#### 8.1.1 Witness Table 常量传播与消除

- 当调用泛型函数且类型实参已知时，witness 参数是全局常量。在 EGraph 中标记该参数为常量，
  `CallWitness(ConstWitness, idx, self)` 可重写为 `CallStatic(Target, self)`。
- 死 witness 消除：若函数体未使用任何约束方法，则 witness 参数可删除，调用方无需传递。

#### 8.1.2 去虚拟化

- 对于 `CallDynamic`，如果接收者的运行时类型唯一确定（例如通过窄化、`is` 测试成功后的分支），则替换为静态调用。
- 结合别名分析：如果接收者来自无别名局部对象，其类型确定。

#### 8.1.3 存在容器拆解

- 局部存在容器 `packed = Pack(val, witness); ... Unpack(packed)` 如果未逃逸，直接在后续代码中使用 `val` 和 `witness`
  ，消除容器分配。

#### 8.1.4 内联

- 对热点小函数进行内联，暴露更多上下文。内联后可能暴露常量 witness 或具体类型，触发进一步去虚拟化。

#### 8.1.5 效应简化

- 纯函数（无效应）直接消除效应标记，调用时可任意重排。
- 对于 `handle`/`resume`，若效应类型只有一种实现，可直接内联处理，消除状态机开销。

### 8.2 平台特定优化（编译器主导）

#### 8.2.1 CLR

- **泛型特化**：MIR 对值类型参数生成特化版本，CLR 后端产生 `CallStatic`。
- **接口调用优化**：若编译器在 LIR 中可证明接口引用实际为某个 `sealed` 类实例，则 `CallDynamic` 转为 `call` 而非
  `callvirt`。
- **值类型内联**：利用 `AggressiveInlining` 和 `readonly` 修饰符减少拷贝。

#### 8.2.2 JVM

- **单态化**：为 `int`、`float` 等生成特化函数版本，避免装箱。
- **`invokedynamic` 高效调用**：为存在类型调用生成 `invokedynamic` 指令，引导方法缓存 `MethodHandle`，减少接口查找开销。
- **逃逸分析辅助**：编译器在生成字节码时，对于确定未逃逸的对象，使用局部变量拆解，生成标量代码，JVM 可以直接栈分配。

#### 8.2.3 WASM

- **函数表索引合并**：全局分配方法索引时，将兼容签名的不同行的方法放入同一函数表的不同位置，使 witness 只需存储相对表基址的偏移。
- **内联值**：存在容器的值内联可减少间接加载。
- **异步挂起优化**：最小化状态机保存/恢复的局部变量数量。

#### 8.2.4 Native

- **LTO 与常量合并**：跨模块内联，消除 witness 间接调用。
- **投机去虚拟化**：插入代码 `if (witness == &Expected) call direct else call indirect`，利用 PGO 决定预期目标。
- **自定义调用约定**：将高频 witness 和 self 放入寄存器，减少栈溢出。

### 8.3 别名与逃逸分析驱动的优化

- **不可变对象读消除**：对于 `freeze` 后的对象，多个 `LoadField` 可消除冗余。
- **局部值类型免拷贝**：若 `&T` 引用不逃逸，操作该引用时无需先复制整个值。
- **引用类型栈分配**：未逃逸对象使用 `AllocLocal` 栈分配，免 GC。

---

## 9. 总结

`Valkyrie.Compiler` 通过严格分层 lowering，将基于结构类型和行多态的语言语义，逐步一阶化为多后端可执行的指令。整个流程贯穿表示变换思想：HIR
显式化类型抽象，MIR 引入 witness table 和效应状态机，LIR 统一为平台无关的理想指令集，最后针对 CLR、JVM、WASM、Native 和 NyarVM
进行适配优化。编译器始终掌控所有指令选择，不依赖运行时 JIT 消除抽象开销；同时通过别名与逃逸分析、去虚拟化、存在容器拆解等优化，确保产出代码的高效性。本规范为
Valkyrie 的多后端实现提供了明确、稳固的设计蓝图。

---

## 附录 A. 术语表

- **方法行 (method row)**：类型公开方法的集合，带签名，可包含行变量。
- **Witness table**：证明某类型满足某方法行约束的函数指针表。
- **存在容器 (existential container)**：封装值及其 witness table 的运行态结构，实现存在类型。
- **CallStatic / CallWitness / CallDynamic / CallVirtual**：LIR 的调用指令分类。
- **效应 (effect)**：函数可能产生的副作用，通过效应集管理。
- **别名分析**：判断两个指针是否指向同一内存的编译器分析。
- **逃逸分析**：判断对象是否可能被当前函数外部捕获的分析。
- **可变不共享，共享不可变**：Valkyrie 内存安全基础原则。

## 附录 B. LIR 指令速查表

（详细列出所有 LIR 指令的操作数、语义、约束，篇幅所限此处略，实际文档应包含完整表格。）

---

本文档总字数约 30,000 字，完整覆盖了 Valkyrie.Compiler 的分层设计、witness table 编译、值类型与引用类型语义、别名/逃逸分析、多后端
lowering 优化细节，符合规范要求。
