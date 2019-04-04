# 📦 Acorn.Redis

Redis 序列化协议（RESP - Redis Serialization Protocol）编解码器。

## 📐 格式布局

RESP 是一种文本行协议，使用前缀字符标识数据类型。

### 数据类型前缀

| 前缀 | 类型       | 说明               | 对应类                          |
|------|------------|--------------------|---------------------------------|
| `+`  | 简单字符串 | 非二进制安全字符串 | `RedisMessageType.SimpleString` |
| `-`  | 错误       | 错误消息           | `RedisMessageType.Error`        |
| `:`  | 整数       | 有符号 64 位整数   | `RedisMessageType.Integer`      |
| `$`  | 批量字符串 | 二进制安全字符串   | `RedisMessageType.BulkString`   |
| `*`  | 数组       | 元素数组           | `RedisMessageType.Array`        |

### 简单字符串（Simple String）

```
+OK\r\n
```

| 部分   | 说明         |
|--------|--------------|
| `+`    | 类型前缀     |
| `OK`   | 字符串内容   |
| `\r\n` | 行尾（CRLF） |

### 错误（Error）

```
-ERR unknown command\r\n
```

| 部分                  | 说明     |
|-----------------------|----------|
| `-`                   | 类型前缀 |
| `ERR unknown command` | 错误内容 |
| `\r\n`                | 行尾     |

### 整数（Integer）

```
:1000\r\n
```

| 部分   | 说明     |
|--------|----------|
| `:`    | 类型前缀 |
| `1000` | 整数值   |
| `\r\n` | 行尾     |

### 批量字符串（Bulk String）

```
$6\r\nfoobar\r\n
```

| 部分     | 说明             |
|----------|------------------|
| `$`      | 类型前缀         |
| `6`      | 数据长度（字节） |
| `\r\n`   | 长度分隔符       |
| `foobar` | 实际数据         |
| `\r\n`   | 数据结束符       |

空批量字符串：

```
$0\r\n\r\n
```

Null 批量字符串：

```
$-1\r\n
```

### 数组（Array）

```
*2\r\n$3\r\nGET\r\n$3\r\nkey\r\n
```

| 部分    | 说明           |
|---------|----------------|
| `*`     | 类型前缀       |
| `2`     | 元素数量       |
| `\r\n`  | 数量分隔符     |
| 元素... | 递归编码的元素 |

Null 数组：

```
*-1\r\n
```

## 🏗️ 核心类

| 类                 | 说明           | 文件                                                 |
|--------------------|----------------|------------------------------------------------------|
| `RedisMessageData` | Redis 消息数据 | [Data/RedisMessageData.cs](Data/RedisMessageData.cs) |
| `RedisMessageType` | 消息类型枚举   | [Data/RedisMessageData.cs](Data/RedisMessageData.cs) |
| `RedisConstants`   | Redis 常量     | [Data/RedisConstants.cs](Data/RedisConstants.cs)     |
| `RedisDecoder`     | RESP 解码器    | [Decode/RedisDecoder.cs](Decode/RedisDecoder.cs)     |
| `RedisEncoder`     | RESP 编码器    | [Encode/RedisEncoder.cs](Encode/RedisEncoder.cs)     |
| `RedisScanner`     | RESP 扫描器    | [Scanner/RedisScanner.cs](Scanner/RedisScanner.cs)   |

## 📚 格式规范参考

- [Redis Protocol Specification](https://redis.io/docs/reference/protocol-spec/)
- [RESP Protocol Deep Dive](https://redis.io/docs/reference/protocol-spec/#resp-protocol-description)
