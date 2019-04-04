# Nyar.Binary 常见问题

本文档汇总 Nyar.Binary 二进制框架的常见问题与解决方案。

## 协议解析问题

### Q1: 协议包含任意复杂的条件字段

**问题：** 如果前一个字段值在集合 `{1, 3, 5}` 中，则后面跟 4 字节，否则跟 2 字节。

**解决方案：** 用户只需在 `partial struct` 中实现一个 `bool ShouldReadExtendedField()` 方法，并在特性中用 `ConditionalOn = nameof(ShouldReadExtendedField)` 引用。生成器会在解析时调用该方法。

```csharp
[BinarySerializable]
public partial struct ConditionalStruct
{
    [Field(Order = 0)]
    public byte Type;

    [Field(Order = 1, ConditionalOn = nameof(ShouldReadExtended))]
    public uint Extended;

    [Field(Order = 2, ConditionalOn = nameof(ShouldReadStandard))]
    public ushort Standard;

    public bool ShouldReadExtended => Type is 1 or 3 or 5;
    public bool ShouldReadStandard => !ShouldReadExtended;
}
```

---

### Q2: 有非对齐的 3 字节整数

**问题：** 网络协议中常见的 3 字节整数。

**解决方案：** 框架提供内置 `U24LE` / `U24BE` 编解码器，或用户自定义 `ICodec<T>`。

```csharp
// 使用内置编解码器
[BinarySerializable]
public partial struct NetworkPacket
{
    [Field(Order = 0, Codec = typeof(U24LE))]
    public int Value24;
}

// 或自定义类型
public struct U24
{
    public int Value;
}

public struct U24Codec : ICodec<U24>
{
    public int GetSize(U24 value) => 3;

    public void Encode(U24 value, Span<byte> destination)
    {
        destination[0] = (byte)(value.Value & 0xFF);
        destination[1] = (byte)((value.Value >> 8) & 0xFF);
        destination[2] = (byte)((value.Value >> 16) & 0xFF);
    }

    public U24 Decode(ReadOnlySpan<byte> source)
    {
        return new U24
        {
            Value = source[0] | (source[1] << 8) | (source[2] << 16)
        };
    }
}
```

---

### Q3: 位压缩复杂：标志位跨越多个字节

**问题：** 标志位跨越多个字节，或者动态位宽。

**解决方案：** 使用 `BitFieldAttribute` 可以跨字节（指定连续位）。动态位宽需要自定义 `ICodec`。

```csharp
[BinarySerializable]
public partial struct CrossByteFlags
{
    [Field(Order = 0)]
    public ushort Raw;

    [BitField(BitOffset = 0, BitCount = 12)]
    public int Value;

    [BitField(BitOffset = 12, BitCount = 4)]
    public byte Flags;
}
```

---

### Q4: 变长嵌套 TLV

**问题：** 内部又是 TLV，深度未知。

**解决方案：** 用户标记数组字段 `[Field(LengthField = "tlvLength")]`，元素类型为另一个 `[BinarySerializable]` 结构体。生成器自动递归调用解析。对于流式 TLV，可以使用 `FrameScanner<T>` 配合自定义协议。

```csharp
[BinarySerializable]
public partial struct TlvContainer
{
    [Field(Order = 0)]
    public uint TotalLength;

    [Field(Order = 1, LengthField = nameof(TotalLength))]
    public TlvEntry[] Entries;
}

[BinarySerializable]
public partial struct TlvEntry
{
    [Field(Order = 0)]
    public byte Tag;

    [Field(Order = 1)]
    public ushort Length;

    [Field(Order = 2, LengthField = nameof(Length))]
    public byte[] Value;
}
```

---

### Q5: 需要随机跳转

**问题：** 如 ELF 节头表在偏移 0x20 处。

**解决方案：** 用户先正常解析头部结构体，拿到 `sectionHeaderOffset` 后，再调用 `buffer.Position = offset;` 然后解析节头表。框架的 `ByteBuffer` 允许随意移动位置。

```csharp
var buffer = new ByteBuffer(elfData);

if (ElfHeader.TryRead(ref buffer, out var header))
{
    buffer.Position = (int)header.SectionHeaderOffset;

    for (var i = 0; i < header.SectionCount; i++)
    {
        if (SectionHeader.TryRead(ref buffer, out var section))
        {
            Console.WriteLine($"Section: {section.Name}");
        }
    }
}
```

---

### Q6: 协议需要依赖先前帧的状态

**问题：** 如 HTTP/2 的 SETTINGS 影响后续帧。

**解决方案：** 用户将状态保存在实现 `IFrameProtocol` 的值类型中，该类型可以作为 `FrameScanner<TProtocol>` 的泛型参数传入，且允许内部可变（`ref struct` + 可变字段）。源生成器生成的协议会自动携带状态。

```csharp
public struct Http2Protocol : IFrameProtocol
{
    private int _maxFrameSize = 16384;
    private bool _settingsReceived = false;

    public int MinFrameSize => 9;

    public bool TryPeekFrameSize(ReadOnlySpan<byte> buffer, out int frameSize)
    {
        if (buffer.Length < 9)
        {
            frameSize = 0;
            return false;
        }

        var length = (buffer[0] << 16) | (buffer[1] << 8) | buffer[2];
        frameSize = 9 + Math.Min(length, _maxFrameSize);
        return buffer.Length >= frameSize;
    }

    public bool TryReadFrame(ReadOnlySpan<byte> buffer, out Frame frame)
    {
        if (!TryPeekFrameSize(buffer, out var size))
        {
            frame = default;
            return false;
        }

        frame = new Frame(size, buffer.Slice(0, size));

        if (buffer[3] == 0x04)
        {
            _settingsReceived = true;
        }

        return true;
    }
}
```

---

### Q7: 性能要求极高，需要 SIMD 批量扫描分隔符

**问题：** 需要高性能分隔符扫描。

**解决方案：** 用户可以选择在自定义 `IFrameProtocol.TryPeekFrameSize` 中使用 `Vector128` 加速。框架不限制底层操作。

```csharp
public struct SimdDelimiterProtocol : IFrameProtocol
{
    private readonly byte _delimiter;

    public SimdDelimiterProtocol(byte delimiter) => _delimiter = delimiter;

    public int MinFrameSize => 1;

    public bool TryPeekFrameSize(ReadOnlySpan<byte> buffer, out int frameSize)
    {
        if (Vector128.IsHardwareAccelerated && buffer.Length >= 16)
        {
            var target = Vector128.Create(_delimiter);
            var pos = 0;

            while (pos + 16 <= buffer.Length)
            {
                var chunk = Vector128.Create(buffer.Slice(pos, 16));
                var compare = Vector128.Equals(chunk, target);

                if (compare != Vector128<byte>.Zero)
                {
                    var mask = compare.ExtractMostSignificantBits();
                    var index = BitOperations.TrailingZeroCount(mask);
                    frameSize = pos + index + 1;
                    return true;
                }

                pos += 16;
            }
        }

        var idx = buffer.IndexOf(_delimiter);
        if (idx >= 0)
        {
            frameSize = idx + 1;
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

---

### Q8: 不想用源生成器，想手写所有逻辑

**问题：** 需要完全控制解析逻辑。

**解决方案：** 框架完全开放所有接口，用户可以完全手写 `IFrameProtocol` 和 `ICodec<T>` 实现，不使用任何特性。源生成器只是可选工具。

```csharp
public struct ManualStruct
{
    public int Value1;
    public string Value2;

    public static bool TryRead(ref ByteBuffer buffer, out ManualStruct value)
    {
        value = default;

        if (buffer.Remaining.Length < 4)
            return false;

        value.Value1 = BitConverter.ToInt32(buffer.Read(4));

        var len = buffer.Peek(1)[0];
        if (buffer.Remaining.Length < 1 + len)
            return false;

        buffer.Advance(1);
        value.Value2 = Encoding.UTF8.GetString(buffer.Read(len));

        return true;
    }

    public void WriteTo(ref ByteBufferWriter writer)
    {
        writer.Write(BitConverter.GetBytes(Value1));

        var bytes = Encoding.UTF8.GetBytes(Value2);
        writer.Write(new[] { (byte)bytes.Length });
        writer.Write(bytes);
    }
}
```

---

### Q9: 需要校验 CRC / 哈希，失败时跳过损坏帧

**问题：** 帧校验失败时需要跳过。

**解决方案：** 在 `TryReadFrame` 中，如果校验失败，可以返回 `false` 并同时输出一个"跳过长度"。框架允许协议实现报告错误并推进到下一个可能同步点。

```csharp
public struct CrcFrameProtocol : IFrameProtocol
{
    public int MinFrameSize => 8;

    public bool TryPeekFrameSize(ReadOnlySpan<byte> buffer, out int frameSize)
    {
        if (buffer.Length < 4)
        {
            frameSize = 0;
            return false;
        }

        frameSize = BitConverter.ToInt32(buffer.Slice(0, 4)) + 8;
        return buffer.Length >= frameSize;
    }

    public bool TryReadFrame(ReadOnlySpan<byte> buffer, out Frame frame)
    {
        if (!TryPeekFrameSize(buffer, out var size))
        {
            frame = default;
            return false;
        }

        var payload = buffer.Slice(4, size - 8);
        var expectedCrc = BitConverter.ToUInt32(buffer.Slice(size - 4, 4));
        var actualCrc = Crc32.Compute(payload);

        if (expectedCrc != actualCrc)
        {
            frame = new Frame(size, ReadOnlySpan<byte>.Empty);
            return false;
        }

        frame = new Frame(size, payload);
        return true;
    }
}
```

---

### Q10: 框架稳定吗？我能否扩展新功能而不改核心？

**问题：** 担心框架稳定性。

**解决方案：** 框架已稳定，所有扩展通过实现接口进行。你甚至可以注册自己的协议工厂（`IProtocolFactory`）让扫描器根据魔数动态选择协议，这不需要修改框架源码。

框架的稳定性体现在：

- **基础设施**（`ByteBuffer`、`ICodec<T>`、`IFrameProtocol`、管道适配器）自 1.0 起未发生断裂式变更
- **声明式特性系统** 的生成契约稳定，扩展不破坏旧代码
- **专用协议** 永远以独立包形式发布，不会污染核心抽象

Nyar.Binary **无需修改内核即可进化**。未来新增协议包只需遵循现有接口，而无需核心库为你妥协。