# 二进制框架典型用例

本文档提供 Nyar.Binary 二进制框架的典型用例，涵盖常见二进制格式的解析与写入。

## 1. 固定结构：GLB 头部

glTF 二进制格式（`.glb`）的头部解析。

### 数据结构

```csharp
[BinarySerializable(Endianness = Endianness.Little)]
public partial struct GlbHeader
{
    [Field(Order = 0, Length = 4)]
    public FixedBytes4 Magic;

    [Field(Order = 1)]
    public uint Version;

    [Field(Order = 2)]
    public uint Length;

    [Field(Order = 3, LengthField = nameof(Length))]
    public byte[] ChunkData;
}
```

### 使用

```csharp
// 解析
var buffer = new ByteBuffer(File.ReadAllBytes("model.glb"));
if (GlbHeader.TryRead(ref buffer, out var header))
{
    Console.WriteLine($"Magic: {header.Magic}");
    Console.WriteLine($"Version: {header.Version}");
    Console.WriteLine($"Length: {header.Length}");
}

// 写入
var newHeader = new GlbHeader
{
    Magic = new FixedBytes4("glTF"),
    Version = 2,
    Length = 0,
    ChunkData = Array.Empty<byte>()
};

var writer = new ByteBufferWriter(buffer);
newHeader.WriteTo(ref writer);
File.WriteAllBytes("output.glb", writer.ToArray());
```

---

## 2. 条件字段：MySQL 握手包

MySQL 协议握手包包含条件字段。

### 数据结构

```csharp
[BinarySerializable(Endianness = Endianness.Little)]
public partial struct MysqlHandshake
{
    [Field(Order = 0)]
    public byte ProtocolVersion;

    [Field(Order = 1, Length = 56)]
    public FixedBytes56 ServerVersion;

    [Field(Order = 2)]
    public uint ConnectionId;

    [Field(Order = 3, Length = 8)]
    public FixedBytes8 AuthPluginData;

    [Field(Order = 4, ConditionalOn = nameof(HasCapabilityFlags))]
    public ushort CapabilityFlags;

    public bool HasCapabilityFlags => ProtocolVersion == 0x0A;
}
```

### 使用

```csharp
var buffer = new ByteBuffer(mysqlHandshakeData);
if (MysqlHandshake.TryRead(ref buffer, out var handshake))
{
    Console.WriteLine($"Protocol: {handshake.ProtocolVersion}");
    Console.WriteLine($"Connection: {handshake.ConnectionId}");

    if (handshake.HasCapabilityFlags)
    {
        Console.WriteLine($"Capabilities: {handshake.CapabilityFlags:X}");
    }
}
```

---

## 3. 位域压缩：标志位

网络协议中常见的位域压缩。

### 数据结构

```csharp
[BinarySerializable]
public partial struct PackedFlags
{
    [Field(Order = 0)]
    public byte Raw;

    [BitField(BitOffset = 0, BitCount = 1)]
    public bool IsValid;

    [BitField(BitOffset = 1, BitCount = 3)]
    public byte Mode;

    [BitField(BitOffset = 4, BitCount = 4)]
    public byte Reserved;
}
```

### 使用

```csharp
var buffer = new ByteBuffer(new byte[] { 0b10110010 });
if (PackedFlags.TryRead(ref buffer, out var flags))
{
    Console.WriteLine($"IsValid: {flags.IsValid}");     // false (bit 0)
    Console.WriteLine($"Mode: {flags.Mode}");           // 1 (bits 1-3)
    Console.WriteLine($"Reserved: {flags.Reserved}");   // 11 (bits 4-7)
}
```

---

## 4. 偏移表：Live2D 模型

Live2D 模型文件使用偏移表定位各部分数据。

### 数据结构

```csharp
[BinarySerializable(Endianness = Endianness.Little)]
public partial struct Live2DFile
{
    [Field(Order = 0)]
    public uint Version;

    [Field(Order = 1)]
    public uint SectionOffset;

    [OffsetTable(OffsetField = nameof(SectionOffset), TargetType = typeof(SectionTable))]
    public SectionTable Sections;
}

[BinarySerializable]
public partial struct SectionTable
{
    [Field(Order = 0)]
    public int Count;

    [Field(Order = 1, LengthField = nameof(Count))]
    public SectionEntry[] Entries;
}

[BinarySerializable]
public partial struct SectionEntry
{
    [Field(Order = 0, Length = 32)]
    public FixedBytes32 Name;

    [Field(Order = 1)]
    public uint Offset;

    [Field(Order = 2)]
    public uint Size;
}
```

### 使用

```csharp
var buffer = new ByteBuffer(File.ReadAllBytes("model.moc3"));
if (Live2DFile.TryRead(ref buffer, out var file))
{
    Console.WriteLine($"Version: {file.Version}");
    Console.WriteLine($"Sections: {file.Sections.Count}");

    foreach (var entry in file.Sections.Entries)
    {
        Console.WriteLine($"  {entry.Name}: offset={entry.Offset}, size={entry.Size}");
    }
}
```

---

## 5. 代数帧：MySQL 响应帧

MySQL 协议的响应帧可能是多种类型之一。

### 数据结构

```csharp
public enum FrameType : byte
{
    Ok = 0x00,
    Error = 0xFF,
    Eof = 0xFE
}

[BinarySerializable]
[AlgebraicUnion(
    DiscriminatorField = nameof(Type),
    Cases = new[] { typeof(OkFrame), typeof(ErrorFrame), typeof(EofFrame) }
)]
public partial struct MysqlFrame
{
    [Field(Order = 0)]
    public FrameType Type;

    [Field(Order = 1, Optional = true)]
    public OkFrame Ok;

    [Field(Order = 2, Optional = true)]
    public ErrorFrame Err;

    [Field(Order = 3, Optional = true)]
    public EofFrame Eof;
}

[BinarySerializable(Endianness = Endianness.Little)]
public partial struct OkFrame
{
    [Field(Order = 0)]
    public byte Header;

    [Field(Order = 1)]
    public uint AffectedRows;

    [Field(Order = 2)]
    public uint LastInsertId;
}

[BinarySerializable(Endianness = Endianness.Little)]
public partial struct ErrorFrame
{
    [Field(Order = 0)]
    public byte Header;

    [Field(Order = 1)]
    public ushort ErrorCode;

    [Field(Order = 2, Length = 1)]
    public byte SqlStateMarker;

    [Field(Order = 3, Length = 5)]
    public FixedBytes5 SqlState;

    [Field(Order = 4)]
    public string Message;
}

[BinarySerializable(Endianness = Endianness.Little)]
public partial struct EofFrame
{
    [Field(Order = 0)]
    public byte Header;

    [Field(Order = 1)]
    public ushort Warnings;

    [Field(Order = 2)]
    public ushort StatusFlags;
}
```

### 使用

```csharp
var scanner = new AsyncFrameScanner<MysqlProtocol>(pipeReader);

await foreach (var frameUnion in scanner.ReadFramesAsync())
{
    frameUnion.Match(
        ok => Console.WriteLine($"OK: {ok.AffectedRows} rows affected"),
        err => Console.WriteLine($"Error {err.ErrorCode}: {err.Message}"),
        eof => Console.WriteLine("EOF")
    );
}
```

---

## 6. 指令流：WebAssembly

WebAssembly 模块包含指令流，需要自定义协议解析。

### 自定义协议

```csharp
public struct WasmProtocol : IFrameProtocol
{
    public int MinFrameSize => 1;

    public bool TryPeekFrameSize(ReadOnlySpan<byte> buffer, out int frameSize)
    {
        if (buffer.Length < 1)
        {
            frameSize = 0;
            return false;
        }

        var opcode = buffer[0];
        frameSize = GetInstructionSize(opcode);
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
        return true;
    }

    private static int GetInstructionSize(byte opcode)
    {
        return opcode switch
        {
            0x00 => 1,  // unreachable
            0x01 => 1,  // nop
            0x41 => 6,  // i32.const (LEB128)
            0x6A => 1,  // i32.add
            // ...
            _ => 1
        };
    }
}
```

### 指令类型

```csharp
public enum Opcode : byte
{
    Unreachable = 0x00,
    Nop = 0x01,
    I32Const = 0x41,
    I32Add = 0x6A,
    // ...
}

[AlgebraicUnion(DiscriminatorField = nameof(Opcode))]
public partial struct WasmInstruction
{
    public Opcode Opcode;

    public UnreachableInst Unreachable;
    public NopInst Nop;
    public I32ConstInst I32Const;
    public I32AddInst I32Add;
}

[BinarySerializable]
public partial struct I32ConstInst
{
    [Field(Order = 0)]
    public Opcode Opcode;

    [Field(Order = 1)]
    public int Value;
}
```

### 使用

```csharp
var buffer = new ByteBuffer(wasmBytes);
var scanner = new FrameScanner<WasmProtocol>(buffer);

while (scanner.TryReadNext(out var rawFrame))
{
    var instruction = WasmInstruction.FromFrame(rawFrame);

    instruction.Match(
        unreachable => Console.WriteLine("unreachable"),
        nop => Console.WriteLine("nop"),
        i32const => Console.WriteLine($"i32.const {i32const.Value}"),
        i32add => Console.WriteLine("i32.add")
    );
}
```

---

## 7. 嵌套结构：Protobuf 消息

Protocol Buffers 消息包含嵌套的字段结构。

### 数据结构

```csharp
[ProtoMessage]
public partial struct Person
{
    [ProtoField(1)]
    public string Name;

    [ProtoField(2)]
    public int Age;

    [ProtoField(3)]
    public Address Address;
}

[ProtoMessage]
public partial struct Address
{
    [ProtoField(1)]
    public string Street;

    [ProtoField(2)]
    public string City;
}
```

### 使用

```csharp
var decoder = new ProtobufDecoder<Person>();
var person = decoder.Decode(protoBytes);

Console.WriteLine($"{person.Name}, {person.Age} years old");
Console.WriteLine($"Lives in {person.Address.City}");
```