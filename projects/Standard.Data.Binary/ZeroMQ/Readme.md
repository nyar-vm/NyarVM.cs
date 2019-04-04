# 📦 Acorn.ZeroMQ

ZeroMQ 消息传输协议（ZMTP）编解码器。

## 📐 格式布局

### ZMTP 帧格式

| 字段   | 偏移   | 大小 | 说明   | 对应类                   |
|--------|--------|------|--------|--------------------------|
| Flags  | 0x00   | 1    | 帧标志 | `ZeroMQFrameData.Flags`  |
| Length | 0x01   | 1/8  | 帧长度 | `ZeroMQFrameData.Length` |
| Data   | 对齐后 | N    | 帧数据 | `ZeroMQFrameData.Data`   |

### 帧标志（Flags）

| 位 | 名称    | 说明                 |
|----|---------|----------------------|
| 0  | More    | 1=后续还有更多帧     |
| 1  | Long    | 1=使用 8 字节长度    |
| 2  | Command | 1=命令帧（非数据帧） |

### 短帧（长度 < 255）

| 字段  | 大小 | 说明     |
|-------|------|----------|
| Flags | 1    | 帧标志   |
| Size  | 1    | 数据长度 |
| Data  | N    | 帧数据   |

### 长帧（长度 >= 255）

| 字段  | 大小 | 说明             |
|-------|------|------------------|
| Flags | 1    | 帧标志（Long=1） |
| Size  | 8    | 数据长度（64位） |
| Data  | N    | 帧数据           |

### 消息类型

| 类型    | 说明     | 对应类                                |
|---------|----------|---------------------------------------|
| Data    | 数据消息 | `ZeroMQConstants.MessageType.Data`    |
| Command | 命令消息 | `ZeroMQConstants.MessageType.Command` |

### 命令类型

| 类型  | 说明      |
|-------|-----------|
| READY | 就绪命令  |
| ERROR | 错误命令  |
| PING  | 心跳 Ping |
| PONG  | 心跳 Pong |

## 🏗️ 核心类

| 类                  | 说明            | 文件                                                   |
|---------------------|-----------------|--------------------------------------------------------|
| `ZeroMQMessageData` | ZeroMQ 消息数据 | [Data/ZeroMQMessageData.cs](Data/ZeroMQMessageData.cs) |
| `ZeroMQFrameData`   | ZeroMQ 帧数据   | [Data/ZeroMQMessageData.cs](Data/ZeroMQMessageData.cs) |
| `ZeroMQConstants`   | ZeroMQ 常量     | [Data/ZeroMQConstants.cs](Data/ZeroMQConstants.cs)     |
| `ZeroMQDecoder`     | ZeroMQ 解码器   | [Decode/ZeroMQDecoder.cs](Decode/ZeroMQDecoder.cs)     |
| `ZeroMQEncoder`     | ZeroMQ 编码器   | [Encode/ZeroMQEncoder.cs](Encode/ZeroMQEncoder.cs)     |
| `ZeroMQScanner`     | ZeroMQ 扫描器   | [Scanner/ZeroMQScanner.cs](Scanner/ZeroMQScanner.cs)   |

## 📚 格式规范参考

- [ZMTP Specification](https://rfc.zeromq.org/spec/23/)
- [ZeroMQ RFC](https://rfc.zeromq.org/)
- [ZeroMQ Guide](https://zguide.zeromq.org/)
