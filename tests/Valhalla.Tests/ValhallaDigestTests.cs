namespace Valhalla.Tests;

public class ValhallaDigestTests
{
    [Fact]
    public void 从字节数组计算_应产生固定长度hex()
    {
        var data = "hello world"u8.ToArray();
        var digest = ValhallaDigest.compute(data);
        Assert.Equal(64, digest.hex_string.Length);
    }

    [Fact]
    public void 从hex创建_恢复原始值()
    {
        var data = new byte[32];
        new Random(42).NextBytes(data);
        var hex = Convert.ToHexString(data).ToLowerInvariant();

        var digest = new ValhallaDigest(hex);
        Assert.Equal(hex, digest.hex_string);
    }

    [Fact]
    public void 从hex创建_非法长度_抛出异常()
    {
        Assert.Throws<ArgumentException>(() => new ValhallaDigest("abc"));
    }

    [Fact]
    public void 从hex创建_空值_抛出异常()
    {
        Assert.Throws<ArgumentException>(() => new ValhallaDigest(""));
    }

    [Fact]
    public void 相同内容_产生相同摘要()
    {
        var data1 = "test"u8.ToArray();
        var data2 = "test"u8.ToArray();

        var d1 = ValhallaDigest.compute(data1);
        var d2 = ValhallaDigest.compute(data2);

        Assert.Equal(d1, d2);
    }

    [Fact]
    public void 不同内容_产生不同摘要()
    {
        var d1 = ValhallaDigest.compute([.. "test1"u8]);
        var d2 = ValhallaDigest.compute([.. "test2"u8]);

        Assert.NotEqual(d1, d2);
    }

    [Fact]
    public async Task 从流计算_结果与字节计算方法一致()
    {
        var data = new byte[1024];
        new Random(42).NextBytes(data);

        using var ms = new MemoryStream(data);
        var streamDigest = await ValhallaDigest.compute(ms);
        var bytesDigest = ValhallaDigest.compute(data);

        Assert.Equal(bytesDigest, streamDigest);
    }
}