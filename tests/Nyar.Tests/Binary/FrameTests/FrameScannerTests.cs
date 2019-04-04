using AcornFrame = Nyar.Binary.Frame;

namespace Nyar.Tests.Binary.FrameTests;

public struct TestLengthPrefixProtocol : AcornFrame.IFrameProtocol
{
    public int MinFrameSize => 4;

    public bool TryPeekFrameSize(ReadOnlySpan<byte> buffer, out int frameSize)
    {
        if (buffer.Length < 4)
        {
            frameSize = 0;
            return false;
        }

        var payloadLength = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(buffer);
        frameSize = 4 + payloadLength;
        return buffer.Length >= frameSize;
    }

    public bool TryReadFrame(ReadOnlySpan<byte> buffer, out AcornFrame.Frame frame)
    {
        if (!TryPeekFrameSize(buffer, out var size))
        {
            frame = default;
            return false;
        }

        var payload = buffer.Slice(4, size - 4);
        frame = new AcornFrame.Frame(size, payload, buffer.Slice(0, size));
        return true;
    }
}

public struct TestMagicProtocol : AcornFrame.IFrameProtocol
{
    private readonly byte _magic;

    public TestMagicProtocol(byte magic) => _magic = magic;

    public int MinFrameSize => 1;

    public bool TryPeekFrameSize(ReadOnlySpan<byte> buffer, out int frameSize)
    {
        var index = buffer.IndexOf(_magic);
        if (index >= 0)
        {
            frameSize = index + 1;
            return true;
        }

        frameSize = 0;
        return false;
    }

    public bool TryReadFrame(ReadOnlySpan<byte> buffer, out AcornFrame.Frame frame)
    {
        if (!TryPeekFrameSize(buffer, out var size))
        {
            frame = default;
            return false;
        }

        var payload = buffer.Slice(0, size - 1);
        frame = new AcornFrame.Frame(size, payload, buffer.Slice(0, size));
        return true;
    }
}

public class FrameScannerTests
{
    [Fact]
    public void TryReadNext_LengthPrefixProtocol_SingleFrame()
    {
        var writer = new AcornFrame.ByteBufferWriter(8);
        writer.WriteI32LE(4);
        writer.Write([0x01, 0x02, 0x03, 0x04]);

        var data = writer.WrittenData;
        var scanner = new AcornFrame.FrameScanner<TestLengthPrefixProtocol>(data);
        Assert.True(scanner.TryReadNext(out var frame));
        Assert.Equal(8, frame.Size);
        Assert.Equal(4, frame.Payload.Length);
        Assert.Equal(0x01, frame.Payload[0]);
        Assert.Equal(0x04, frame.Payload[3]);
    }

    [Fact]
    public void TryReadNext_LengthPrefixProtocol_MultipleFrames()
    {
        var writer = new AcornFrame.ByteBufferWriter(12);
        writer.WriteI32LE(2);
        writer.Write([0xAA, 0xBB]);
        writer.WriteI32LE(2);
        writer.Write([0xCC, 0xDD]);

        var data = writer.WrittenData;
        var scanner = new AcornFrame.FrameScanner<TestLengthPrefixProtocol>(data);

        Assert.True(scanner.TryReadNext(out var frame1));
        Assert.Equal(6, frame1.Size);
        Assert.Equal(2, frame1.Payload.Length);
        Assert.Equal(0xAA, frame1.Payload[0]);

        Assert.True(scanner.TryReadNext(out var frame2));
        Assert.Equal(6, frame2.Size);
        Assert.Equal(0xCC, frame2.Payload[0]);

        Assert.False(scanner.TryReadNext(out _));
    }

    [Fact]
    public void TryReadNext_LengthPrefixProtocol_IncompleteData()
    {
        var writer = new AcornFrame.ByteBufferWriter(8);
        writer.WriteI32LE(100);

        var partialData = writer.WrittenData.Slice(0, 3).ToArray();
        var scanner = new AcornFrame.FrameScanner<TestLengthPrefixProtocol>(partialData);
        Assert.False(scanner.TryReadNext(out _));
    }

    [Fact]
    public void TryReadNext_MagicProtocol()
    {
        var data = new byte[] { 0x01, 0x02, 0x0A, 0x03, 0x0A };
        var protocol = new TestMagicProtocol(0x0A);
        var scanner = new AcornFrame.FrameScanner<TestMagicProtocol>(data, protocol);

        Assert.True(scanner.TryReadNext(out var frame1));
        Assert.Equal(3, frame1.Size);
        Assert.Equal(2, frame1.Payload.Length);

        Assert.True(scanner.TryReadNext(out var frame2));
        Assert.Equal(2, frame2.Size);
        Assert.Equal(1, frame2.Payload.Length);
    }

    [Fact]
    public void TryPeekFrameSize_DoesNotConsumeData()
    {
        var writer = new AcornFrame.ByteBufferWriter(8);
        writer.WriteI32LE(4);
        writer.Write([0x01, 0x02, 0x03, 0x04]);

        var data = writer.WrittenData;
        var scanner = new AcornFrame.FrameScanner<TestLengthPrefixProtocol>(data);

        Assert.True(scanner.TryPeekFrameSize(out var size));
        Assert.Equal(8, size);
        Assert.Equal(0, scanner.Position);

        Assert.True(scanner.TryReadNext(out var frame));
        Assert.Equal(8, frame.Size);
        Assert.Equal(8, scanner.Position);
    }
}

public class FrameStructTests
{
    [Fact]
    public void Frame_WithRaw_Properties()
    {
        var raw = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        var payload = new ReadOnlySpan<byte>(raw, 2, 3);
        var frame = new AcornFrame.Frame(5, payload, raw);

        Assert.Equal(5, frame.Size);
        Assert.Equal(3, frame.Payload.Length);
        Assert.Equal(5, frame.Raw.Length);
    }

    [Fact]
    public void Frame_WithoutRaw_Properties()
    {
        var payload = new byte[] { 0x01, 0x02, 0x03 };
        var frame = new AcornFrame.Frame(3, payload);

        Assert.Equal(3, frame.Size);
        Assert.Equal(3, frame.Payload.Length);
        Assert.True(frame.Raw.IsEmpty);
    }
}

public class AlgebraicFrameTests
{
    private enum TestFrameKind : byte
    {
        data = 0x01,
        control = 0x02
    }

    private struct TestFrameKindWrapper : AcornFrame.IFrameKind
    {
        public TestFrameKind value { get; }
        public TestFrameKindWrapper(TestFrameKind value) => this.value = value;
    }

    [Fact]
    public void AlgebraicFrame_HoldsKindAndFrame()
    {
        var payload = new byte[] { 0x01, 0x02 };
        var frame = new AcornFrame.Frame(2, payload);
        var kind = new TestFrameKindWrapper(TestFrameKind.data);
        var algebraic = new AcornFrame.AlgebraicFrame<TestFrameKindWrapper, TestLengthPrefixProtocol>(kind, frame);

        Assert.Equal(TestFrameKind.data, algebraic.Kind.value);
        Assert.Equal(2, algebraic.Frame.Payload.Length);
    }
}
