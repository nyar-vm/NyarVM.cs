using Commander.Testing.Commands;

namespace Commander.Testing;

/// <summary>
///     CalcCommand 的环境变量覆盖测试。
/// </summary>
public class CalcCommandEnvTests
{
    /// <summary>
    ///     测试环境变量覆盖默认值。
    /// </summary>
    [Fact]
    public void Parse_EnvVarOverridesDefault()
    {
        Environment.SetEnvironmentVariable("CALC_PRECISION", "4");
        try
        {
            var cmd = CalcCommand.Parse([]);

            Assert.Equal(4, cmd.precision);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CALC_PRECISION", null);
        }
    }

    /// <summary>
    ///     测试命令行优先级高于环境变量。
    /// </summary>
    [Fact]
    public void Parse_CommandLineOverridesEnvVar()
    {
        Environment.SetEnvironmentVariable("CALC_PRECISION", "4");
        try
        {
            var cmd = CalcCommand.Parse(["--precision", "8"]);

            Assert.Equal(8, cmd.precision);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CALC_PRECISION", null);
        }
    }

    /// <summary>
    ///     测试无环境变量时使用默认值。
    /// </summary>
    [Fact]
    public void Parse_NoEnvVar_UsesDefault()
    {
        Environment.SetEnvironmentVariable("CALC_PRECISION", null);
        var cmd = CalcCommand.Parse([]);

        Assert.Equal(2, cmd.precision);
    }
}