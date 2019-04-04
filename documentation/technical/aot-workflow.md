# AOT 编译工作流

## 编译管线总览

```
┌─────────────┐    ┌─────────────────┐    ┌──────────────────┐
│ .nyar 模块  │───▶│  优化与提取     │───▶│  平台抽象层 (PAL) │
│ (多方言IR)  │    │ (EGraph饱和+成本)│    │  目标三元组选择   │
└─────────────┘    └─────────────────┘    └──────────────────┘
                                                │
                     ┌──────────────────────────┼──────────────────────────┐
                     ▼                          ▼                          ▼
              ┌─────────────┐            ┌─────────────┐            ┌─────────────┐
              │ 原生 CPU 后端 │            │ 托管平台后端 │            │ DSL 专用后端 │
              │ x64/ARM64    │            │ Wasm / JS   │            │ SQL / Verilog│
              └─────────────┘            └─────────────┘            └─────────────┘
                     │                          │                          │
                     ▼                          ▼                          ▼
              ┌─────────────┐            ┌─────────────┐            ┌─────────────┐
              │ 系统链接器   │            │ 运行时胶水   │            │ 领域代码文件│
              │ (ELF/Mach-O/PE)│          │ (Wasm导入/JS)│            │ (.sql/.v)   │
              └─────────────┘            └─────────────┘            └─────────────┘
```

## 目标三元组

Nyar 采用 LLVM 风格的目标三元组描述目标环境：`<arch>-<vendor>-<os>[-<abi>]`

- **arch**：x86_64, aarch64, riscv64, wasm32, js (虚拟架构)
- **vendor**：unknown, apple, pc, nvidia
- **os**：linux, windows, macos, freebsd, none (裸机), wasm, js
- **abi**：gnu, msvc, eabi, emscripten

用户通过命令行指定：`nyar build --target=x86_64-pc-windows-msvc`

## 平台抽象层 (PAL)

PAL 为不同平台提供统一的接口，处理：

- 数据类型大小与对齐（如 long 在 Linux x64 为 8 字节，Windows 为 4 字节）
- 调用约定（System V AMD64 ABI, Windows x64 ABI, AAPCS64 等）
- 运行时库接口（如 memcpy, 数学库函数, 系统调用封装）
- 栈布局与 unwinding 信息（DWARF/PDB）

PAL 信息由目标描述文件提供，Nyar 内置常见目标描述，用户也可扩展。

## 原生 CPU 后端 (x64 / ARM64 / RISC-V)

### 降级到可代码生成的 LIR

在 AOT 流程中，经过优化的意图需完全降低到以下节点集合才可进入原生代码生成：

- Literal, 算术/比较运算, 位运算 (Standard)
- Load, Store (带内存顺序)
- Branch, Call, Return
- Alloc, Free (可映射到堆分配运行时)
- Phi (在指令选择时消除)
- Tuple, Project (映射到寄存器/栈打包)

任何残留的高层方言节点（如 Sort, Regex）必须先被降级规则替换。若无法完全降级，编译报错（除非用户指定保留 VM 后备）。

### 指令选择与寄存器分配

**指令选择**：通过重写规则将通用 LIR 节点映射到目标架构指令：

- `(Add I32 a b)` + 成本模型 → x64 `addl %b, %a`
- `(Load I32 (Add Ptr base offset))` → x64 `movl offset(%base), %dst`

**寄存器分配**：采用 SSA 破坏后的线性扫描或图着色算法。

**栈帧管理**：根据调用约定计算参数/局部变量栈布局，生成 prologue/epilogue。

### 平台差异处理

| 差异点 | Linux (GNU) | Windows (MSVC) | macOS (Darwin) |
|--------|-------------|----------------|----------------|
| 目标文件格式 | ELF | COFF/PE | Mach-O |
| 调用约定 | System V | Microsoft x64 | System V |
| PIC/PIE 默认 | 是 | 否 | 是 |
| 异常处理 | DWARF .eh_frame | SEH (.pdata/.xdata) | DWARF compact unwind |
| 系统库 | glibc 符号 | kernel32 / ucrt | libSystem |
| 数学库 | libm (需显式链接) | 内置于 ucrt | 内置于 libSystem |

Nyar 通过 PAL 配置选择正确的 ABI 和运行时调用。

### 运行时支持

AOT 编译的可执行文件需链接 Nyar 运行时库 (`libnyar_rt`)，提供：

- 堆分配器（nyar_alloc, nyar_free）
- 异常/效应处理桩（若程序未使用效应则省略）
- 启动代码（调用 main 函数并初始化运行时）

对于裸机或无 OS 环境，用户可提供自定义 PAL 描述，使 Nyar 生成不依赖 libc 的代码。

## 托管平台后端

### WebAssembly 后端

Wasm 后端将意图编译为符合 Wasm MVP 或 Wasm GC / Exception Handling 提案的模块。

**降级策略**：

- Wasm 是栈式虚拟机，Nyar 需将 SSA 形式的意图转换为 Wasm 的栈操作
- 控制流节点 (Branch, Loop) 映射到 Wasm 的 block, loop, br_if
- Call 映射到 Wasm call 指令（直接调用）或 call_indirect（通过 Witness Table）
- 内存操作映射到 Wasm 线性内存指令 (i32.load, i32.store)

**效应处理**：

- 对于无异步效应的程序，直接生成栈式 Wasm 代码
- 对于使用 Timer / Network 效应的程序，生成集成 async/await 的 JavaScript 胶水代码

**平台差异**：

- **WASI**：系统调用效应映射到 WASI 导入函数
- **Web 嵌入**：生成 ES 模块胶水代码，导出 WebAssembly.instantiate 接口

### JavaScript 后端

JS 后端将意图编译为人类可读或压缩的 JavaScript 代码。

**映射规则**：

| 意图节点 | JavaScript 代码模式 |
|------------|---------------------|
| (Literal 42) | `42` |
| (Add a b) | `(a + b) \| 0` (若 I32 语义需截断) |
| (Tuple a b) | `[a, b]` |
| (Branch cond then else) | `if (cond) { ... } else { ... }` |
| (Loop body) | `while (1) { ... }` |
| (Call f args) | `f(args)` |
| (Alloc size) | `new ArrayBuffer(size)` |
| (Perform effect) | `await fetch(...)` 或 `throw` |

**异步效应处理**：

Nyar 的代数效应 Perform 在 JS 后端会被降级为显式的 Promise 和 async/await。编译器分析效应依赖，生成最小闭包的 CPS 变换代码。

**平台优化**：

- 尾调用优化：当检测到尾递归模式且目标环境支持 PTC 时，生成 `return func()` 形式；否则转换为循环
- 内存模型：JS 的 TypedArray 提供接近原生的内存访问，但需小心边界检查

## DSL 专用后端

### SQL 后端

输入：Data 方言的意图（包含 Scan, Join, Filter, Aggregate 等）

输出：针对特定数据库方言（PostgreSQL, MySQL, SQLite, BigQuery）的 SQL 文本

EGraph 已进行关系代数等价变换（Join 顺序、谓词下推）。成本模型可结合目标数据库的统计信息估算代价。最终提取的最优意图直接漂亮打印为 SQL 字符串。

### Verilog / VHDL 后端

输入：Hardware 方言意图（Module, Reg, Always, Pipeline 等）

输出：可综合的 Verilog 或 VHDL 代码

特殊处理：

- 将 `Always @(posedge clk)` 翻译为 `always_ff` 块
- 将流水线意图展开为显式寄存器阶段
- 处理位宽推断与符号扩展规则

### 其他专用后端

| 后端 | 输出 | 用途 |
|------|------|------|
| LLVM IR | `.ll/.bc` | 复用 LLVM 优化与目标支持 |
| C 代码 | `.c` | 可移植中间代码，适合嵌入式 |
| SPIR-V / WGSL | 着色器代码 | GPU 计算 |

## 跨平台构建与分发

### 构建工具 nyar build

```bash
nyar build \
  --input src/ \
  --output dist/ \
  --target x86_64-pc-linux-gnu \
  --opt-level 2 \
  --backend native \
  --cost-model latency
```

- 多目标构建：可一次指定多个 `--target`，生成多平台二进制
- 外部依赖：通过 `.nyar.toml` 声明依赖模块，类似 Cargo/npm

### 应用打包

| 平台 | 产物 |
|------|------|
| Windows | `.exe` + 资源文件，可选 MSI 打包 |
| macOS | `.app` 目录结构，签名与公证支持 |
| Linux | AppImage / Flatpak 描述文件 |
| Web | `.wasm` + `.js` 胶水代码 |

## AOT 工作流全景

| 步骤 | 描述 | 产物 |
|------|------|------|
| 1. 前端编译 | 各语言前端生成 .nyar 模块 | .nyar 文件 |
| 2. 链接与优化 | Nyar 元编译器加载模块，执行跨方言 EGraph 优化 | 优化后的意图 |
| 3. 降级提取 | 基于目标成本模型，从等价类中提取最优 LIR 图 | LIR 意图 |
| 4. 平台适配 | PAL 根据目标三元组调整数据类型、调用约定 | 带平台标注的 LIR |
| 5. 代码生成 | 目标后端将 LIR 转换为目标代码 | 汇编/机器码/Wasm/JS 等 |
| 6. 链接 | 调用系统链接器或自定义链接器，生成可执行文件 | .exe, .elf, .wasm, .js |
| 7. 打包 | 可选，创建平台特定的分发包 | 安装包/容器镜像 |
