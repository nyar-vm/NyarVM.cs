# 📦 Acorn.Protobuf

Protocol Buffers（`.proto`）二进制格式编解码器。

## 📐 格式布局

### 字段编码（Field Encoding）

每个字段以 `tag + value` 的形式编码，tag 包含字段号和 wire type。

| 部分  | 说明     | 编码                                            |
|-------|----------|-------------------------------------------------|
| Tag   | 字段标识 | `(field_number << 3) \| wire_type`，Varint 编码 |
| Value | 字段值   | 根据 wire type 编码                             |

### Wire Type

| 值 | 名称            | 说明       | 用于                                              |
|----|-----------------|------------|---------------------------------------------------|
| 0  | Varint          | 变长整数   | int32/64, uint32/64, sint32/64, bool, enum        |
| 1  | Fixed64         | 固定 64 位 | fixed64, sfixed64, double                         |
| 2  | LengthDelimited | 长度前缀   | string, bytes, embedded messages, packed repeated |
| 5  | Fixed32         | 固定 32 位 | fixed32, sfixed32, float                          |

### Varint 编码

使用 LEB128 编码，每字节的最高位表示是否还有后续字节。

| 字节   | 说明                                    |
|--------|-----------------------------------------|
| byte 0 | bit 7=1 表示继续，bit 0-6 为数据位 0-6  |
| byte 1 | bit 7=1 表示继续，bit 0-6 为数据位 7-13 |
| ...    | 最多 10 字节（64位）                    |

### ZigZag 编码（用于 sint32/sint64）

| 原始值 | 编码值 |
|--------|--------|
| 0      | 0      |
| -1     | 1      |
| 1      | 2      |
| -2     | 3      |
| 2      | 4      |

编码公式：`encode(n) = (n << 1) ^ (n >> 31)`（32位）

### 长度前缀（Length-Delimited）

| 部分   | 大小   | 说明     |
|--------|--------|----------|
| Length | Varint | 数据长度 |
| Data   | N 字节 | 实际数据 |

## 🏗️ 核心类

| 类                      | 说明                    | 文件                                                                 |
|-------------------------|-------------------------|----------------------------------------------------------------------|
| `ProtobufMessageData`   | Protobuf 消息           | [Data/ProtobufMessageData.cs](Data/ProtobufMessageData.cs)           |
| `ProtobufFieldData`     | Protobuf 字段           | [Data/ProtobufMessageData.cs](Data/ProtobufMessageData.cs)           |
| `ProtobufConstants`     | Protobuf 常量           | [Data/ProtobufConstants.cs](Data/ProtobufConstants.cs)               |
| `WireType`              | Wire type 枚举          | [Data/ProtobufConstants.cs](Data/ProtobufConstants.cs)               |
| `ProtobufDecoder`       | Protobuf 解码器         | [Decode/ProtobufDecoder.cs](Decode/ProtobufDecoder.cs)               |
| `ProtobufEncoder`       | Protobuf 编码器         | [Encode/ProtobufEncoder.cs](Encode/ProtobufEncoder.cs)               |
| `ProtobufScanner`       | Protobuf 扫描器         | [Scanner/ProtobufScanner.cs](Scanner/ProtobufScanner.cs)             |
| `ProtobufFrameProtocol` | Protobuf 帧协议         | [Scanner/ProtobufFrameProtocol.cs](Scanner/ProtobufFrameProtocol.cs) |
| `ProtobufStringCodec`   | Protobuf 字符串编解码器 | [Codec/ProtobufStringCodec.cs](Codec/ProtobufStringCodec.cs)         |

## 📚 格式规范参考

- [Protocol Buffers Encoding](https://developers.google.com/protocol-buffers/docs/encoding)
- [Protocol Buffers Language Guide](https://developers.google.com/protocol-buffers/docs/proto3)
- [Protobuf Wire Format](https://protobuf.dev/programming-guides/encoding/)
