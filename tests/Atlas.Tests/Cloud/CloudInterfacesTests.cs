using Xunit;

namespace Atlas.Tests.Cloud;

/// <summary>
///     云存储接口类型测试
/// </summary>
public sealed class CloudInterfacesTests
{
    [Fact]
    public void BlobResult_ok_ReturnsSuccess()
    {
        var result = BlobResult.ok("etag-123");

        Assert.True(result.success);
        Assert.Equal("etag-123", result.etag);
        Assert.Null(result.error);
    }

    [Fact]
    public void BlobResult_fail_ReturnsError()
    {
        var result = BlobResult.fail("桶不存在");

        Assert.False(result.success);
        Assert.Null(result.etag);
        Assert.Equal("桶不存在", result.error);
    }

    [Fact]
    public void SmsResult_ok_ReturnsSuccess()
    {
        var result = SmsResult.ok("req-abc");

        Assert.True(result.success);
        Assert.Equal("req-abc", result.request_id);
        Assert.Null(result.error);
    }

    [Fact]
    public void SmsResult_fail_ReturnsError()
    {
        var result = SmsResult.fail("模板不存在");

        Assert.False(result.success);
        Assert.Null(result.request_id);
        Assert.Equal("模板不存在", result.error);
    }

    [Fact]
    public void BlobInfo_HasRequiredProperties()
    {
        var info = new BlobInfo
        {
            key = "folder/file.txt",
            size = 1024,
            last_modified = new DateTime(2025, 1, 1),
            etag = "abc123"
        };

        Assert.Equal("folder/file.txt", info.key);
        Assert.Equal(1024, info.size);
        Assert.Equal(new DateTime(2025, 1, 1), info.last_modified);
        Assert.Equal("abc123", info.etag);
    }
}