namespace Nyar.Tests.Binary;

public sealed class VrmRoundTripTests
{
    #region VRM Õ˘∑µ≤‚ ‘

    [Fact]
    public void EncodeDecode_Vrm0_Roundtrip()
    {
        var original = new VrmModelData
        {
            Version = VrmVersion.Vrm0,
            Meta = new VrmMeta { Name = "TestModel", Author = "TestAuthor" }
        };

        var encoder = new VrmEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new VrmDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(VrmVersion.Vrm0, result.Version);
    }

    [Fact]
    public void EncodeDecode_Minimal_Roundtrip()
    {
        var original = new VrmModelData();

        var encoder = new VrmEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new VrmDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(VrmVersion.Vrm0, result.Version);
    }

    [Fact]
    public void DetectVersion_ValidGlTF_ReturnsVrm0()
    {
        var decoder = new VrmDecoder("glTF"u8);

        var version = decoder.DetectVrmVersion();

        Assert.Equal(VrmVersion.Vrm0, version);
    }

    [Fact]
    public void DetectVersion_InvalidData_ReturnsUnknown()
    {
        var decoder = new VrmDecoder("INVALID"u8);

        var version = decoder.DetectVrmVersion();

        Assert.Equal(VrmVersion.Unknown, version);
    }

    #endregion
}
