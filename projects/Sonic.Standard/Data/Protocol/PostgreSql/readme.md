# PostgreSQL 前端/后端协议帧

## 定位

PostgreSQL 线协议比 MySQL 更"消息化"：每条消息由 1 字节 **类型标识** + 4 字节大端序 **总长度（含自身）** +
载荷组成。类型标识直接告诉你"这条消息是 Query、是 Parse、还是 AuthenticationOk"——不像 MySQL 需要上下文推断。

Sonic 的 PostgreSQL 实现是一套 `IFramer`/`IUnframer`，处理最底层的消息帧封装。

## 为什么 PostgreSQL 选择显式类型标识？

PostgreSQL 协议被设计为 **扩展友好**。第三方的 Foreign Data Wrapper、自定义 WAL 复制插件、逻辑解码（Logical
Decoding）——所有这些都需要在协议层添加新的消息类型而不破坏现有解析器。

1 字节类型标识 = 255 种消息类型，其中 PostgreSQL 核心只用了约 30 种。剩余 220+ 种预留给扩展。解析器不认识的消息类型直接按"
长度 + 跳过载荷"跳过，不阻塞协议流。

MySQL 的协议通过"上下文 + 包序列号 + payload[0] 命令字节"做类型判断——紧凑但不可扩展。PostgreSQL 选择了
**每一条消息自带完整元数据**，代价是每条消息多 1 字节头。

## Startup 消息的特殊性

PostgreSQL 的 Startup 消息 **没有类型字节**——它从 4 字节长度 + 4 字节协议版本号（`3.0` → `0x00 0x03 0x00 0x00`
）开始。这是协议中唯一类型=0 的有效消息。

`PostgreSqlFramer` 的构造参数是 `byte messageType`。对于 Startup 消息，传入 `0x00`；对于查询（`'Q'`），传入 `(byte)'Q'`。Framer
不对类型值做任何校验——它只是一个字节的透传通道。

## 为什么长度含自身？

协议设计中的一个经典选择：

- **长度不含自身**（MySQL、HTTP Content-Length）：消息总长度 = header_size + payload。
- **长度含自身**（PostgreSQL）：消息总长度 = 4 + payload（length 本身占 4 字节）。

含自身的优势：接收方读 4 字节得到 `N`，然后只需再读 `N - 4` 字节。接收方的缓冲区比较始终是 `remaining >= N`——不需要做
`header_size + payload` 的加法。防御性检查（`N < 4`）直接判非法。

代价：最小长度是 4（表示"4 字节的消息 = 没有 body"），而不是 0。语义上，"长度 4 的消息" vs "长度 0 的载荷"
——同一个状态的两种表达方式，但前者类型标识在 body 之前，后者 body 空了，这在协议中不是一个合法消息。

## 不熟悉的类型字节怎么处理？

`PostgreSqlUnframer.current_message_type` 暴露了原始类型字节。上层代码的标准处理模式是：

```
switch (unframer.current_message_type) {
    case 'R': → 处理 AuthenticationXxx
    case 'K': → 处理 BackendKeyData
    case 'Z': → 处理 ReadyForQuery
    default:  → 跳过（消息已在 try_get_next_frame 中消费，直接忽略）
}
```

这是 PostgreSQL 兼容性的基石——新版本新增的消息类型（如 PG17 新增的逻辑复制消息），旧版本的代码只需要 `default: break;`
就能正常工作。

## 不应该承担什么职责？

- **不应实现 Startup/Authentication 握手**：SCRAM-SHA-256、MD5 密码哈希、SSLRequest 全部是应用层逻辑。
- **不应处理 Extended Query 子协议**：Parse/Bind/Describe/Execute/Sync 的五步流程和消息配对是上层状态机的事。
- **不应做 COPY 子协议**：COPY 的流式数据传输有自己的帧格式。
- **不应实现 `ReadyForQuery` 的事务状态追踪**：`I`（idle）、`T`（in transaction）、`E`（error）三个状态属于连接状态管理。

## 与上下游的衔接

```
上游（协议构造层）→ 构造载荷
    ↓
PostgreSqlFramer(type).frame(payload, writer) → 1B type + 4B BE length + payload
    ↓
下游（Socket.Send）→ 发送

接收方向：
上游（Socket.Receive）→ 喂入 TCP 字节
    ↓
PostgreSqlUnframer.feed(data) → 累积 → try_get_next_frame()
    ↓（通过 current_message_type 获取类型字节）
下游（协议解析层）→ switch(type) 分发到 Authentication/Query/Parse 处理器
```

## 典型场景

- **PG 协议代理（PgBouncer 同类）**：接收客户端的 Startup → 转发到后端 PG → 透明透传所有消息。只需要读帧头知道消息边界，不解析
  body。
- **WAL 复制消费者**：连接到 PG 的 replication slot，接收 XLogData 消息（类型 `'w'`），解帧后获取 WAL 记录。不需要完整 PG 客户端。
- **PG 协议模糊测试**：构造非法长度（`length=0`）、非法类型字节、截断消息等，测试 PG 服务端的边界处理。