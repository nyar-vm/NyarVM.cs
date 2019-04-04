namespace Nyar.Tests.Binary.GnosisTests;

public class GnosisRoundTripTests
{
    [Fact]
    public void Encode_Decode_MinimalModule()
    {
        var original = new GnosisModuleData
        {
            Version = GnosisConstants.CurrentVersion
        };

        var encoder = new GnosisEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new GnosisDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.NotNull(decoded);
    }

    [Fact]
    public void Encode_Decode_ModuleWithName()
    {
        var original = new GnosisModuleData
        {
            Version = GnosisConstants.CurrentVersion,
            ModuleName = "TestModule"
        };

        var encoder = new GnosisEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new GnosisDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.NotNull(decoded);
        Assert.Equal("TestModule", decoded.ModuleName);
    }

    [Fact]
    public void Encode_Decode_ModuleWithConstants()
    {
        var original = new GnosisModuleData
        {
            Version = GnosisConstants.CurrentVersion,
            Constants =
            [
                GnosisConstant.String("hello"),
                GnosisConstant.Int(42),
                GnosisConstant.Float(3.14)
            ]
        };

        var encoder = new GnosisEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new GnosisDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.NotNull(decoded);
        Assert.Equal(3, decoded.Constants.Count);
        Assert.Equal(GnosisConstantTag.String, decoded.Constants[0].Tag);
        Assert.Equal("hello", decoded.Constants[0].Value);
        Assert.Equal(GnosisConstantTag.Int, decoded.Constants[1].Tag);
        Assert.Equal(42L, decoded.Constants[1].Value);
        Assert.Equal(GnosisConstantTag.Float, decoded.Constants[2].Tag);
        Assert.InRange(Convert.ToDouble(decoded.Constants[2].Value), 3.13, 3.15);
    }

    [Fact]
    public void Encode_Decode_ModuleWithImportsAndExports()
    {
        var original = new GnosisModuleData
        {
            Version = GnosisConstants.CurrentVersion,
            ImportedSymbols = ["math.add", "math.sub"],
            ExportedSymbols = ["main", "init"]
        };

        var encoder = new GnosisEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new GnosisDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.NotNull(decoded);
        Assert.Equal(2, decoded.ImportedSymbols.Count);
        Assert.Equal("math.add", decoded.ImportedSymbols[0]);
        Assert.Equal("math.sub", decoded.ImportedSymbols[1]);
        Assert.Equal(2, decoded.ExportedSymbols.Count);
        Assert.Equal("main", decoded.ExportedSymbols[0]);
        Assert.Equal("init", decoded.ExportedSymbols[1]);
    }

    [Fact]
    public void Encode_Decode_ModuleWithDependencies()
    {
        var original = new GnosisModuleData
        {
            Version = GnosisConstants.CurrentVersion,
            Dependencies = ["lib.std", "lib.math"]
        };

        var encoder = new GnosisEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new GnosisDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.NotNull(decoded);
        Assert.Equal(2, decoded.Dependencies.Count);
        Assert.Equal("lib.std", decoded.Dependencies[0]);
        Assert.Equal("lib.math", decoded.Dependencies[1]);
    }

    [Fact]
    public void Encode_Decode_ModuleWithInstructions()
    {
        var original = new GnosisModuleData
        {
            Version = GnosisConstants.CurrentVersion,
            Instructions = [0x10, 0x2A, 0x52, 0x00]
        };

        var encoder = new GnosisEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new GnosisDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.NotNull(decoded);
        Assert.Equal(original.Instructions, decoded.Instructions);
    }

    [Fact]
    public void Encode_Decode_EmptyModuleName_ShouldBeEmptyAfterDecode()
    {
        var original = new GnosisModuleData
        {
            Version = GnosisConstants.CurrentVersion
        };

        var encoder = new GnosisEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new GnosisDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.NotNull(decoded);
        Assert.Equal(string.Empty, decoded.ModuleName);
    }

    [Fact]
    public void Decode_InvalidMagic_Throws()
    {
        var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00 };

        Assert.ThrowsAny<Exception>(() =>
        {
            var decoder = new GnosisDecoder(data);
            decoder.Decode();
        });
    }

    [Fact]
    public void Decode_TruncatedData_Throws()
    {
        var data = new byte[3];

        Assert.ThrowsAny<Exception>(() =>
        {
            var decoder = new GnosisDecoder(data);
            decoder.Decode();
        });
    }
}
