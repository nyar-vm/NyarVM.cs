namespace Nyar.Tests.Binary;

public sealed class FlacRoundTripTests
{
    #region »ù±¾Íù·µ²âÊÔ

    [Fact]
    public void EncodeDecode_StereoCD_Roundtrip()
    {
        var original = new FlacAudioData
        {
            Channels = 2,
            SampleRate = 44100,
            BitsPerSample = 16,
            TotalSamples = 44100 * 10,
            MinBlockSize = 4096,
            MaxBlockSize = 4096,
            MD5Checksum = new byte[16]
        };

        var encoder = new FlacEncoder();
        var bytes = encoder.Encode(original);

        Assert.Equal(42, bytes.Length);

        var decoder = new FlacDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(2, result.Channels);
        Assert.Equal(44100, result.SampleRate);
        Assert.Equal(16, result.BitsPerSample);
        Assert.Equal(44100 * 10, result.TotalSamples);
        Assert.Equal(4096, result.MinBlockSize);
        Assert.Equal(4096, result.MaxBlockSize);
    }

    [Fact]
    public void EncodeDecode_Mono_Roundtrip()
    {
        var original = new FlacAudioData
        {
            Channels = 1,
            SampleRate = 16000,
            BitsPerSample = 8,
            TotalSamples = 16000,
            MinBlockSize = 512,
            MaxBlockSize = 512,
            MD5Checksum = new byte[16]
        };

        var encoder = new FlacEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new FlacDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(1, result.Channels);
        Assert.Equal(16000, result.SampleRate);
        Assert.Equal(8, result.BitsPerSample);
    }

    [Fact]
    public void EncodeDecode_HighRes_Roundtrip()
    {
        var original = new FlacAudioData
        {
            Channels = 2,
            SampleRate = 96000,
            BitsPerSample = 24,
            TotalSamples = 96000 * 30,
            MinBlockSize = 8192,
            MaxBlockSize = 8192,
            MD5Checksum = new byte[16]
        };

        var encoder = new FlacEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new FlacDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(96000, result.SampleRate);
        Assert.Equal(24, result.BitsPerSample);
    }

    [Fact]
    public void EncodeDecode_Multichannel_Roundtrip()
    {
        var original = new FlacAudioData
        {
            Channels = 6,
            SampleRate = 48000,
            BitsPerSample = 16,
            TotalSamples = 48000 * 5,
            MinBlockSize = 2048,
            MaxBlockSize = 2048,
            MD5Checksum = new byte[16]
        };

        var encoder = new FlacEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new FlacDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(6, result.Channels);
        Assert.Equal(48000, result.SampleRate);
    }

    [Fact]
    public void EncodeDecode_WithMD5_Roundtrip()
    {
        var md5 = new byte[]
            { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10 };

        var original = new FlacAudioData
        {
            Channels = 2,
            SampleRate = 44100,
            BitsPerSample = 16,
            TotalSamples = 44100,
            MinBlockSize = 4096,
            MaxBlockSize = 4096,
            MD5Checksum = md5
        };

        var encoder = new FlacEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new FlacDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(md5, result.MD5Checksum);
    }

    #endregion
}
