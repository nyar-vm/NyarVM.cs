# 🏗️ 二进制框架架构设计

本文档描述 Nyar.Binary（原 Acorn Binary Framework）的核心接口与基础设施。这些接口和类是框架的**稳定基础设施**，用户和生成器共同依赖，**不会随业务变化而修改**。

## 1. 内存抽象（零拷贝）

### ByteBuffer

`ByteBuffer` 是只读二进制数据的零拷贝抽象，支持任意位置跳转。

```csharp
public ref struct ByteBuffer
{
    public ReadOnlySpan<byte> Remaining { get; }
    public int Position { get; set; }
    public int Length { get; }

    public void Advance(int bytes);
    public ReadOnlySpan<byte> Read(int bytes);
    public ReadOnlySpan<byte> Peek(int bytes);
}
```

**特性：**
- 支持 `Position` 任意跳转，是实现偏移表解析的基础
- `Peek` 不移动位置，用于预读
- `Read` 移动位置并返回数据

### ByteBufferWriter

`ByteBufferWriter` 是写入二进制数据的零分配抽象。

```csharp
public ref struct ByteBufferWriter
{
    public Span<byte> GetSpan(int sizeHint = 0);
    public void Advance(int bytes);
    public void Write(ReadOnlySpan<byte> data);
}
```

**特性：**
- `GetSpan` 获取可写入的缓冲区
- `Advance` 提交写入的字节数
- `Write` 便捷方法，写入并自动推进

---

## 2. 编解码器接口（类型安全）

### ICodec\<T\>

`ICodec<T>` 定义了类型安全的编解码契约。

```csharp
public interface ICodec<T>
{
    int GetSize(T value);
    void Encode(T value, Span<byte> destination);
    T Decode(ReadOnlySpan<byte> source);
}
```

**内置标准编解码器：**

| 编解码器 | 说明 |
|---|---|
| `U8Codec` | 8 位无符号整数 |
| `U16LE` / `U16BE` | 16 位整数（小端/大端） |
| `U32LE` / `U32BE` | 32 位整数（小端/大端） |
| `U64LE` / `U64BE` | 64 位整数（小端/大端） |
| `I32LE` / `I32BE` | 32 位有符号整数 |
| `VarIntCodec` | 变长整数（LEB128） |
| `Float32LE` / `Float64LE` | 浮点数 |
| `U24LE` / `U24BE` | 24 位整数（网络协议常见） |

**自定义编解码器：**

用户可随时实现自己的 `ICodec<T>` 以处理非标准格式：

```csharp
public struct BcdCodec : ICodec<int>
{
    public int GetSize(int value) => 4;

    public void Encode(int value, Span<byte> destination)
    {
        // BCD 编码逻辑
    }

    public int Decode(ReadOnlySpan<byte> source)
    {
        // BCD 解码逻辑
        return result;
    }
}
```

---

## 3. 帧协议抽象（无虚调用）

### Frame

`Frame` 表示从字节流中切出的一帧数据。

```csharp
public readonly struct Frame
{
    public int Size { get; }
    public ReadOnlySpan<byte> Payload { get; }
    public ReadOnlySpan<byte> Raw { get; }
}
```

### IFrameProtocol

`IFrameProtocol` 定义了**如何从字节流中切出一帧**。

```csharp
public interface IFrameProtocol
{
    int MinFrameSize { get; }
    bool TryPeekFrameSize(ReadOnlySpan<byte> buffer, out int frameSize);
    bool TryReadFrame(ReadOnlySpan<byte> buffer, out Frame frame);
}
```

**设计要点：**
- 框架不假定任何特定的切分算法（长度前缀、分隔符、状态机）
- `TryPeekFrameSize` 用于预判帧大小，避免不必要的拷贝
- `TryReadFrame` 执行实际的帧切分

---

## 4. 帧扫描器（泛型 + 值类型）

### FrameScanner\<TProtocol\>

同步帧扫描器，用于从缓冲区中逐帧读取。

```csharp
public ref struct FrameScanner<TProtocol>
    where TProtocol : struct, IFrameProtocol
{
    public FrameScanner(ByteBuffer buffer, TProtocol protocol = default);
    public bool TryReadNext(out Frame frame);
}
```

**使用示例：**

```csharp
var scanner = new FrameScanner<MyProtocol>(buffer);
while (scanner.TryReadNext(out var frame))
{
    // 处理帧
}
```

### AsyncFrameScanner\<TProtocol\>

异步帧扫描器，基于 `PipeReader` 实现。

```csharp
public ref struct AsyncFrameScanner<TProtocol>
    where TProtocol : struct, IFrameProtocol
{
    public AsyncFrameScanner(PipeReader reader, TProtocol protocol = default);
    public ValueTask<ReadResult> ReadNextAsync(CancellationToken ct = default);
    public bool TryGetNextFrame(out Frame frame);
    public void AdvanceTo(SequencePosition consumed, SequencePosition examined);
}
```

**使用示例：**

```csharp
var scanner = new AsyncFrameScanner<MyProtocol>(pipeReader);
await foreach (var frame in scanner.ReadFramesAsync())
{
    // 处理帧
}
```

---

## 5. 写入侧支持（对称设计）

### FrameBuilder\<TProtocol\>

帧构建器，用于向 `PipeWriter` 写入帧。

```csharp
public ref struct FrameBuilder<TProtocol>
    where TProtocol : struct, IFrameProtocol
{
    public FrameBuilder(PipeWriter writer, TProtocol protocol = default);
    public bool TryBeginFrame(out Span<byte> headerSpace);
    public void CompleteFrame(Span<byte> headerSpan, ReadOnlySpan<byte> payload);
    public ValueTask<FlushResult> FlushAsync(CancellationToken ct);
}
```

**使用示例：**

```csharp
var builder = new FrameBuilder<MyProtocol>(pipeWriter);
if (builder.TryBeginFrame(out var header))
{
    // 写入帧头
    header[0] = 0x01;
    
    // 写入载荷
    var payload = Encoding.UTF8.GetBytes("Hello");
    builder.CompleteFrame(header, payload);
    
    await builder.FlushAsync();
}
```

---

## 6. 代数帧类型（Union）

### IFrameKind

标记接口，用于标识帧类型。

```csharp
public interface IFrameKind { }
```

### AlgebraicFrame\<TKind, TProtocol\>

代数帧类型，用于表达"一帧可能是多种类型之一"。

```csharp
public readonly struct AlgebraicFrame<TKind, TProtocol>
    where TKind : struct, IFrameKind
    where TProtocol : struct, IFrameProtocol
{
    public TKind Kind { get; }
    public Frame Frame { get; }

    // 由源生成器实现的模式匹配
    public TResult Match<TResult>(Func<...>...);
}
```

**使用示例：**

```csharp
await foreach (var frameUnion in scanner.ReadFramesAsync())
{
    frameUnion.Match(
        ok => Console.WriteLine("OK"),
        err => Console.WriteLine($"Error: {err.Message}"),
        eof => Console.WriteLine("EOF")
    );
}
```

---

## 7. 边界保证与稳定性

`Nyar.Binary` 的接口设计已经过多个项目的验证，并承载了多种复杂协议（MySQL、PostgreSQL、ZeroMQ、GLTF、SPIR-V、WASM）的手写和生成式解析。其稳定性体现在：

- **基础设施**（`ByteBuffer`、`ICodec<T>`、`IFrameProtocol`、管道适配器）自 1.0 起未发生断裂式变更
- **声明式特性系统** 的生成契约稳定，扩展不破坏旧代码
- **专用协议** 永远以独立包形式发布，不会污染核心抽象

Nyar.Binary **无需修改内核即可进化**。未来新增 `Nyar.Protocol.FlatBuffers`、`Nyar.Protocol.Bencode` 等包只需遵循现有接口，而无需核心库为你妥协。