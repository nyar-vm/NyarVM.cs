# 📦 Acorn.MySQL

MySQL 客户端/服务器通信协议编解码器。

## 📐 格式布局

### MySQL 数据包头

| 字段       | 偏移 | 大小 | 说明                          | 对应类                       |
|------------|------|------|-------------------------------|------------------------------|
| Length     | 0x00 | 3    | 包体长度（小端序，最大 16MB） | `MySqlPacketData.Length`     |
| SequenceId | 0x03 | 1    | 包序号（0-255）               | `MySqlPacketData.SequenceId` |
| Payload    | 0x04 | N    | 包体数据                      | `MySqlPacketData.Data`       |

### 握手包（Handshake）

| 字段                | 大小         | 说明                 | 对应类                           |
|---------------------|--------------|----------------------|----------------------------------|
| ProtocolVersion     | 1            | 协议版本（10）       | `MySqlConstants.ProtocolVersion` |
| ServerVersion       | 以 null 结尾 | 服务器版本字符串     | -                                |
| ConnectionId        | 4            | 连接 ID              | -                                |
| AuthPluginDataPart1 | 8            | 认证插件数据第一部分 | -                                |
| Filler              | 1            | 填充（0x00）         | -                                |
| CapabilityFlags1    | 2            | 能力标志低 16 位     | -                                |
| CharacterSet        | 1            | 字符集               | -                                |
| StatusFlags         | 2            | 服务器状态标志       | `MySqlConstants.ServerStatus`    |
| CapabilityFlags2    | 2            | 能力标志高 16 位     | -                                |
| AuthPluginDataLen   | 1            | 认证插件数据长度     | -                                |
| Reserved            | 10           | 保留                 | -                                |
| AuthPluginDataPart2 | N            | 认证插件数据第二部分 | -                                |
| AuthPluginName      | 以 null 结尾 | 认证插件名称         | -                                |

### 命令包（Command）

| 字段     | 大小 | 说明     | 对应类                       |
|----------|------|----------|------------------------------|
| Command  | 1    | 命令类型 | `MySqlConstants.CommandType` |
| Argument | 变长 | 命令参数 | -                            |

### 常见命令类型

| 值   | 名称             | 说明           |
|------|------------------|----------------|
| 0x01 | COM_QUIT         | 关闭连接       |
| 0x02 | COM_INIT_DB      | 选择数据库     |
| 0x03 | COM_QUERY        | SQL 查询       |
| 0x04 | COM_FIELD_LIST   | 获取字段列表   |
| 0x16 | COM_STMT_PREPARE | 预处理语句     |
| 0x17 | COM_STMT_EXECUTE | 执行预处理语句 |

## 🏗️ 核心类

| 类                | 说明             | 文件                                               |
|-------------------|------------------|----------------------------------------------------|
| `MySqlPacketData` | MySQL 数据包     | [Data/MySqlPacketData.cs](Data/MySqlPacketData.cs) |
| `MySqlConstants`  | MySQL 协议常量   | [Data/MySqlConstants.cs](Data/MySqlConstants.cs)   |
| `MySqlPacketType` | 数据包类型枚举   | [Data/MySqlPacketData.cs](Data/MySqlPacketData.cs) |
| `MySqlDecoder`    | MySQL 协议解码器 | [Decode/MySqlDecoder.cs](Decode/MySqlDecoder.cs)   |
| `MySqlEncoder`    | MySQL 协议编码器 | [Encode/MySqlEncoder.cs](Encode/MySqlEncoder.cs)   |
| `MySqlScanner`    | MySQL 协议扫描器 | [Scanner/MySqlScanner.cs](Scanner/MySqlScanner.cs) |

## 📚 格式规范参考

- [MySQL Internals Manual - Client/Server Protocol](https://dev.mysql.com/doc/internals/en/client-server-protocol.html)
- [MySQL Protocol Documentation](https://dev.mysql.com/doc/dev/mysql-server/latest/PAGE_PROTOCOL.html)
