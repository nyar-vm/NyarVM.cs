using System.Security.Cryptography;
using System.Text;

namespace Valhalla.Tests;

public class Ed25519AuthMiddlewareTests
{
    /// <summary>
    ///     创建 Ed25519 测试密钥对并返回（原始公钥字节, 密钥对象, 算法实例, 指纹）
    /// </summary>
    private static (byte[] RawPublicKey, Key Key, SignatureAlgorithm Algorithm, string Fingerprint)
        create_test_key_pair()
    {
        var algorithm = SignatureAlgorithm.Ed25519;
        var key = Key.Create(algorithm, new KeyCreationParameters
        {
            ExportPolicy = KeyExportPolicies.AllowPlaintextExport
        });
        byte[] rawPublicKey = key.Export(KeyBlobFormat.RawPublicKey);
        var fingerprint = Convert.ToHexString(rawPublicKey).ToLowerInvariant();
        return (rawPublicKey, key, algorithm, fingerprint);
    }

    /// <summary>
    ///     对请求体进行 Ed25519 签名，返回 Base64 编码的签名
    /// </summary>
    private static string sign_request_body(Key key, SignatureAlgorithm algorithm, byte[] bodyBytes)
    {
        var bodyHash = SHA256.HashData(bodyBytes);
        byte[] signature = algorithm.Sign(key, bodyHash);
        return Convert.ToBase64String(signature);
    }

    /// <summary>
    ///     创建包含请求体和 Authorization 头的 HttpContext
    /// </summary>
    private static DefaultHttpContext create_context(
        string method, byte[] bodyBytes, string fingerprint, string signatureBase64)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Body = new MemoryStream(bodyBytes);
        context.Request.Headers["Authorization"] =
            $"ed25519-signature({fingerprint}, {signatureBase64})";
        return context;
    }

    [Fact]
    public async Task 有效签名_应放行并设置指纹()
    {
        var (rawPublicKey, key, algorithm, fingerprint) = create_test_key_pair();
        var bodyBytes = Encoding.UTF8.GetBytes(@"{""name"":""test.pkg"",""version"":""1.0.0""}");
        var signatureBase64 = sign_request_body(key, algorithm, bodyBytes);

        var trustedKeys = new Dictionary<string, byte[]>
        {
            [fingerprint] = rawPublicKey
        };

        var nextCalled = false;
        var middleware = new Ed25519AuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, trustedKeys);

        var context = create_context("POST", bodyBytes, fingerprint, signatureBase64);

        await middleware.invoke(context);

        Assert.True(nextCalled, "有效签名应放行请求");
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(fingerprint, context.Items["ValhallaActorFingerprint"] as string);
    }

    [Fact]
    public async Task 无效签名_应返回401()
    {
        var (rawPublicKey, key, algorithm, fingerprint) = create_test_key_pair();
        var bodyBytes = Encoding.UTF8.GetBytes(@"{""name"":""test.pkg""}");
        var signatureBase64 = sign_request_body(key, algorithm, bodyBytes);

        var tamperedBody = Encoding.UTF8.GetBytes(@"{""name"":""evil.pkg""}");

        var trustedKeys = new Dictionary<string, byte[]>
        {
            [fingerprint] = rawPublicKey
        };

        var nextCalled = false;
        var middleware = new Ed25519AuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, trustedKeys);

        var context = create_context("POST", tamperedBody, fingerprint, signatureBase64);

        await middleware.invoke(context);

        Assert.False(nextCalled, "无效签名不应放行请求");
        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task 未知指纹_应返回401()
    {
        var (rawPublicKey, key, algorithm, fingerprint) = create_test_key_pair();
        var bodyBytes = Encoding.UTF8.GetBytes(@"{""name"":""test.pkg""}");
        var signatureBase64 = sign_request_body(key, algorithm, bodyBytes);

        var trustedKeys = new Dictionary<string, byte[]>
        {
            ["known-fingerprint"] = rawPublicKey
        };

        var nextCalled = false;
        var middleware = new Ed25519AuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, trustedKeys);

        var context = create_context("POST", bodyBytes, fingerprint, signatureBase64);

        await middleware.invoke(context);

        Assert.False(nextCalled, "未知指纹不应放行请求");
        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task 错误的签名_应返回401()
    {
        var (rawPublicKey, _, _, fingerprint) = create_test_key_pair();
        var bodyBytes = Encoding.UTF8.GetBytes(@"{""name"":""test.pkg""}");

        var fakeSignature = Convert.ToBase64String(new byte[64]);

        var trustedKeys = new Dictionary<string, byte[]>
        {
            [fingerprint] = rawPublicKey
        };

        var nextCalled = false;
        var middleware = new Ed25519AuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, trustedKeys);

        var context = create_context("POST", bodyBytes, fingerprint, fakeSignature);

        await middleware.invoke(context);

        Assert.False(nextCalled, "伪造签名不应放行请求");
        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task GET请求_应跳过认证()
    {
        var (rawPublicKey, _, _, fingerprint) = create_test_key_pair();

        var trustedKeys = new Dictionary<string, byte[]>
        {
            [fingerprint] = rawPublicKey
        };

        var nextCalled = false;
        var middleware = new Ed25519AuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, trustedKeys);

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Body = new MemoryStream();

        await middleware.invoke(context);

        Assert.True(nextCalled, "GET 请求应跳过认证直接放行");
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task HEAD请求_应跳过认证()
    {
        var (rawPublicKey, _, _, fingerprint) = create_test_key_pair();

        var trustedKeys = new Dictionary<string, byte[]>
        {
            [fingerprint] = rawPublicKey
        };

        var nextCalled = false;
        var middleware = new Ed25519AuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, trustedKeys);

        var context = new DefaultHttpContext();
        context.Request.Method = "HEAD";
        context.Request.Body = new MemoryStream();

        await middleware.invoke(context);

        Assert.True(nextCalled, "HEAD 请求应跳过认证直接放行");
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task 缺少Authorization头_应返回401()
    {
        var (rawPublicKey, _, _, fingerprint) = create_test_key_pair();

        var trustedKeys = new Dictionary<string, byte[]>
        {
            [fingerprint] = rawPublicKey
        };

        var nextCalled = false;
        var middleware = new Ed25519AuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, trustedKeys);

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(@"{""name"":""x""}"));

        await middleware.invoke(context);

        Assert.False(nextCalled, "缺少 Authorization 头应返回 401");
        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task 无效Base64签名_应返回401()
    {
        var (rawPublicKey, _, _, fingerprint) = create_test_key_pair();
        var bodyBytes = Encoding.UTF8.GetBytes(@"{""name"":""test.pkg""}");

        var trustedKeys = new Dictionary<string, byte[]>
        {
            [fingerprint] = rawPublicKey
        };

        var nextCalled = false;
        var middleware = new Ed25519AuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, trustedKeys);

        var context = create_context("POST", bodyBytes, fingerprint, "!!!这不是Base64!!!");

        await middleware.invoke(context);

        Assert.False(nextCalled, "无效 Base64 签名应返回 401");
        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task 格式错误的认证头_应返回401()
    {
        var (rawPublicKey, _, _, fingerprint) = create_test_key_pair();
        var bodyBytes = Encoding.UTF8.GetBytes(@"{""name"":""test.pkg""}");

        var trustedKeys = new Dictionary<string, byte[]>
        {
            [fingerprint] = rawPublicKey
        };

        var nextCalled = false;
        var middleware = new Ed25519AuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, trustedKeys);

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Body = new MemoryStream(bodyBytes);
        context.Request.Headers["Authorization"] = "ed25519-signature(incomplete";

        await middleware.invoke(context);

        Assert.False(nextCalled, "格式错误的认证头应返回 401");
        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task 空请求体_有效签名_应放行()
    {
        var (rawPublicKey, key, algorithm, fingerprint) = create_test_key_pair();
        var bodyBytes = Array.Empty<byte>();
        var signatureBase64 = sign_request_body(key, algorithm, bodyBytes);

        var trustedKeys = new Dictionary<string, byte[]>
        {
            [fingerprint] = rawPublicKey
        };

        var nextCalled = false;
        var middleware = new Ed25519AuthMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, trustedKeys);

        var context = create_context("PUT", bodyBytes, fingerprint, signatureBase64);

        await middleware.invoke(context);

        Assert.True(nextCalled, "空请求体有效签名应放行");
        Assert.Equal(200, context.Response.StatusCode);
    }
}