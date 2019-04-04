using Std.Database.Core;

namespace LightDB.Tests.Core;

public sealed class DatabaseKeyPatternsTests
{
    [Fact]
    public void User_ShouldCreateCorrectKey()
    {
        var key = DatabaseKeyPatterns.User("u1");

        Assert.Equal("user:u1", key.ToString());
    }

    [Fact]
    public void UserProfile_ShouldCreateCorrectKey()
    {
        var key = DatabaseKeyPatterns.UserProfile("u1");

        Assert.Equal("user:u1:profile", key.ToString());
    }

    [Fact]
    public void UserSettings_ShouldCreateCorrectKey()
    {
        var key = DatabaseKeyPatterns.UserSettings("u1");

        Assert.Equal("user:u1:settings", key.ToString());
    }

    [Fact]
    public void Order_ShouldCreateCorrectKey()
    {
        var key = DatabaseKeyPatterns.Order("o1");

        Assert.Equal("order:o1", key.ToString());
    }

    [Fact]
    public void UserOrder_ShouldCreateCorrectKey()
    {
        var key = DatabaseKeyPatterns.UserOrder("u1", "o1");

        Assert.Equal("order:u1:o1", key.ToString());
    }

    [Fact]
    public void Session_ShouldCreateCorrectKey()
    {
        var key = DatabaseKeyPatterns.Session("s1");

        Assert.Equal("session:s1", key.ToString());
    }

    [Fact]
    public void Config_ShouldCreateCorrectKey()
    {
        var key = DatabaseKeyPatterns.Config("theme");

        Assert.Equal("config:theme", key.ToString());
    }

    [Fact]
    public void Counter_ShouldCreateCorrectKey()
    {
        var key = DatabaseKeyPatterns.Counter("visits");

        Assert.Equal("counter:visits", key.ToString());
    }

    [Fact]
    public void Lock_ShouldCreateCorrectKey()
    {
        var key = DatabaseKeyPatterns.Lock("resource1");

        Assert.Equal("lock:resource1", key.ToString());
    }

    [Fact]
    public void Collection_ShouldCreateCorrectKey()
    {
        var key = DatabaseKeyPatterns.Collection("users");

        Assert.Equal("collection:users", key.ToString());
    }

    [Fact]
    public void CollectionDoc_ShouldCreateCorrectKey()
    {
        var key = DatabaseKeyPatterns.CollectionDoc("users", "doc1");

        Assert.Equal("collection:users:doc1", key.ToString());
    }

    [Fact]
    public void Index_ShouldCreateCorrectKey()
    {
        var key = DatabaseKeyPatterns.Index("idx_name");

        Assert.Equal("index:idx_name", key.ToString());
    }

    [Fact]
    public void Meta_ShouldCreateCorrectKey()
    {
        var key = DatabaseKeyPatterns.Meta("version");

        Assert.Equal("meta:version", key.ToString());
    }

    [Fact]
    public void Sequence_ShouldCreateCorrectKey()
    {
        var key = DatabaseKeyPatterns.Sequence("auto_inc");

        Assert.Equal("seq:auto_inc", key.ToString());
    }

    [Fact]
    public void UserKey_ShouldStartWithUserPrefix()
    {
        var key = DatabaseKeyPatterns.User("u1");

        Assert.True(key.starts_with(DatabaseKey.from_string("user:")));
    }
}