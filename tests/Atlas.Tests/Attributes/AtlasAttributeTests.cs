using Std.App.Server.Attributes;
using Xunit;

namespace Atlas.Tests.Attributes;

/// <summary>
///     Atlas 自有特性标签测试
/// </summary>
public sealed class AtlasAttributeTests
{
    [Fact]
    public void RoutePrefixAttribute_StoresPrefix()
    {
        var attr = new RoutePrefixAttribute("/api/users");

        Assert.Equal("/api/users", attr.prefix);
    }

    [Fact]
    public void AuthorizeAttribute_HasRolesAndScheme()
    {
        var attr = new AuthorizeAttribute
        {
            roles = ["admin", "user"],
            scheme = "Bearer"
        };

        Assert.Equal(2, attr.roles.Length);
        Assert.Equal("admin", attr.roles[0]);
        Assert.Equal("user", attr.roles[1]);
        Assert.Equal("Bearer", attr.scheme);
    }
}