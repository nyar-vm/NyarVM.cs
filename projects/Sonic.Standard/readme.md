# Sonic

Sonic 标准库运行时与 Source Generator 实现。

## 概述

Sonic 是 Hypersonic 属性门面库的配套实现库，提供运行时逻辑、数据结构、基础设施组件和 Source Generator。它消费 Hypersonic
定义的属性，生成编译期代码并提供运行时服务。

## 核心模块

| 模块            | 说明                                                                      |
|:----------------|:--------------------------------------------------------------------------|
| **Category**    | `Option<T>` 和 `Result<T, E>` 函数式错误处理                              |
| **Collection**  | `BTreeMap`、`IndexMap`、`Deque`、`RingBuffer` 等集合类型                  |
| **Command**     | CLI 框架：命令解析、帮助生成、中间件、REPL                                |
| **Config**      | 配置系统：多源 Provider、热更新、强类型绑定                               |
| **Data**        | 序列化（JSON/CSV/XML/MessagePack/Protobuf）、缓存、协议帧处理、搜索、存储 |
| **DataProcess** | 编解码、帧处理、序列化/反序列化、扫描管线                                 |
| **Database**    | 嵌入式数据库 `LightDB`：BTree 索引、WAL、事务、MVCC                       |
| **Finance**     | 货币、汇率、金额运算                                                      |
| **Flow**        | 事件总线、Pipeline、Saga 工作流                                           |
| **Interactive** | 交互式终端：选择、确认、进度条、REPL                                      |
| **Math**        | 向量、四元数、大整数、有理数、复数                                        |
| **Net**         | HTTP 服务器/客户端、WebSocket、RPC、SignalR、中间件                       |
| **Security**    | 认证、授权、审计、沙箱、加密                                              |
| **State**       | 响应式状态、Cron 调度、时钟抽象                                           |
| **Terminal**    | TUI 框架：控件、布局、主题、渲染                                          |
| **Simulation**  | ECS、物理、状态机、寻路                                                   |

## 依赖

- **Hypersonic** — 属性门面库
- **Microsoft.Extensions.\*** — 依赖注入、配置、选项
- **Sonic.SourceGenerator** — 编译期代码生成

## 许可证

MIT
