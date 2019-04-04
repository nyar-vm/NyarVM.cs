# Nyar.Language.Valkyrie

Valkyrie 是 Nyar 生态中的一种现代通用编程语言，设计为与 NyarVM 深度集成，同时保持独立的语言特性。

## 核心特性

- **运行时**: 直接使用 NyarVM 作为默认运行时
- **AOT 支持**: 可编译到 WebAssembly 等后端
- **前端集成**: 从 `d:\RiderProjects\Oak.cs\vendors\Nyar.Language + Nyar.Syntax` 获取解析器基础设施

## 项目结构

- `Valkyrie.csproj` - 项目配置文件
- `README.md` - 项目说明文档

## 技术架构

### 前端

- **词法分析**: 基于 Nyar 语言基础设施
- **语法分析**: 基于 Nyar 语言基础设施
- **AST 转换**: 转换为 Nyar IKun 意图

### 后端

- **运行时**: NyarVM 解释执行
- **AOT 编译**: 支持编译到 WebAssembly 等目标

## 依赖关系

| 依赖                          | 用途                      | 路径                                                                                             |
|:------------------------------|:--------------------------|:-------------------------------------------------------------------------------------------------|
| `Nyar.Core`                   | NyarVM 核心、IR、方言体系 | `..\..\projects\Nyar.Core\Nyar.Core.csproj`                                                      |
| `Nyar.Language + Nyar.Syntax` | 解析器基础设施            | `d:\RiderProjects\Oak.cs\vendors\Nyar.Language + Nyar.Syntax\Nyar.Language + Nyar.Syntax.csproj` |

## 开发计划

1. **初始化项目结构** - ✅ 完成
2. **集成 Nyar 语言基础设施** - ✅ 完成
3. **实现 Valkyrie 词法/语法分析**
4. **实现 AST → IKun 转换**
5. **验证 NyarVM 运行时执行**
6. **实现 AOT 到 WebAssembly**

## 联系方式

- **团队**: nyar-team
- **代码仓库**: `d:\RiderProjects\NyarVM.cs\examples\Nyar.Language.Valkyrie\`
