namespace Nyar.Tests.Binary.SpirvTests;

public class SpirvRoundTripTests
{
    [Fact]
    public void Encode_Decode_MinimalModule()
    {
        var original = new SpirvModuleData();

        var encoder = new SpirvEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new SpirvDecoder(bytes);
        var decoded = decoder.DecodeAll();

        Assert.NotNull(decoded);
    }

    [Fact]
    public void Encode_Decode_ModuleWithNopInstructions()
    {
        var original = new SpirvModuleData
        {
            Version = SpirvConstants.Version13,
            Bound = 10,
            Instructions =
            [
                new() { Opcode = SpirvOpCode.OpNop, WordCount = 1 },
                new() { Opcode = SpirvOpCode.OpNop, WordCount = 1 },
                new() { Opcode = SpirvOpCode.OpNop, WordCount = 1 }
            ]
        };

        var encoder = new SpirvEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new SpirvDecoder(bytes);
        var decoded = decoder.DecodeAll();

        Assert.NotNull(decoded);
        Assert.Equal(3, decoded.Instructions.Count);
        Assert.All(decoded.Instructions, inst => Assert.Equal(SpirvOpCode.OpNop, inst.Opcode));
    }

    [Fact]
    public void Encode_Decode_ModuleWithCustomGeneratorMagic()
    {
        var original = new SpirvModuleData
        {
            GeneratorMagic = 0x12345678,
            Bound = 5
        };

        var encoder = new SpirvEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new SpirvDecoder(bytes);
        var decoded = decoder.DecodeAll();

        Assert.NotNull(decoded);
        Assert.Equal(original.GeneratorMagic, decoded.GeneratorMagic);
    }

    [Fact]
    public void Encode_Decode_ModuleWithVersion()
    {
        var original = new SpirvModuleData
        {
            Version = SpirvConstants.Version15,
            Bound = 3
        };

        var encoder = new SpirvEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new SpirvDecoder(bytes);
        var decoded = decoder.DecodeAll();

        Assert.NotNull(decoded);
        Assert.Equal(original.Version, decoded.Version);
    }

    [Fact]
    public void Encode_Decode_ModuleWithInstructionOperands()
    {
        var original = new SpirvModuleData
        {
            Bound = 20,
            Instructions =
            [
                new()
                {
                    Opcode = SpirvOpCode.OpTypeVoid,
                    WordCount = 2,
                    Operands = [1]
                },
                new()
                {
                    Opcode = SpirvOpCode.OpTypeInt,
                    WordCount = 4,
                    Operands = [2, 32, 1]
                }
            ]
        };

        var encoder = new SpirvEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new SpirvDecoder(bytes);
        var decoded = decoder.DecodeAll();

        Assert.NotNull(decoded);
        Assert.Equal(2, decoded.Instructions.Count);
        Assert.Equal(SpirvOpCode.OpTypeVoid, decoded.Instructions[0].Opcode);
        Assert.Equal((ushort)2, decoded.Instructions[0].WordCount);
        Assert.Equal(SpirvOpCode.OpTypeInt, decoded.Instructions[1].Opcode);
        Assert.Equal((ushort)4, decoded.Instructions[1].WordCount);
    }

    [Fact]
    public void Decode_InvalidMagic_Throws()
    {
        var data = new byte[24];

        Assert.ThrowsAny<Exception>(() =>
        {
            var decoder = new SpirvDecoder(data);
            decoder.DecodeAll();
        });
    }

    [Fact]
    public void Decode_TruncatedData_Throws()
    {
        var data = new byte[3];

        Assert.ThrowsAny<Exception>(() =>
        {
            var decoder = new SpirvDecoder(data);
            decoder.DecodeAll();
        });
    }
}
