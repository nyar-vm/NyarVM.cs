namespace Valhalla.Tests;

public class ValhallaConfigTests
{
    [Fact]
    public void CreateDefault_产生有效默认配置()
    {
        var config = ValhallaConfig.create_default();
        Assert.Equal("瓦尓哈拉", config.name);
        Assert.Equal(8080, config.port);
        Assert.Equal(RegistrationMode.open, config.registration);
        Assert.Equal(StorageBackend.local, config.storage);
        Assert.True(config.@public);
    }

    [Fact]
    public void Validate_默认配置_有效()
    {
        var config = ValhallaConfig.create_default();
        var errors = config.validate();
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_名称为空_报错()
    {
        var config = ValhallaConfig.create_default();
        config.name = "  ";
        var errors = config.validate();
        Assert.Contains("实例名称不能为空", errors);
    }

    [Fact]
    public void Validate_端口越界_报错()
    {
        var config = ValhallaConfig.create_default();
        config.port = 0;
        var errors = config.validate();
        Assert.Contains("端口号必须在 1-65535 范围内", errors);
    }

    [Fact]
    public void Validate_S3缺少配置_报错()
    {
        var config = ValhallaConfig.create_default();
        config.storage = StorageBackend.s3;
        var errors = config.validate();
        Assert.Contains("使用 S3 存储时必须配置 S3 信息", errors);
    }

    [Fact]
    public void 往返序列化_默认配置_一致()
    {
        var original = ValhallaConfig.create_default();
        original.name = "测试实例";
        original.description = "一个测试";
        original.cooling_period_days = 7;

        var von = original.to_von_string();
        var parsed = ValhallaConfig.parse(von);

        Assert.Equal(original.name, parsed.name);
        Assert.Equal(original.description, parsed.description);
        Assert.Equal(original.cooling_period_days, parsed.cooling_period_days);
        Assert.Equal(original.port, parsed.port);
        Assert.Equal(original.registration, parsed.registration);
    }

    [Fact]
    public void 往返序列化_S3配置_一致()
    {
        var original = new ValhallaConfig
        {
            storage = StorageBackend.s3,
            s3 = new S3Config
            {
                endpoint = "https://s3.example.com",
                bucket = "my-bucket",
                region = "us-west-2"
            }
        };

        var von = original.to_von_string();
        var parsed = ValhallaConfig.parse(von);

        Assert.Equal(StorageBackend.s3, parsed.storage);
        Assert.NotNull(parsed.s3);
        Assert.Equal("https://s3.example.com", parsed.s3!.endpoint);
        Assert.Equal("my-bucket", parsed.s3.bucket);
    }

    [Fact]
    public void 解析_邀请码模式_正确()
    {
        var von = @"{ 
    registration: ""invite"",
    inviteCode: ""let-me-in""
}";
        var config = ValhallaConfig.parse(von);
        Assert.Equal(RegistrationMode.invite, config.registration);
        Assert.Equal("let-me-in", config.invite_code);
    }

    [Fact]
    public void 解析_关闭注册_正确()
    {
        var von = @"{ registration: ""closed"" }";
        var config = ValhallaConfig.parse(von);
        Assert.Equal(RegistrationMode.closed, config.registration);
    }

    [Fact]
    public void 解析_非对象_抛出异常()
    {
        Assert.Throws<ArgumentException>(() => ValhallaConfig.parse("\"hello\""));
    }

    [Fact]
    public async Task SaveAsync_写入文件_可重新加载()
    {
        var config = ValhallaConfig.create_default();
        config.name = "写入测试";

        var tempFile = Path.Combine(Path.GetTempPath(),
            $"valhalla-test-{Guid.NewGuid():N}.von");

        try
        {
            await config.save(tempFile);
            Assert.True(File.Exists(tempFile));

            var loaded = await ValhallaConfig.load(tempFile);
            Assert.Equal(config.name, loaded.name);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}