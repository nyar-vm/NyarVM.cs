# 📦 Acorn.MessagePack

MessagePack 二进制序列化格式编解码器。

## 📐 格式布局

### 格式标记

| 标记范围  | 类型            | 说明                    |
|-----------|-----------------|-------------------------|
| 0x00-0x7F | Positive FixInt | 正 fixint               |
| 0x80-0x8F | FixMap          | fixmap（0-15 个元素）   |
| 0x90-0x9F | FixArray        | fixarray（0-15 个元素） |
| 0xA0-0xBF | FixStr          | fixstr（0-31 字节）     |
| 0xC0      | Nil             | 空值                    |
| 0xC2      | False           | 布尔假                  |
| 0xC3      | True            | 布尔真                  |
| 0xC4-C6   | Bin8/16/32      | 二进制                  |
| 0xC7-C9   | Ext8/16/32      | 扩展类型                |
| 0xCA-CB   | Float32/64      | 浮点数                  |
| 0xCC-CF   | Uint8-64        | 无符号整数              |
| 0xD0-D3   | Int8-64         | 有符号整数              |
| 0xD4-D8   | FixExt1-16      | 固定扩展                |
| 0xD9-DB   | Str8/16/32      | 字符串                  |
| 0xDC-DD   | Array16/32      | 数组                    |
| 0xDE-DF   | Map16/32        | 映射                    |
| 0xE0-FF   | Negative FixInt | 负 fixint               |

## 🏗️ 核心类

| 类                 | 说明               | 文件                                                   |
|--------------------|--------------------|--------------------------------------------------------|
| `MsgPackData`      | MessagePack 数据   | [Data/MsgPackData.cs](Data/MsgPackData.cs)             |
| `MsgPackValue`     | MessagePack 值     | [Data/MsgPackData.cs](Data/MsgPackData.cs)             |
| `MsgPackConstants` | MessagePack 常量   | [Data/MsgPackConstants.cs](Data/MsgPackConstants.cs)   |
| `MsgPackDecoder`   | MessagePack 解码器 | [Decode/MsgPackDecoder.cs](Decode/MsgPackDecoder.cs)   |
| `MsgPackScanner`   | MessagePack 扫描器 | [Scanner/MsgPackScanner.cs](Scanner/MsgPackScanner.cs) |

## 📚 格式规范参考

- [MessagePack Specification](https://msgpack.org/)
