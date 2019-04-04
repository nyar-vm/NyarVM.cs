# MySQL 客户端-服务器协议帧

## 定位

MySQL 线协议定义了客户端与服务端之间 **每个数据包**的边界格式。核心规则简单：3 字节小端序载荷长度 + 1 字节递增序列号 + 载荷。

Sonic 的 MySQL 实现是一套 `IFramer`/`IUnframer`，负责最底层的"数据包帧封装/解帧"——比你写裸 Socket 少 4 字节 header 的手动处理。

## 为什么只做数据包层而不是 MySQL 客户端？

MySQL 协议有三层：

1. **数据包帧**：4 字节头部 + 载荷。最大 16MB，超出需要分包。
2. **握手/认证**：服务端发握手包（协议版本、salt），客户端回认证包（用户名、密码哈希）。
3. **命令/响应**：`COM_QUERY` → `ColumnDefinition` + `ResultSetRow` → `EOF/OK`。

完整的 MySQL 客户端需要处理 SSL 协商、认证插件（`caching_sha2_password` vs `mysql_native_password`）、CHARACTER_SET
协商、预编译语句（`COM_STMT_PREPARE`）、二进制协议结果集（`ProtocolBinary::ResultsetRow`）。

这些复杂度远超"帧封装"的职责边界。Sonic 提供的是一块积木——包帧封装。你需要用这块积木和其他积木（认证、命令编码、结果集解析）搭出你的
MySQL 客户端。或者把这层连到一个完整的 MySQL 驱动实现中。

## 为什么 MySQL 包帧与 PostgreSQL 不同？

两个协议的帧头设计反映了设计年代和哲学的差异：

|            | MySQL                   | PostgreSQL          |
|:-----------|:------------------------|:--------------------|
| 帧头大小   | 4 字节                  | 5 字节              |
| 长度字节序 | **小端序**（LE）        | **大端序**（BE）    |
| 序列号     | 1 字节递增              | 无（不关心顺序）    |
| 长度语义   | 仅载荷                  | 含自身 4 字节       |
| 类型标识   | 隐含在序列号/命令字节中 | **显式 1 字节类型** |

MySQL 的小端序来自 x86 原生端序（MySQL 最初是 x86 Linux 上的）。PostgreSQL 的大端序来自网络字节序惯例（TCP/IP 标准）。MySQL
的序列号是为检测 **丢包/乱序**设计的——这在今天的 TCP 上几乎不会发生，但设计于上世纪 90 年代的协议默认不信任传输层。

## 为什么 Framer 的序列号是有状态的？

这是 `MySqlPacketFramer` 区别于 `LengthPrefixedFramer` 的关键。每个 MySQL 数据包的序列号必须递增（对客户端，从 0
开始；对服务端，从握手包的序列号+1 开始）。

如果封帧器无状态，序列号管理就会泄漏到调用方——每次 `frame` 调用都需要传入序列号。而序列号递增是机械规则，封装在 Framer
内部更安全（不会传错）。

`reset_sequence` 的存在是因为收到服务端的 EOF/OK 包后，客户端可能需要重置序列号到 0。

## 不应该承担什么职责？

- **不应实现 MySQL 握手**：初始包、认证交换、SSL 升级——这些是应用层逻辑。
- **不应解析结果集**：`ColumnDefinition`、`ResultSetRow`、OK/EOF/ERR 包的类型判断和解析属于上层。
- **不应管理连接池**：连接池与协议帧无关。
- **不应自动分包**：超过 16MB 载荷直接抛 `FramingException`，由调用方决定如何拆分。自动拆分涉及语义——你拆出来的是 2 个有效
  MySQL 包还是 1 个拆坏的包？调用方知道上下文的意图。

## 与上下游的衔接

```
上游（认证/命令构造层）→ 构造载荷（如 COM_QUERY 的 SQL 文本）
    ↓
MySqlPacketFramer.frame(payload, writer)  → 加 4 字节 header（长度 LE + 序列号）
    ↓
下游（Socket.Send）→ 发送

接收方向：
上游（Socket.Receive）→ 喂入 TCP 字节
    ↓
MySqlPacketUnframer.feed(data) → 累积
MySqlPacketUnframer.try_get_next_frame() → header+payload 一起读出
    ↓
下游（认证/命令解析层）→ payload[0] 判断包类型，payload[1..] 解析内容
```

## 典型场景

- **MySQL 代理/审计**：透明拦截 MySQL 流量，不修改协议只需读写帧。`MySqlPacketUnframer` 解帧 → 检查 SQL →
  `MySqlPacketFramer` 重新封帧转发。
- **MySQL 协议压测工具**：手动构造恶意包（超长 payload、跳变序列号）测试 MySQL 服务端的容错边界。
- **嵌入式 MySQL 通信**：你的 IoT 网关直接通过 TCP 发送 `COM_QUERY` 到 MySQL，不引入 `MySqlConnector` 依赖。