namespace Nyar.Tests.Binary;

public sealed class OpusRoundTripTests
{
    #region »ù±¾Íù·µ²âÊÔ

    [Fact]
    public void EncodeDecode_Stereo_Roundtrip()
    {
        var original = new OpusAudioData
        {
            Channels = 2,
            SampleRate = 48000,
            PreSkip = 312,
            OutputGain = 0,
            ChannelMappingFamily = 0
        };

        var encoder = new OpusEncoder();
        var bytes = encoder.Encode(original);

        Assert.Equal(19, bytes.Length);

        var decoder = new OpusDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(2, result.Channels);
        Assert.Equal(48000u, result.SampleRate);
        Assert.Equal(312, result.PreSkip);
        Assert.Equal(0, result.OutputGain);
        Assert.Equal(0, result.ChannelMappingFamily);
    }

    [Fact]
    public void EncodeDecode_Mono_Roundtrip()
    {
        var original = new OpusAudioData
        {
            Channels = 1,
            SampleRate = 16000,
            PreSkip = 120,
            OutputGain = 0,
            ChannelMappingFamily = 0
        };

        var encoder = new OpusEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new OpusDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(1, result.Channels);
        Assert.Equal(16000u, result.SampleRate);
        Assert.Equal(120, result.PreSkip);
    }

    [Fact]
    public void EncodeDecode_HighSampleRate_Roundtrip()
    {
        var original = new OpusAudioData
        {
            Channels = 2,
            SampleRate = 96000,
            PreSkip = 384,
            OutputGain = 0,
            ChannelMappingFamily = 0
        };

        var encoder = new OpusEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new OpusDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(96000u, result.SampleRate);
        Assert.Equal(384, result.PreSkip);
    }

    [Fact]
    public void EncodeDecode_WithGain_Roundtrip()
    {
        var original = new OpusAudioData
        {
            Channels = 2,
            SampleRate = 48000,
            PreSkip = 312,
            OutputGain = 256,
            ChannelMappingFamily = 1
        };

        var encoder = new OpusEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new OpusDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(256, result.OutputGain);
        Assert.Equal(1, result.ChannelMappingFamily);
    }

    [Fact]
    public void EncodeDecode_Multichannel_Roundtrip()
    {
        var original = new OpusAudioData
        {
            Channels = 8,
            SampleRate = 48000,
            PreSkip = 312,
            OutputGain = 0,
            ChannelMappingFamily = 255
        };

        var encoder = new OpusEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new OpusDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(8, result.Channels);
        Assert.Equal(255, result.ChannelMappingFamily);
    }

    #endregion
}
