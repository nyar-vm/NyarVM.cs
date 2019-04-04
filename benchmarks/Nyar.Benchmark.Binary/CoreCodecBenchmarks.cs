using System.Buffers.Binary;
using Nyar.Binary.Frame;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Nyar.Binary.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}

#region ByteBuffer 读取基准

[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3)]
public class ByteBufferReadBenchmarks
{
    private byte[] _data = null!;

    [Params(1024, 65536)]
    public int DataSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data = new byte[DataSize];
        new Random(42).NextBytes(_data);
    }

    [Benchmark]
    public int ReadU8()
    {
        var buffer = new ByteBuffer(_data);
        var sum = 0;
        for (var i = 0; i < _data.Length; i++)
        {
            sum += buffer.ReadU8();
        }
        return sum;
    }

    [Benchmark]
    public int ReadU16LE()
    {
        var buffer = new ByteBuffer(_data);
        var sum = 0;
        var limit = _data.Length / 2;
        for (var i = 0; i < limit; i++)
        {
            sum += buffer.ReadU16LE();
        }
        return sum;
    }

    [Benchmark]
    public long ReadU32LE()
    {
        var buffer = new ByteBuffer(_data);
        long sum = 0;
        var limit = _data.Length / 4;
        for (var i = 0; i < limit; i++)
        {
            sum += buffer.ReadU32LE();
        }
        return sum;
    }

    [Benchmark]
    public long ReadU64LE()
    {
        var buffer = new ByteBuffer(_data);
        long sum = 0;
        var limit = _data.Length / 8;
        for (var i = 0; i < limit; i++)
        {
            sum += (long)buffer.ReadU64LE();
        }
        return sum;
    }
}

#endregion

#region LEB128 解码基准

[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3)]
public class Leb128DecodeBenchmarks
{
    private byte[] _smallData = null!;
    private byte[] _mediumData = null!;
    private byte[] _largeData = null!;

    [GlobalSetup]
    public void Setup()
    {
        _smallData = GenerateLeb128Data(1000, 0, 127);
        _mediumData = GenerateLeb128Data(1000, 128, 16383);
        _largeData = GenerateLeb128Data(1000, 16384, 2097151);
    }

    [Benchmark]
    public long DecodeSmallValues()
    {
        var buffer = new ByteBuffer(_smallData);
        long sum = 0;
        while (!buffer.IsEnd)
        {
            sum += (long)buffer.ReadLeb128U64();
        }
        return sum;
    }

    [Benchmark]
    public long DecodeMediumValues()
    {
        var buffer = new ByteBuffer(_mediumData);
        long sum = 0;
        while (!buffer.IsEnd)
        {
            sum += (long)buffer.ReadLeb128U64();
        }
        return sum;
    }

    [Benchmark]
    public long DecodeLargeValues()
    {
        var buffer = new ByteBuffer(_largeData);
        long sum = 0;
        while (!buffer.IsEnd)
        {
            sum += (long)buffer.ReadLeb128U64();
        }
        return sum;
    }

    [Benchmark]
    public long DecodeSignedI32()
    {
        var buffer = new ByteBuffer(_smallData);
        long sum = 0;
        while (!buffer.IsEnd)
        {
            sum += buffer.ReadLeb128I32();
        }
        return sum;
    }

    [Benchmark]
    public long DecodeZigZagI32()
    {
        var buffer = new ByteBuffer(_smallData);
        long sum = 0;
        while (!buffer.IsEnd)
        {
            sum += buffer.ReadZigZagLeb128I32();
        }
        return sum;
    }

    [Benchmark]
    public long DecodeBatchAllInOne()
    {
        var span = new ReadOnlySpan<byte>(_smallData);
        long sum = 0;
        var pos = 0;
        while (pos < span.Length)
        {
            var value = ReadLeb128U64Fast(span, ref pos);
            sum += (long)value;
        }
        return sum;
    }

    private static ulong ReadLeb128U64Fast(ReadOnlySpan<byte> span, ref int pos)
    {
        var value = 0UL;
        var shift = 0;

        while (pos < span.Length)
        {
            var b = span[pos++];
            value |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                return value;
            }
            shift += 7;
        }

        return value;
    }

    private static byte[] GenerateLeb128Data(int count, ulong minValue, ulong maxValue)
    {
        var writer = new ByteBufferWriter(count * 10);
        var rng = new Random(42);

        for (var i = 0; i < count; i++)
        {
            var value = minValue + (ulong)(rng.NextDouble() * (maxValue - minValue + 1));
            writer.WriteLeb128U64(value);
        }

        return writer.ToArray();
    }
}

#endregion

#region ByteBufferWriter 写入基准

[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3)]
public class ByteBufferWriterBenchmarks
{
    private byte[] _data = null!;

    [Params(1024, 65536)]
    public int DataSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data = new byte[DataSize];
        new Random(42).NextBytes(_data);
    }

    [Benchmark]
    public byte[] WriteU8Batch()
    {
        var writer = new ByteBufferWriter(_data.Length);
        for (var i = 0; i < 1000; i++)
        {
            writer.WriteU8(_data[i]);
        }
        return writer.ToArray();
    }

    [Benchmark]
    public byte[] WriteU32LEBatch()
    {
        var writer = new ByteBufferWriter(4000);
        for (var i = 0; i < 1000; i++)
        {
            writer.WriteU32LE((uint)(i * 7919));
        }
        return writer.ToArray();
    }

    [Benchmark]
    public byte[] WriteLeb128U32_Small()
    {
        var writer = new ByteBufferWriter(1000);
        for (var i = 0; i < 1000; i++)
        {
            writer.WriteLeb128U32((uint)(i % 100));
        }
        return writer.ToArray();
    }

    [Benchmark]
    public byte[] WriteLeb128U32_Medium()
    {
        var writer = new ByteBufferWriter(2000);
        for (var i = 0; i < 1000; i++)
        {
            writer.WriteLeb128U32((uint)(i * 127));
        }
        return writer.ToArray();
    }

    [Benchmark]
    public byte[] WriteLeb128I32_Signed()
    {
        var writer = new ByteBufferWriter(5000);
        for (var i = 0; i < 1000; i++)
        {
            var value = (i % 2 == 0) ? i : -i;
            writer.WriteLeb128I32(value);
        }
        return writer.ToArray();
    }
}

#endregion

#region FrameScanner 基准

[MemoryDiagnoser]
public class FrameScannerBenchmarks
{
    private byte[] _frameData = null!;

    [Params(10, 100, 1000)]
    public int FrameCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        var writer = new ByteBufferWriter(FrameCount * 1024);

        for (var i = 0; i < FrameCount; i++)
        {
            var payloadSize = rng.Next(16, 512);
            var frame = new byte[4 + payloadSize];
            frame[0] = (byte)(payloadSize & 0xFF);
            frame[1] = (byte)((payloadSize >> 8) & 0xFF);
            frame[2] = (byte)((payloadSize >> 16) & 0xFF);
            frame[3] = (byte)i;
            rng.NextBytes(frame.AsSpan(4));
            writer.Write(frame);
        }

        _frameData = writer.ToArray();
    }

    [Benchmark]
    public int ScanFrames()
    {
        var scanner = new FrameScanner<TestFrameProtocol>(_frameData);
        var count = 0;
        while (scanner.TryReadNext(out _))
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    public int ScanWithPayloadCheck()
    {
        var scanner = new FrameScanner<TestFrameProtocol>(_frameData);
        var totalPayload = 0;
        while (scanner.TryReadNext(out var frame))
        {
            totalPayload += frame.Payload.Length;
        }
        return totalPayload;
    }

    private struct TestFrameProtocol : IFrameProtocol
    {
        public int MinFrameSize => 4;

        public bool TryPeekFrameSize(ReadOnlySpan<byte> buffer, out int frameSize)
        {
            frameSize = 0;
            if (buffer.Length < 4) return false;
            var payloadLength = buffer[0] | (buffer[1] << 8) | (buffer[2] << 16);
            frameSize = 4 + payloadLength;
            return true;
        }

        public bool TryReadFrame(ReadOnlySpan<byte> buffer, out Frame.Frame frame)
        {
            frame = default;
            if (buffer.Length < 4) return false;
            var payloadLength = buffer[0] | (buffer[1] << 8) | (buffer[2] << 16);
            if (buffer.Length < 4 + payloadLength) return false;
            var payload = payloadLength > 0 ? buffer.Slice(4, payloadLength) : ReadOnlySpan<byte>.Empty;
            frame = new Frame.Frame(4 + payloadLength, payload);
            return true;
        }
    }
}

#endregion

#region ByteBuffer vs Span 直接读取对比

[MemoryDiagnoser]
public class ByteBufferVsSpanBenchmarks
{
    private byte[] _data = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = new byte[65536];
        new Random(42).NextBytes(_data);
    }

    [Benchmark(Baseline = true)]
    public long SpanDirectRead()
    {
        var span = _data.AsSpan();
        long sum = 0;
        for (var i = 0; i < span.Length; i += 4)
        {
            if (i + 4 <= span.Length)
            {
                sum += BinaryPrimitives.ReadInt32LittleEndian(span[i..]);
            }
        }
        return sum;
    }

    [Benchmark]
    public long ByteBufferRead()
    {
        var buffer = new ByteBuffer(_data);
        long sum = 0;
        var limit = _data.Length / 4;
        for (var i = 0; i < limit; i++)
        {
            sum += buffer.ReadI32LE();
        }
        return sum;
    }
}

#endregion



