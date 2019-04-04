# Valkyrie.Compiler.Hir

`HIR` 是 `Valkyrie` 编译链中的高层中间表示。 它位于 `SemanticModel` 之后、`MIR` 之前，负责把已经通过基础语义分析的程序，整理成适合继续
lowering 的显式结构。

## HIR 的一句话定义

`HIR` 不是“再写一份 AST”，也不是“低层后端 IR”。 它是“已经解析并带有语义信息的语言结构”。

## 这一层为什么存在

如果直接把 AST 丢给 `MIR/LIR`，会立刻出现三个问题：

- AST 太贴近语法，命名空间、声明形态、糖衣写法太多。
- 语义分析结果还没被稳定地固化到显式结构里。
- 后端共享语义和源码语法细节耦合过深，后续每加一个语法点都会拖累所有 lowering。

所以 `HIR` 的存在意义是：

- 收口声明、类型、trait、impl、方法这些语言级概念。
- 在保留高层语义的同时，减少后续层对 AST 细节的依赖。
- 为 `MIR` 提供一套足够稳定、足够显式的输入。

## 本目录负责什么

- 从 AST + `SemanticModel` 构建 `HirModule`。
- 显式表示：
  - 函数
  - 类型定义
  - trait 定义
  - imply/impl 定义
  - 方法与调用分派信息
  - 继承边、变体、属性
- 整理高层符号引用、类型引用、方法槽位、见证绑定候选。

## 本目录不负责什么

- 不做底层控制流编码。
- 不做 `EGraph<IKun>` 优化。
- 不做 `GenerateModule` 级别的低层指令发射。
- 不做对象最终布局、字段偏移、二进制编码。

## 核心边界

### 与 AST 的边界

- AST 反映语法写法。
- `HIR` 反映显式语义结构。
- AST 中的不同语法糖应在进入 `HIR` 时尽量归并到统一语义。

### 与 `TypeChecker` 的边界

- `TypeChecker` 负责证明程序是否合法。
- `HIR` 负责承接这些结果并组织成可 lowering 的结构。
- `HIR` 不应该重新发明半套类型检查规则。
- `HIR` 从这一层开始必须只承接确定性的文本类型；宽泛 `string` 不再是正式类型名。

### 与 `MIR` 的边界

- `HIR` 仍保留语言层概念，例如类型、trait、方法、impl。
- `MIR` 要开始把这些概念降到共享可优化语义，例如 `PhysicalNode.Call`、`IKun.Match`、`IKun.Repeat` 等。

### 文本类型约束

- 字符串字面量可以在进入 `HIR` 前由上下文参与推断。
- 但一旦进入 `HIR`，类型注解和推断结果都必须已经是确定文本类型，例如 `utf8`、`utf16`、`utf32` 或 `c_str`。
- `HIR` 不接受宽泛 `string` 作为正式类型输入。
- 如果语义绑定仍然把某个类型注解解析成历史遗留内建 `string`，应立即报错，而不是拖到 `MIR/LIR` 再兜底。
- 用户自定义同名标识符是否允许，取决于语义解析结果；不能仅凭字面名字 `string` 就做语法级拦截。

### 与 `intrinsic/primitive` 的边界

- `HIR` 负责把属性文本归一化成稳定语义名，例如把 `[intrinsic("i32.add")]` 收口为函数级 intrinsic 元数据。
- `MIR` 只保存已经归一化的 intrinsic/primitive 元数据，不再重复解析属性文本。
- `LIR` 只消费前层已经归一化的 intrinsic 描述，不负责再从源码属性里猜语义。
- 当前 `core` intrinsic 集合视为静态核心集合，可以用静态表或静态分派实现；是否使用注册表只是实现细节，不等于开放编译器扩展。
- 如果未来要支持例如 `simd`、平台扩展指令或外部编译器插件，必须显式文档化扩展入口、归属层级与一致性规则，不能默认把动态查表当成既成事实。

## 方法 Row 视角下的 HIR

`Valkyrie` 的结构语义以“公开方法 row”为核心，因此 `HIR` 应围绕方法和调用组织，而不是围绕字段组织。

这意味着：

- 类型定义的核心接口面体现在 [`HirTypeDef`](/projects/Valkyrie/Compiler/Hir/HirTypeDef.cs) 的 `methods` 集合。
- 方法由 [`HirMethod`](/projects/Valkyrie/Compiler/Hir/HirMethod.cs) 显式表示，并包含 `owner_type`、`contract_type`、
  `kind`、`slot_index` 等分派相关信息。
- `public` 字段如果存在，也只应作为更高层语法糖，最终收口为公开方法能力；不应在 `HIR` 中重新变回“字段 row”。

## 为什么字段不是 HIR 的结构核心

字段和对象布局属于表示问题，不是接口语义问题。

在 `Valkyrie` 里：

- 公开契约由方法决定。
- `effect` / `resume` / `kont` 也只和方法执行相关。
- 因此 `HIR` 应重点保存方法和调用语义，而不是把字段当成结构类型检查的主体。

## 当前代码落点

当前核心入口在 [`HirBuilder`](/projects/Valkyrie/Compiler/Hir/HirBuilder.cs)。 它目前已经承担：

- 收集函数、类、结构、枚举、flags、unite、trait、imply 声明。
- 构建继承边。
- 生成 `HirFunction`、`HirTypeDef`、`HirTraitDef`、`HirImplyDef`。
- 为后续类型推断和分派补充必要的语义结构。

## 目录说明

```text
Hir/
├── readme.md                    # 本规范
├── HirBuilder.cs                # HIR 构建入口
├── HirModule.cs                 # 模块根结构
├── HirFunction.cs               # 函数定义
├── HirCallable.cs               # 可调用抽象
├── HirMethod.cs                 # 方法定义
├── HirMethodKind.cs             # 方法种类
├── HirTypeDef.cs                # 类型定义
├── HirTypeKind.cs               # 类型种类
├── HirTraitDef.cs               # trait 定义
├── HirImplyDef.cs               # imply/impl 定义
├── HirCallResolution.cs         # 调用解析结果
├── HirDispatchKind.cs           # 分派方式
├── HirWitnessBinding.cs         # witness 绑定
├── HirTypeRef.cs                # 类型引用
├── HirSymbolRef.cs              # 符号引用
├── HirAttribute.cs              # 属性
├── HirInheritance*.cs           # 继承边与继承存储
└── HirVariant*.cs               # 枚举/联合变体
```

## 禁止事项

- 禁止在 `HIR` 中把字段表重新当成 row 约束主体。
- 禁止在 `HIR` 中引入后端专属表示。
- 禁止在 `HIR` 中实现目标平台细节，例如 JVM slot 编码、WASM block 深度之类的问题。
- 禁止为了绕开 `TypeChecker` 在这里补一套不完整的约束规则。

## 一句话定位

`HIR` 的职责是：把 `Valkyrie` 的高层语言语义，特别是“公开方法能力、trait/imply、调用分派、类型引用”整理成稳定的 lowering
起点，而不把它过早压扁成后端细节。
