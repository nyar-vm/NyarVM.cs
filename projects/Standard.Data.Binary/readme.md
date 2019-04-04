# Nyar.Protocol — 协议编解码包

本目录包含所有网络协议编解码相关的项目。所有协议包均构建于 [Nyar.Binary](../binary/Nyar.Binary/) 之上，遵循 `Encode`/
`Decode`/`Scanner`（或 `Data`）三件套结构。

## 项目列表

### 协议抽象基座

| 项目                              | 命名空间        | 说明                                                                          |
|:----------------------------------|:----------------|:------------------------------------------------------------------------------|
| [Nyar.Protocol](./Nyar.Protocol/) | `Nyar.Protocol` | 协议抽象基座，定义 `IMessage`、`ISession`、`ProtocolScanner` 等通用接口与工具 |

### 数据库协议

| 项目                                                    | 命名空间                   | 协议                                | 说明                            |
|:--------------------------------------------------------|:---------------------------|:------------------------------------|:--------------------------------|
| [Nyar.Protocol.MySQL](./Nyar.Protocol.MySQL/)           | `Nyar.Protocol.MySQL`      | MySQL Wire Protocol                 | MySQL 数据库通信协议编解码      |
| [Nyar.Protocol.PostgreSQL](./Nyar.Protocol.PostgreSQL/) | `Nyar.Protocol.PostgreSQL` | PostgreSQL Wire Protocol            | PostgreSQL 数据库通信协议编解码 |
| [Nyar.Protocol.Redis](./Nyar.Protocol.Redis/)           | `Nyar.Protocol.Redis`      | RESP (REdis Serialization Protocol) | Redis 序列化协议编解码          |

### 通用协议

| 项目                                                | 命名空间                 | 协议                                     | 说明                      |
|:----------------------------------------------------|:-------------------------|:-----------------------------------------|:--------------------------|
| [Nyar.Protocol.Protobuf](./Nyar.Protocol.Protobuf/) | `Nyar.Protocol.Protobuf` | Protocol Buffers                         | Google 的二进制序列化格式 |
| [Nyar.Protocol.ZeroMQ](./Nyar.Protocol.ZeroMQ/)     | `Nyar.Protocol.ZeroMQ`   | ZMTP (ZeroMQ Message Transport Protocol) | ZeroMQ 消息传输协议编解码 |

## 项目结构规范

每个协议包遵循统一的三件套结构：

```
Nyar.Protocol.XXX/
├── Data/              # 数据模型（常量、消息类型定义）
├── Encode/            # 编码器（数据结构 → 字节流）
├── Decode/            # 解码器（字节流 → 数据结构）
├── Scanner/           # 帧扫描器（流式帧切分，可选）
└── Readme.md          # 协议说明
```

## 核心依赖

```
Nyar.Protocol.XXX
    ├── Nyar.Protocol    （协议抽象基座）
    └── Nyar.Binary      （ByteBuffer、ICodec<T>、IFrameProtocol 等）
```

## 文档

| 文档                                                                | 说明                           |
|:--------------------------------------------------------------------|:-------------------------------|
| [框架概述](../../documentation/overview/nyar-binary-overview.md)    | Nyar.Binary 设计哲学与项目结构 |
| [架构设计](../../documentation/core-systems/binary-architecture.md) | 核心接口与基础设施             |
| [扩展点](../../documentation/development/binary-extensibility.md)   | 自定义协议包、编解码器、帧协议 |
| [典型用例](../../documentation/development/binary-examples.md)      | MySQL、Protobuf 等解析示例     |