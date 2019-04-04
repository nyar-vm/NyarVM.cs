# Valkyrie.Compiler.Lir

`LIR` 是 `Valkyrie` 编译链里的低层统一表示。 它的职责是把已经抽取好的 `MirTree` 继续降为 `Nyar Standard IR`，即当前的
`GenerateModule` 形式，供 `jvm/wasm/clr` 后端消费。

## LIR 的一句话定义

`LIR` 不是语义检查层，也不是目标平台编码层。 它是“多后端共享的低层程序表示”。

## 为什么需要 LIR

如果让各后端直接从 `MIR` 生成目标代码，会出现：

- 每个后端都要重复理解 `EGraph/IKun` 语义。
- 控制流、局部变量、调用、返回、导出等规则在三端重复实现。
- 很难保证 `jvm/wasm/clr` 真正共享一套正式主链。

所以 `LIR` 的作用是：

- 把共享语义进一步压低到可执行、可后端映射的统一形式。
- 在进入平台编码前，先把程序收束成一阶、显式、低层的模块结构。

## 本目录负责什么

- 将 `MirTree` 降为 `GenerateModule`。
- 组织低层函数、局部变量、指令、标签、控制流、调用和返回。
- 保存必要的低层类型与布局元数据，供后端编码使用。

## 本目录不负责什么

- 不重新做类型检查。
- 不重新发明高层语言语义。
- 不直接负责 `.class`、`.wasm`、CLR 二进制编码。
- 不在这里处理文本解析、语义约束或宏展开。

## 与相邻层的边界

### 与 `MIR` 的边界

- `MIR` 关心共享语义与优化空间。
- `LIR` 关心低层执行结构和后端映射稳定性。

### 文本类型边界

- 从 `LIR` 视角看，文本类型已经必须是完全确定的低层语义，不能再保留宽泛 `string`。
- `LIR` 只接受显式文本类型，例如 `utf8`、`utf16`、`utf32`、`c_str`。
- `LIR` 禁止根据“宿主平台上通常用什么字符串类型”去反推文本编码。
- 如果某个上游阶段仍把 legacy `string` 带入 `LIR`，应立即报错，不允许再做兼容兜底。

### 与后端的边界

- `LIR` 负责生成统一低层模块。
- 后端负责把这个低层模块映射到 JVM / WASM / CLR 各自的数据结构和编码格式。
- 如果某种能力无法在 `LIR` 形成统一表示，就不应轻易把它伪装成“已经正式支持”的语言特性。
- 对文本类型也同样如此：后端只能把 `utf8` / `utf16` / `utf32` / `c_str` 等既定语义映射到目标表示，不能在后端重新发明 `string`。

## 方法 Row 与 LIR 的关系

`row` 的本体是公开方法能力，它主要属于 `TypeChecker/HIR/MIR` 的语义世界。

到了 `LIR`：

- `row` 本身应当已经被消化为调用与分派决策。
- `public` 字段早已不再是结构语义主体，只剩实现所需的低层表示。
- `effect/kont` 也不再以抽象理论术语存在，而应表现为显式控制流、调用协议、状态机片段或其他一阶结构。

也就是说：

- `LIR` 不关心“字段是不是 row”。
- `LIR` 只关心“这里最终要怎样调用、跳转、存储、返回”。

## 为什么字段元数据仍会出现在 LIR

当前目录中有 [`LirFieldDef`](/projects/Valkyrie/Compiler/Lir/LirFieldDef.cs) 这类结构。 这不是说 `LIR` 把字段当成类型系统核心，而是因为：

- 一旦进入低层表示，对象实现仍可能需要字段、偏移、大小等运行时元数据。
- 这些信息属于布局和编码需要，不属于结构类型的 row 语义。

这条区别必须明确：

- 结构契约在高层以公开方法表达。
- 物理存储在低层以字段布局表达。
- 两者不能再混回去。

## 当前代码落点

当前关键结构：

- [`LirBuilder`](/projects/Valkyrie/Compiler/Lir/LirBuilder.cs) 负责 `MirTree -> GenerateModule`。
- [`LirModule`](/projects/Valkyrie/Compiler/Lir/LirModule.cs) 是对 `GenerateModule` 的轻量封装。
- `LirFieldDef` / `LirTypeDef` 用于携带低层类型与布局辅助信息。

## 目录说明

```text
Lir/
├── readme.md          # 本规范
├── LirBuilder.cs      # LIR 构建入口
├── LirModule.cs       # GenerateModule 封装
├── LirTypeDef.cs      # 低层类型定义
└── LirFieldDef.cs     # 低层字段布局元数据
```

## 对 effect / kont 的工程要求

如果未来 `effect/handler/resume` 更完整地进入三后端正式主链，`LIR` 应遵守这些原则：

- 只接受已经一阶化、显式化的控制表示。
- 不要求后端理解高阶 continuation 术语。
- 优先使用显式标签、局部状态、调用协议、状态机等可跨后端映射的表示。
- 避免把 full CPS 直接暴露给后端。

这是为了保证：

- `JVM`、`WASM`、`CLR` 能共享同一套 lowering 主线。
- 调试与验证仍然可控。
- 不因为抽象过高而把技术债推给每个后端。

## 禁止事项

- 禁止在 `LIR` 重新定义语言级 row 语义。
- 禁止在 `LIR` 保存只对单一后端成立的特殊指令约定。
- 禁止把高阶 continuation 直接当成后端契约。
- 禁止把布局元数据误写成类型系统规范。
- 禁止在 `LIR` 中重新引入宽泛 `string`，或把 legacy `string` 偷偷默认成 `utf8`。

## 一句话定位

`LIR` 的职责是：把已经收束好的共享语义进一步压低成 `jvm/wasm/clr` 都能稳定消费的统一低层模块，同时严格区分“高层方法能力语义”和“低层字段布局实现”。
