# 二进制框架扩展点

本文档描述 Nyar.Binary 的扩展机制。框架核心绝不尝试直接提供所有协议的完整解析器，但提供了构建它们的 **全部基础设施**。

## 1. 自定义编解码器 ICodec\<T\>

### 何时实现

- 自定义数值编解码（例如 3 字节整数、BCD、定长浮点）
- 需要加密/压缩/校验的字段（例如 `CRC32Codec`）
- 非标准编码格式

### 实现示例：BCD 编码

```csharp
public struct BcdCodec : ICodec<int>
{
    public int GetSize(int value) => 4;

    public void Encode(int value, Span<byte> destination)
    {
        for (var i = 0; i < 4; i++)
        {
            var digit = value % 100;
            destination[3 - i] = (byte)(((digit / 10) << 4) | (digit % 10));
            value /= 100;
        }
    }

    public int Decode(ReadOnlySpan<byte> source)
    {
        var result = 0;
        for (var i = 0; i < 4; i++)
        {
            var b = source[i];
            result = result * 100 + ((b >> 4) * 10) + (b & 0x0F);
        }
        return result;
    }
}
```

### 使用自定义编解码器

```csharp
[BinarySerializable]
public partial struct BcdField
{
    [Field(Order = 0, Codec = typeof(BcdCodec))]
    public int Value;
}
```

---

## 2. 自定义帧协议 IFrameProtocol

### 何时实现

- 非长度前缀的帧切分（分隔符、状态机）
- 需要帧头校验（CRC、序列号）
- 指令流中的操作码提取与长度计算
- 跨帧状态（HTTP/2 HPACK、SETTINGS）

### 实现示例：分隔符协议

```csharp
public struct DelimiterProtocol : IFrameProtocol
{
    private readonly byte _delimiter;

    public DelimiterProtocol(byte delimiter)
    {
        _delimiter = delimiter;
    }

    public int MinFrameSize => 1;

    public bool TryPeekFrameSize(ReadOnlySpan<byte> buffer, out int frameSize)
    {
        var index = buffer.IndexOf(_delimiter);
        if (index >= 0)
        {
            frameSize = index + 1;
            return true;
        }

        frameSize = 0;
        return false;
    }

    public bool TryReadFrame(ReadOnlySpan<byte> buffer, out Frame frame)
    {
        if (!TryPeekFrameSize(buffer, out var size))
        {
            frame = default;
            return false;
        }

        frame = new Frame(size, buffer.Slice(0, size - 1));
        return true;
    }
}
```

### 使用自定义协议

```csharp
var protocol = new DelimiterProtocol((byte)'\n');
var scanner = new FrameScanner<DelimiterProtocol>(buffer);

while (scanner.TryReadNext(out var frame))
{
    var line = Encoding.UTF8.GetString(frame.Payload);
    Console.WriteLine(line);
}
```

---

## 3. 专用协议包

框架鼓励将特定协议的实现封装为独立的 NuGet 包。

### 包结构示例：Nyar.Protocol.Protobuf

```
Nyar.Protocol.Protobuf/
├── Nyar.Protocol.Protobuf.csproj
├── Codec/
│   └── ProtobufVarintCodec.cs
├── Scanner/
│   └── ProtobufFrameProtocol.cs
├── Attributes/          # 由 Nyar.Binary 提供基础
├── Decode/              # 解码器
├── Encode/              # 编码器
└── Data/                # 数据模型
```

### Protobuf 协议实现

```csharp
public struct ProtobufFrameProtocol : IFrameProtocol
{
    public int MinFrameSize => 1;

    public bool TryPeekFrameSize(ReadOnlySpan<byte> buffer, out int frameSize)
    {
        var pos = 0;
        frameSize = 0;

        while (pos < buffer.Length)
        {
            var b = buffer[pos++];
            frameSize++;

            if ((b & 0x80) == 0)
            {
                return true;
            }
        }

        frameSize = 0;
        return false;
    }

    public bool TryReadFrame(ReadOnlySpan<byte> buffer, out Frame frame)
    {
        if (!TryPeekFrameSize(buffer, out var size))
        {
            frame = default;
            return false;
        }

        frame = new Frame(size, buffer.Slice(0, size));
        return true;
    }
}
```

---

## 4. 协议工厂

对于需要根据魔数动态选择协议的场景，可以实现 `IProtocolFactory`。

```csharp
public interface IProtocolFactory
{
    bool TryDetect(ReadOnlySpan<byte> buffer, out IFrameProtocol protocol);
}
```

### 实现示例：多协议检测

```csharp
public class ImageProtocolFactory : IProtocolFactory
{
    public bool TryDetect(ReadOnlySpan<byte> buffer, out IFrameProtocol protocol)
    {
        if (buffer.Length < 4)
        {
            protocol = default;
            return false;
        }

        if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
        {
            protocol = new PngProtocol();
            return true;
        }

        if (buffer[0] == 0xFF && buffer[1] == 0xD8)
        {
            protocol = new JpegProtocol();
            return true;
        }

        if (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46)
        {
            protocol = new GifProtocol();
            return true;
        }

        protocol = default;
        return false;
    }
}
```

---

## 5. 扩展最佳实践

### 保持核心稳定

- 所有扩展通过实现接口进行
- 不修改框架核心代码
- 新功能以独立包形式发布

### 复用基础设施

- 使用框架提供的 `ByteBuffer` 和 `ByteBufferWriter`
- 复用内置编解码器
- 遵循 `ICodec<T>` 和 `IFrameProtocol` 契约

### 文档与测试

- 每个专用协议包应有独立的 README
- 提供典型用例和测试用例
- 说明与核心框架的关系

---

## 6. 已有专用协议包

| 包 | 协议 | 说明 |
|---|---|---|
| `Nyar.Protocol.Protobuf` | Protocol Buffers | Google 的二进制序列化格式 |
| `Nyar.Protocol.Redis` | RESP | Redis 序列化协议 |
| `Nyar.Protocol.MySQL` | MySQL 协议 | MySQL 数据库通信协议 |
| `Nyar.Protocol.PostgreSQL` | PostgreSQL 协议 | PostgreSQL 数据库通信协议 |
| `Nyar.Protocol.ZeroMQ` | ZMTP | ZeroMQ 消息传输协议 |