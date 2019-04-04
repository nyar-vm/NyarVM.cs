# Nyar.Types.Targets

## 目标

`Nyar.Types.Targets` 只负责定义 **Nyar 自己的目标约定**，不负责承载任何下游构建工具的专有语义。

这里必须长期保持两个边界：

1. `target` 始终是稳定的规范四元组 `arch-vendor-spec-abi`
2. 所有宿主方言、打包格式、分发渠道、商店规则都只能作为下游 `build` 配置附加属性

换句话说，Nyar 负责定义"编译身份"，下游工具负责定义"如何交付"。

## 第一原则

### `target` 不能变形

规范目标四元组固定为：

`CanonicalTarget = arch-vendor-spec-abi`

- `arch`：执行载体，如 `x86_64`、`aarch64`、`wasm32`、`clr`、`jvm`
- `vendor`：实现方或生态方，如 `microsoft`、`openjdk`、`apple`、`node`
- `spec`：平台规范或 OS，如 `windows`、`linux`、`mac_os`、`android`、`ios`、`web`、`wasi`
- `abi`：真实 ABI，如 `msvc`、`gnu`、`aapcs64`、`managed`、`wasip1`

以下都 **不得进入** `target`：

- 编辑器扩展宿主名
- 社交/短视频平台名
- 应用商店名
- 包格式名（如 `extension`、`app-package`）

这些都不是编译身份，而是宿主、打包或发布语义。

### `target` 中允许与禁止的字段边界

| 可以进入四元组               | 不得进入四元组                     |
|:-----------------------------|:-----------------------------------|
| 指令集架构（CPU / VM / GPU） | 宿主方言（浏览器、Node、小程序等） |
| 运行时实现方/生态提供方      | 打包格式                           |
| 操作系统 / 平台规范          | 分发渠道 / 应用商店                |
| 真实 ABI 规范                | 宿主专有 API 协议                  |
|                              | 审核规则 / 上架规则                |

## 核心模型

### 身份层

`CanonicalTarget`

- 负责解析、格式化、比较、别名归一化
- 是缓存键、输出目录键、后端选择键的基础
- 不关心小游戏平台、应用商店、上传渠道
- `CanonicalTarget` 只表达编译身份，不表达宿主协议、打包格式、分发渠道

### 派生层

`TargetProfile`

`TargetProfile` 是从 `CanonicalTarget` 派生出的 **Nyar 内部编译策略视图**，只保留真正影响编译管线的字段：

- `canonical_triple`：规范四元组字符串
- `target_mode`：编译模式（Dev / Prod）
- `backend_family`：后端家族（NyarVm / Clr / Jvm / Wasm / Spirv / Native / Gpu）
- `host_kind`：粗粒度宿主类别（NyarVm / Jvm / Dotnet / JavaScript / Native / Browser / Wasi / Gpu）
- `abi`：真实 ABI
- `entry_policy`：入口策略（默认入口名、包装策略）
- `artifact_policy`：默认产物策略（扩展名、默认发布格式、调试符号等）

这里的 `host_kind` 仍然只是 **粗粒度宿主类别**，用于编译期判断，不是下游构建工具的完整宿主协议。

`TargetProfile` 的边界：

- 可以表达编译器默认产物特征
- 可以表达默认入口约束
- 可以表达粗粒度宿主类别
- 不可以表达下游具体打包和分发工作流

### 构建层

`BuildOptions`

`BuildOptions` 是下游构建概念，不属于 Nyar 核心目标模型。

它由以下分组构成：

| 分组       | 职责                     | 示例值                                                                  |
|:-----------|:-------------------------|:------------------------------------------------------------------------|
| `emit`     | 编译输出细节             | `source_map`、`typescript`、`wat`、`msil`                               |
| `host`     | 宿主方言与宿主协议       | `browser`、`node`、`extension-host`、`mini-game`                        |
| `package`  | 打包格式                 | `directory`、`web-app`、`extension`、`app-package`、`mini-game-package` |
| `channel`  | 分发渠道                 | `self-hosted`、`marketplace`、`open-platform`                           |
| `assets`   | 资源布局与静态资源策略   | `root`、`strategy`                                                      |
| `signing`  | 签名、证书、provisioning | `enabled`、`certificate`、`provisioning_profile`                        |
| `manifest` | 目标宿主清单与元数据     | `format`、`fields`                                                      |

即：

`BuildTarget = CanonicalTarget + BuildOptions`

注意：`BuildTarget` 是下游配置概念，不应该反向污染 `Nyar.Types.Targets` 的命名与建模。

### 兼容层

以下概念只允许作为兼容投影存在，不再作为新设计中心：

- `TargetApi`
- `TargetEnvironment`
- `TargetRuntime`
- `TargetAbiProfile`
- `TargetOutputKind`
- `CompilationTarget`

## 为什么只保留这些

### Windows / Android / iOS 的差异主要在交付，不在 target

例如：

- Windows
  - `target`: `x86_64-pc-windows-msvc`
  - 下游附加属性：`package.format = app-package | directory`
- Android
  - `target`: `aarch64-unknown-android-aapcs64`
  - 下游附加属性：`package.format = app-package`
- iOS
  - `target`: `aarch64-apple-ios-aapcs64`
  - 下游附加属性：`package.format = app-package`

因此 Nyar 不应该为这些再生造一轮目标概念。

### 编辑器扩展 / 小游戏不是新的 target

这些平台最容易把人带偏，因为表面上都像 "JS" 或 "WASM"，但本质上是不同宿主协议。

例如：

- 编辑器扩展
  - 可能运行在 JS extension host
  - 也可能携带 Node-hosted Wasm
  - 交付格式是 `extension`
- 社交平台小游戏
  - 不是普通浏览器
  - 有平台专有 API、独立生命周期、包体约束、资源规则
- 短视频平台小游戏
  - 不是普通 Node
  - 有平台专有 API、独立清单、审核和上传规则

所以这些平台不能被编码进 `TargetArch` / `TargetAbi` / `TargetSpecification`，只能作为下游 build 配置中的宿主与发布语义。

### 同一 `target` 可以对应多个 `BuildTarget`

同一编译身份可以产生多种交付产物。例如：

- `target: "wasm32-unknown-web-wasm"` 可以同时产出：
  - `host.flavor = browser` + `package.format = web-app`
  - `host.flavor = mini-game` + `package.format = mini-game-package`
- `target: "x86_64-pc-windows-msvc"` 可以同时产出：
  - `package.format = directory` + `channel.kind = self-hosted`
  - `package.format = app-package` + `channel.kind = marketplace`

这些应建模为多个 `BuildTarget`，它们共享相同 `target`，仅附加 build 属性不同。

## BuildOptions 分层详解

### `emit` 分组

编译输出细节，控制编译器产物中额外生成的辅助文件：

| 字段          | 类型 | 说明                         |
|:--------------|:-----|:-----------------------------|
| `source_map`  | bool | 是否生成 Source Map          |
| `type_script` | bool | 是否生成 TypeScript 声明文件 |
| `wat`         | bool | 是否生成 WAT 文本输出        |
| `msil`        | bool | 是否生成 MSIL 文本输出       |

### `host` 分组

宿主方言与宿主协议，描述代码最终运行在哪类宿主环境：

| 字段            | 类型     | 说明                                                            |
|:----------------|:---------|:----------------------------------------------------------------|
| `flavor`        | string   | 宿主风味，如 `browser`、`node`、`extension-host`、`mini-game`   |
| `capabilities`  | string[] | 宿主能力标签，如 `dom`、`filesystem`、`lifecycle`、`bridge-api` |
| `module_system` | string   | 模块系统类型，如 `esmodule`、`commonjs`                         |

### `package` 分组

打包格式，定义最终交付产物的打包方式：

| 字段                 | 类型       | 说明                                                                                 |
|:---------------------|:-----------|:-------------------------------------------------------------------------------------|
| `format`             | string     | 打包格式，如 `directory`、`web-app`、`extension`、`app-package`、`mini-game-package` |
| `enable_sub_package` | bool       | 是否启用分包机制                                                                     |
| `extra`              | Dictionary | 额外打包配置，由具体 packager 解释                                                   |

### `channel` 分组

分发渠道，定义产物最终的分发和上架方式：

| 字段    | 类型       | 说明                                                       |
|:--------|:-----------|:-----------------------------------------------------------|
| `kind`  | string     | 渠道类型，如 `self-hosted`、`marketplace`、`open-platform` |
| `extra` | Dictionary | 渠道专有配置，由具体 publisher 解释                        |

### `assets` 分组

资源布局与静态资源策略：

| 字段       | 类型   | 说明                                            |
|:-----------|:-------|:------------------------------------------------|
| `root`     | string | 资源根路径                                      |
| `strategy` | string | 资源打包策略，如 `inline`、`bundle`、`external` |

### `signing` 分组

签名与证书配置：

| 字段                   | 类型    | 说明                      |
|:-----------------------|:--------|:--------------------------|
| `enabled`              | bool    | 是否启用代码签名          |
| `certificate`          | string? | 签名证书标识或路径        |
| `provisioning_profile` | string? | Provisioning Profile 路径 |

### `manifest` 分组

目标宿主所需清单与元数据生成规则：

| 字段     | 类型       | 说明                                         |
|:---------|:-----------|:---------------------------------------------|
| `format` | string     | 清单格式，如 `package.json`、`manifest.json` |
| `fields` | Dictionary | 需要注入的元数据键值对                       |

## Build Matrix 的职责

`BuildMatrix` 不应该重新发明 target 系统。

它只应该做三件事：

1. 读取多个 `BuildTarget` 条目
2. 以 `CanonicalTarget` 为主键展开目标组合
3. 将附加属性合并为一组待执行构建任务

也就是说：

- `TargetSystem` 负责"目标是什么"
- `BuildMatrix` 负责"有哪些构建项要跑"
- `Packager/Publisher` 负责"怎么打包和发布"

`BuildMatrix` 不应该：

- 自己重新定义 target 格式
- 自己维护一套和 `CanonicalTarget` 平行的目标词汇
- 把宿主方言、包格式、渠道规则偷偷塞回 target
- 重新解释 target 语法

`BuildMatrix` 与 packager/publisher 的边界：

- `BuildMatrix`：输入构建项列表，展开为可执行任务集合，输出是任务调度计划
- `Packager`：接收单个构建任务 + `BuildOptions.package`，产出打包产物
- `Publisher`：接收打包产物 + `BuildOptions.channel`，完成分发/上架

## 推荐心智模型

以后统一按四问建模：

1. 编译给谁？`CanonicalTarget`
2. 编译时怎么做策略派生？`TargetProfile`
3. 最终跑在什么宿主协议上？下游 `build.host`
4. 最终如何打包和分发？下游 `build.package` 与 `build.channel`

判断规则：

- 回答"机器码 / 字节码打给谁"时，改 `target`
- 回答"代码跑在谁的运行协议里"时，改 `host`
- 回答"怎么打包"时，改 `package`
- 回答"怎么上架 / 上传 / 分发"时，改 `channel`

## 端到端规划样例

以下用抽象形态展示分层足以覆盖各类现实平台：

### Browser Web App

```
target: "wasm32-unknown-web-wasm"
emit: { source_map: true, typescript: true }
host: { flavor: "browser" }
package: { format: "web-app" }
channel: { kind: "self-hosted" }
```

### 编辑器扩展

```
target: "wasm32-node-unknown-wasm"
emit: { source_map: true }
host: { flavor: "extension-host" }
package: { format: "extension" }
channel: { kind: "marketplace" }
```

### Windows 桌面应用

```
target: "x86_64-pc-windows-msvc"
emit: { source_map: true }
host: { flavor: "win32" }
package: { format: "app-package" }
channel: { kind: "marketplace" }
signing: { enabled: true }
```

### Android 应用

```
target: "aarch64-unknown-android-aapcs64"
emit: { source_map: false }
host: { flavor: "android-native" }
package: { format: "app-package" }
channel: { kind: "marketplace" }
signing: { enabled: true }
```

### iOS 应用

```
target: "aarch64-apple-ios-aapcs64"
emit: { source_map: false }
host: { flavor: "ios-native" }
package: { format: "app-bundle" }
channel: { kind: "marketplace" }
signing: { enabled: true, provisioning_profile: "..." }
```

### 社交平台小游戏

```
target: "wasm32-unknown-web-wasm"
host: { flavor: "mini-game", capabilities: ["bridge-api", "game-lifecycle"] }
package: { format: "mini-game-package", enable_sub_package: true }
channel: { kind: "open-platform" }
```

### 短视频平台小游戏

```
target: "wasm32-unknown-web-wasm"
host: { flavor: "mini-game", capabilities: ["bridge-api", "game-lifecycle"] }
package: { format: "mini-game-package", enable_sub_package: true }
channel: { kind: "open-platform" }
```

这些样例的重点是：

- `target` 保持稳定，不随宿主或渠道变化
- 平台差异下沉到 build 附加属性
- 不同小游戏平台可以共享编译目标，但不共享宿主协议
- 同一 `target` 可对应多种交付方式

## ArtifactPolicy 的边界

`ArtifactPolicy` 仍然可以表达 **Nyar 内部可预知的默认产物特征**，例如：

- 主扩展名（`.wasm`、`.nyar`、`.exe`、`.spv`）
- 是否生成调试符号
- 是否生成运行时配置
- 默认发布格式候选

但它不应该替代下游完整的宿主/包/渠道配置。

更准确地说：

- `ArtifactPolicy` 是默认能力和默认倾向
- 下游 `BuildOptions.package` / `BuildOptions.channel` 才是最终交付配方
- 最终是否选择特定的包格式和分发渠道，由下游 build 配置决定

## 宿主方言建模规则

### 宿主方言概念

宿主方言是开放字符串，不属于 Nyar 核心枚举：

| 宿主大类       | `host.flavor` 示例 | 说明                                     |
|:---------------|:-------------------|:-----------------------------------------|
| 浏览器         | `browser`          | 标准 Web API，DOM、Canvas、Fetch         |
| Node.js        | `node`             | Node.js API，fs、process、timers         |
| 编辑器扩展宿主 | `extension-host`   | 编辑器扩展 API，运行在 JS extension host |
| 小游戏宿主     | `mini-game`        | 平台专有桥接 API，独立生命周期           |

### 宿主协议差异应放入 `host` 而不是 `target`

宿主协议差异包括：

- 模块系统差异（ESModule vs CommonJS vs AMD）
- API 面差异（DOM vs Node API vs 桥接 API）
- 生命周期差异（Web 生命周期 vs 小游戏生命周期 vs 移动端生命周期）
- 资源规则差异（URL 加载 vs 文件系统 vs 资源包）

这些都属于 `BuildHostOptions`，不应编码到 `CanonicalTarget`。

### 宿主能力矩阵

下游工具可根据 `host.flavor` 和 `host.capabilities` 构建宿主能力矩阵：

| 能力标签           | 说明            | 示例宿主                  |
|:-------------------|:----------------|:--------------------------|
| `dom`              | DOM 操作        | browser                   |
| `canvas`           | Canvas 2D/WebGL | browser、mini-game        |
| `filesystem`       | 文件系统访问    | node、native              |
| `esmodule`         | ES Module 加载  | browser、node             |
| `game-lifecycle`   | 游戏生命周期    | mini-game                 |
| `bridge-api`       | 宿主桥接 API    | mini-game、extension-host |
| `mobile-lifecycle` | 移动端生命周期  | android、ios              |

## 打包与分发规划规则

### `package.format` 的职责

`package.format` 定义产物的打包方式，不定义编译目标：

| `package.format`    | 说明           | 对应场景               |
|:--------------------|:---------------|:-----------------------|
| `directory`         | 目录形式输出   | 本地开发、自托管       |
| `web-app`           | Web 应用打包   | 浏览器部署             |
| `extension`         | 扩展/插件打包  | 编辑器扩展             |
| `app-package`       | 应用包（通用） | 移动端、桌面端应用商店 |
| `mini-game-package` | 小游戏包体     | 社交/短视频平台小游戏  |
| `app-bundle`        | 应用 Bundle    | macOS/iOS 原生         |

### `channel.kind` 的职责

`channel.kind` 定义分发渠道类型，不定义编译目标：

| `channel.kind`  | 说明         | 对应场景       |
|:----------------|:-------------|:---------------|
| `self-hosted`   | 自托管分发   | 私有部署       |
| `marketplace`   | 应用市场分发 | 编辑器扩展商店 |
| `open-platform` | 开放平台分发 | 小游戏平台     |

### 签名、清单、上传和审核规则的归属

- `signing`：签名和证书 → `BuildSigningOptions`
- `manifest`：清单文件生成 → `BuildManifestOptions`
- 上传与审核规则：属于 publisher 的实现细节，不在目标系统中表达

## 反模式

以下方向都应视为抽象泄露或重复造轮子：

- 在 `Nyar.Types.Targets` 中出现任何具体下游工具名
- 在 `target` 中编码宿主平台名称
- 在 `BuildMatrix` 中重写一套平行的 target 解析器
- 把 `package/channel` 重新塞回 `TargetSpecification` 或 `TargetVendor`
- 为每个新发行平台都新增一组 Target 枚举
- 在核心枚举中塞入发行平台语义
- 在 `BuildMatrix` 中重复造 target 轮子

## 结论

理想架构应长期保持下面这条主线：

- Nyar 定义稳定 `CanonicalTarget`
- Nyar 从 `CanonicalTarget` 派生 `TargetProfile`
- 下游工具基于 `target + BuildOptions` 形成 `BuildTarget`
- `BuildMatrix` 只负责展开 build 组合，不重复定义 target
- 宿主方言、包格式、分发渠道始终不反向污染 Nyar 抽象
- 下游工具只能"采用" Nyar target 约定，不能"定义" Nyar target