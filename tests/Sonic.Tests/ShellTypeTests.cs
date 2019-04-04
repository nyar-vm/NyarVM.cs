namespace Commander.Testing;

/// <summary>
///     ShellType 枚举测试。
/// </summary>
public class ShellTypeTests
{
    /// <summary>
    ///     测试枚举值完整性。
    /// </summary>
    [Fact]
    public void AllValues_Defined()
    {
        Assert.True(Enum.IsDefined(typeof(ShellType), ShellType.bash));
        Assert.True(Enum.IsDefined(typeof(ShellType), ShellType.zsh));
        Assert.True(Enum.IsDefined(typeof(ShellType), ShellType.fish));
        Assert.True(Enum.IsDefined(typeof(ShellType), ShellType.power_shell));
    }
}