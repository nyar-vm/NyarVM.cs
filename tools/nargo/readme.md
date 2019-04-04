# nargo

## 概述

`nargo` 是面向 `Node.js` / Web 生态的**统一工程支配工具**。  
它的目标不是做一个“再包一层 `npm` 命令”的薄包装器，也不是只做一个 dev server，而是成为：

- `npm` 的替代品
- `vite` 的替代品
- `pnpm workspace` / `npm workspace` / `yarn workspace` 的兼容宿主
- `Valkyrie` 未来前端与全栈生态在 JavaScript 世界中的正式入口

从长期定位看，`nargo` 应该是未来 `valkyrie.v` 生态在 `Node` / Web 工程侧的预演工具，作用类似：

- `legend` 之于语言编译与运行工作流
- `legion` 之于通用包管理与工作区
- `nargo` 之于 `npm` 项目、前端工程、构建管线与开发服务器

一句话说，`nargo` 要**完全支配整个 npm 项目的构造**，而不是只负责安装依赖或启动本地服务器。

## 设计目标

### 1. 全生命周期接管

`nargo` 需要覆盖一个现代 `npm` 项目的完整生命周期：

- 初始化项目
- 识别并导入现有项目
- 安装与解析依赖
- 锁定版本与生成锁文件
- 组织 workspace
- 发现入口与任务
- 启动开发服务器
- 执行构建、测试、检查、发布
- 管理缓存、产物与增量状态

用户不应该再分别理解：

- `npm install`
- `pnpm install`
- `vite dev`
- `vite build`
- `tsc`
- `eslint`
- `postcss`
- `tailwind`

这些工具和阶段最终都应被 `nargo` 收束为一套统一模型。

### 2. 兼容现有 npm 项目

`nargo` 不是“只服务新项目”的理想化工具，它必须兼容现实世界已有项目，至少包括：

- `package.json`
- `package-lock.json`
- `pnpm-lock.yaml`
- `pnpm-workspace.yaml`
- `node_modules` 布局
- 传统 `scripts`
- 常见前端入口结构

兼容目标不是永远依赖这些格式，而是：

- 能识别
- 能导入
- 能解释
- 能逐步收束到 `nargo` 自己的正式项目模型

### 3. 统一工程模型

`nargo` 必须建立比 `package.json + scripts` 更强的正式工程模型，至少要显式表达：

- 工程类型
- 入口点
- 产物类型
- 运行目标
- workspace 成员关系
- 依赖图
- 构建目标
- dev server 能力
- 发布配置
- 兼容层状态

否则它永远只是“更聪明的 npm wrapper”，而不是项目构造支配者。

### 4. 与 `valkyrie.v` 对齐

`nargo` 不只是 JavaScript 生态工具，它还要服务未来的 `Valkyrie` 工程体系。

对齐关系建议如下：

- `legend`：语言编译与运行编排
- `legion`：通用包管理、工作区、注册表与发布基础设施
- `nargo`：Node / Web 工程构造、前端构建、开发服务器、产物装配
- `voa` / `Asgard`：面向 Web / App 的上层脚手架与宿主体验

也就是说，`nargo` 是 **Web 工程面** 的基础设施工具，而不是包管理基础设施本身。

## 核心定位

### 1. `nargo` 不是什么

`nargo` 不应：

- 重写整个 `npm registry` 协议
- 自己维护一套和 `legion` 平行的依赖解析器
- 自己维护一套和 `Nyar.Language` 平行的前端语言管线
- 只做 `vite` 风格 dev server
- 退化成“跑 scripts 的命令启动器”

### 2. `nargo` 是什么

`nargo` 应该是：

- `npm` 项目的统一工程宿主
- `Node` / Web 项目的工程图构造器
- 前端构建与开发服务器编排器
- 多语言资源处理的统一入口
- 兼容现有 JavaScript 工程生态的正式收敛层

## 与现有项目的关系

### 1. 与 `Nyar.Language` 的关系

`nargo` 不解析语言，不拥有语言语义；它应复用 `Nyar.Language`。

可复用能力：

- `JavaScriptLanguage`
- `TypeScriptLanguage`
- `Css`
- `Scss`
- `Tailwind`
- `Sql`
- `Valkyrie`
- `Awsl.Asgard` 中现有的 Web 构建、SSR、dev server、HMR、文档与资源处理经验

`nargo` 对 `Nyar.Language` 的使用方式应是：

- 发现工程中有哪些语言与资源类型
- 为这些语言选择正确的解析、检查、格式化与构建管线
- 统一组织构建图，而不是自己写一套 TypeScript / CSS / Tailwind 处理器

建议边界：

- `Nyar.Language` 负责“看懂源码与资源”
- `nargo` 负责“组织项目、依赖、入口、构建、开发与发布工作流”

### 2. 与 `Nyar.PackageRegistry` 的关系

`nargo` 不应自建 registry 层，而应直接复用 `Nyar.PackageRegistry`。

可直接复用能力：

- `IRegistry`
- `IRegistryResolver`
- `NpmRegistry`
- `JsrRegistry`
- `RegistryFactory`
- `RegistryResolver`
- 发布与 token 校验模型

对 `nargo` 而言，注册表层的职责是：

- 解析来源
- 拉取包元数据
- 查询版本
- 发布包
- 验证 token

这些职责已经天然属于 `Nyar.PackageRegistry`，不应在 `nargo` 中复制。

### 3. 与 `Nyar.PackageManager` 的关系

`nargo` 应建立在 `Nyar.PackageManager` 之上，而不是绕过它。

优先复用能力：

- `DependencyResolver`
- `PackageCache`
- `LockFile`
- `PackagePublisher`
- `RegistrySourceManager`
- `BuildOrchestrator`
- `BuildPlan`
- `WorkspaceDependencyGraph`
- `LegionManifest`
- `LegionsWorkspace`
- `ScriptRunner`
- 安全审计、许可证、签名与版本体系

建议关系是：

- `Nyar.PackageManager` 是通用包管理与工作区基础设施
- `nargo` 是面向 `npm` / Web 项目的专用工程宿主

也就是说：

- `legion` 解决“包管理基础设施”
- `nargo` 解决“Node / Web 工程构造与运行时工作流”

### 4. 与 `legion` 的关系

`nargo` 不是 `legion` 的替身，更像是其上层垂直工具。

分工建议：

- `legion`
  - 通用清单
  - 通用 workspace
  - 依赖解析
  - registry 访问
  - 包缓存
  - 发布
  - 安全审计
- `nargo`
  - `package.json` 导入与兼容
  - `npm` / `pnpm` 项目识别
  - Web 入口与资源图发现
  - `dev` / `build` / `preview` / `test` / `publish` 的 Web 工程工作流
  - HMR、SSR、静态资源、浏览器目标、Node 目标组织

## 工作模式

建议 `nargo` 支持四种工作模式。

### 1. Import 模式

检测到现有 `npm` 项目时：

- 读取 `package.json`
- 识别 `pnpm-workspace.yaml`
- 识别 lockfile
- 推断工程类型
- 生成 `nargo` 的内部项目图

这个模式必须把“兼容现有项目”放在第一优先级。

### 2. Native 模式

对新建项目，`nargo` 使用自己的正式配置模型，而不是把全部语义塞进 `package.json scripts`。

这意味着：

- `package.json` 仍可输出用于生态兼容
- 但真正的工程真相应由 `nargo` 的配置与项目图承载

### 3. Workspace 模式

用于多包仓库和复合前端工程。

需要兼容：

- `pnpm workspace`
- `npm workspace`
- `monorepo`

同时要能表达：

- 多应用
- 多库
- 多运行目标
- 多入口构建
- 混合 `Node` / Browser / SSR / Worker` 工程

### 4. Script-Bridge 模式

对历史项目，`nargo` 可以保留对原有 `scripts` 的桥接执行。

但这只是兼容层，不应成为长期正式模型。

## 工程模型

建议 `nargo` 的内部核心对象至少包含：

- `NargoWorkspace`
- `NargoProject`
- `NargoTarget`
- `NargoEntry`
- `NargoDependencyGraph`
- `NargoBuildGraph`
- `NargoDevSession`
- `NargoPackageAdapter`
- `NargoCompatibilityReport`

### 1. `NargoProject`

表达单个 Node / Web 工程的正式定义：

- 项目名
- 类型
- 根目录
- workspace 归属
- 入口
- 目标平台
- 输出类型
- 依赖集合
- 资源规则
- 构建配置
- 兼容层来源

### 2. `NargoTarget`

建议支持以下目标：

- `browser-app`
- `browser-lib`
- `node-app`
- `node-lib`
- `ssr-app`
- `worker`
- `isomorphic-lib`
- `cli`

### 3. `NargoEntry`

入口不应只看 `main` / `module` / `exports`，还应识别：

- HTML 入口
- TS / JS 入口
- SSR 入口
- Worker 入口
- CSS 入口
- 组件文档入口
- 测试入口

## 兼容策略

### 1. 对 `package.json` 的兼容

`nargo` 必须能读取并解释：

- `name`
- `version`
- `type`
- `main`
- `module`
- `exports`
- `scripts`
- `dependencies`
- `devDependencies`
- `peerDependencies`
- `optionalDependencies`
- `workspaces`

同时应把其中一部分语义提升为正式内部模型，而不是长期直接依赖这些字段驱动全部行为。

### 2. 对 `pnpm workspace` 的兼容

必须兼容：

- `pnpm-workspace.yaml`
- workspace include / exclude
- lockfile
- hoist / link 结构的现实差异

但 `nargo` 的目标不是复制 `pnpm` 的所有内部优化，而是：

- 能读懂
- 能导入
- 能共存
- 能平滑迁移

### 3. 对现有脚本生态的兼容

`nargo` 应允许用户先继续使用：

- `test`
- `lint`
- `dev`
- `build`
- `preview`

但这些名字最终应被映射到正式任务图，而不是无限期保留为一堆自由文本命令。

## 命令模型

建议 `nargo` 的顶层命令如下。

### 1. `init`

初始化 `nargo` 原生项目：

```bash
nargo init
nargo init web-app
nargo init node-lib
```

### 2. `import`

导入现有 `npm` / `pnpm` 项目：

```bash
nargo import
nargo import --from pnpm
nargo import --from npm
```

### 3. `install`

统一依赖安装与锁定：

```bash
nargo install
nargo install react
nargo install react --dev
nargo install vue --workspace packages/app
```

### 4. `dev`

启动开发会话，统一 dev server、watch、HMR、SSR bridge：

```bash
nargo dev
nargo dev web
nargo dev --ssr
```

### 5. `build`

统一构建：

```bash
nargo build
nargo build --target browser-app
nargo build --workspace packages/ui
```

### 6. `graph`

显示工程图、依赖图、入口图、构建图：

```bash
nargo graph
nargo graph deps
nargo graph entries
```

### 7. `doctor`

诊断兼容问题与迁移问题：

```bash
nargo doctor
nargo doctor --compat
nargo doctor --workspace
```

### 8. `publish`

复用 `Nyar.PackageRegistry` 和 `Nyar.PackageManager` 的发布链路：

```bash
nargo publish
nargo publish --tag next
```

## 内部分层

建议 `nargo` 自身按以下层组织。

```text
tools/nargo/
  ├── Cli/                  # 命令入口与参数解析
  ├── ProjectModel/         # NargoProject / Workspace / Target / Entry
  ├── Compatibility/        # npm / pnpm / package.json / lockfile 导入
  ├── Build/                # 构建图、目标、增量、缓存
  ├── DevServer/            # dev session / HMR / preview / SSR bridge
  ├── Scripts/              # 历史 scripts 桥接与任务映射
  ├── Registry/             # 对 Nyar.PackageRegistry 的薄适配
  ├── Package/              # 对 Nyar.PackageManager 的薄适配
  ├── Language/             # 对 Nyar.Language 的管线编排
  ├── Diagnostics/          # doctor / explain / compatibility report
  └── Documentation/        # 工程图与项目说明导出
```

注意：

- `Registry/` 不应自带新的 registry 实现
- `Package/` 不应自带新的 resolver
- `Language/` 不应自带新的 TS / JS / CSS 语义实现

## 对现有项目的复用映射

### 1. 复用 `Nyar.Language`

建议优先对接：

- `JavaScriptLanguage`
- `TypeScriptLanguage`
- `Css`
- `Scss`
- `Tailwind`
- `Valkyrie`
- `Awsl.Asgard` 的 dev server、SSR、HMR、文档能力

### 2. 复用 `Nyar.PackageRegistry`

建议直接复用：

- `NpmRegistry`
- `JsrRegistry`
- `RegistryResolver`
- `RegistryFactory`
- token 与 publish 模型

### 3. 复用 `Nyar.PackageManager`

建议直接复用：

- `DependencyResolver`
- `PackageCache`
- `LockFile`
- `WorkspaceDependencyGraph`
- `RegistrySourceManager`
- `BuildOrchestrator`
- `BuildPlan`
- `PackagePublisher`
- `SecurityAudit`
- `Version` 体系

## 关键能力域

### 1. 工程识别

自动判断一个目录是：

- 单包 `npm` 项目
- `pnpm workspace`
- `npm workspace`
- 纯静态站点
- SSR 工程
- Node CLI
- 混合 monorepo

### 2. 入口发现

需要自动发现：

- `index.html`
- `src/main.ts`
- `src/main.tsx`
- `src/server.ts`
- `worker.ts`
- CSS / SCSS / Tailwind 主入口

### 3. 构建图生成

`nargo` 的核心不是“跑一个 bundler”，而是生成统一构建图。

这个图应包括：

- 语言处理节点
- 资源处理节点
- 样式处理节点
- SSR 节点
- 浏览器产物节点
- Node 产物节点
- 发布装配节点

### 4. 开发服务器

开发服务器应只是工程图的一种实时执行模式，而不是工具的唯一中心。

建议支持：

- 静态资源服务
- 模块热替换
- SSR 预览
- 错误覆盖层
- 多入口并行开发
- workspace 联动重编译

### 5. 可观测性

`nargo` 应像 `legend` 一样提供统一可观测性：

- 当前工程模式
- 兼容来源
- 入口发现结果
- 构建图
- 缓存命中
- workspace 依赖关系
- registry 来源
- 产物位置

## 与 `npm` / `vite` / `pnpm` 的关系

### 1. 对 `npm`

`nargo` 不是只替代安装命令，而是替代：

- 项目初始化
- 包安装
- script 运行
- 生命周期钩子编排
- workspace 入口行为

### 2. 对 `vite`

`nargo` 不是只做一个更快的 dev server，而是替代：

- dev
- build
- preview
- 入口扫描
- 前端资源处理管线装配

### 3. 对 `pnpm`

`nargo` 不一定要复制 `pnpm` 的全部磁盘布局优化，但必须兼容其项目形态与 workspace 模型。

更准确地说：

- 兼容 `pnpm workspace`
- 能导入 `pnpm` 项目
- 能与 `pnpm` 项目共存
- 逐步把项目控制权上收至 `nargo`

## 配置模型

建议 `nargo` 最终拥有自己的正式配置文件，而不是永远只靠 `package.json`。

文件建议：

- `nargo.von`：单项目主配置
- `nargo.workspace.von`：工作区配置
- `nargo.lock`：正式锁文件

兼容文件：

- `package.json`
- `pnpm-workspace.yaml`
- `package-lock.json`
- `pnpm-lock.yaml`

原则是：

- 兼容层可以读取旧格式
- 真正的工程真相应逐步收口到 `nargo.*`

## 分阶段实施

### Phase 1：兼容导入器

- 识别 `package.json`
- 识别 `pnpm workspace`
- 导入依赖与 scripts
- 生成 `NargoCompatibilityReport`
- 建立 `NargoProject` / `NargoWorkspace` 内部模型

### Phase 2：统一安装与工作区

- 复用 `Nyar.PackageRegistry`
- 复用 `Nyar.PackageManager.DependencyResolver`
- 建立 `nargo install`
- 支持 workspace 依赖图
- 建立锁文件与缓存策略

### Phase 3：统一构建图

- 接入 `Nyar.Language` 的 JS / TS / CSS / SCSS / Tailwind`
- 建立入口发现
- 建立构建图
- 接管 `build`

### Phase 4：开发服务器与实时工作流

- 接入 `Awsl.Asgard` 的 dev server / HMR / SSR 经验
- 接管 `dev`
- 接管 `preview`
- 建立错误覆盖层与增量状态

### Phase 5：原生工程模型

- 引入 `nargo.von`
- 引入 `nargo.workspace.von`
- 引入 `nargo.lock`
- 让 `package.json` 从真相来源降级为兼容输出或兼容输入

### Phase 6：正式发布与生态收束

- 统一发布流程
- 接入 `npm` / `jsr` / 未来 `valkyrie.v` 注册表
- 支持脚手架、模板、项目升级与迁移助手

## 非目标

`nargo` 第一阶段不应：

- 自己实现 JavaScript / TypeScript 编译器
- 自己实现 CSS / SCSS / Tailwind 语义
- 自己实现 registry 协议
- 自己实现通用包管理器核心
- 一开始就取代现实世界所有 bundler / test runner / lint tool

它的第一阶段目标是：

- 建立正式工程模型
- 建立兼容导入能力
- 建立统一项目图
- 把现有工具链组织成可收敛的正式系统

## 一句话总结

`nargo` 的正确定位是：

- `Node` / Web 工程的统一支配入口
- `npm` / `vite` 的替代者
- `pnpm workspace` 等现有项目的兼容宿主
- 建立在 `Nyar.Language`、`Nyar.PackageRegistry`、`Nyar.PackageManager` 之上的前端工程总编排器
- 未来 `valkyrie.v` 生态在 JavaScript 世界中的工程预演
