using System;
using System.IO;
using Nyar.Language.Valkyrie.Compiler;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Xunit;

namespace Valkyrie.Tests.CompilerTests;

/// <summary>
///     验证静态方法必须使用 `::` 调用，不能误用实例调用语法。
/// </summary>
public sealed class StaticMethodSyntaxTests
{
    /// <summary>
    ///     构建主链路应在前端阶段拦截 `Type.method()` 形式的静态调用。
    /// </summary>
    [Fact]
    public void CompileToTarget_StaticFactoryCalledWithDot_ShouldFailSemanticCheck()
    {
        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("static_call_syntax", "jvm-openjdk-linux-managed", "static_call_syntax.v");

        var source = """
            namespace sample;

            class Box {
            }

            imply Box {
                micro new() -> Self {
                    return Self {}
                }
            }

            [main]
            micro main() -> Unit {
                let value = Box.new()
            }
            """;

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.compile_to_target(source, plan));

        Assert.Equal("语义分析失败，无法继续编译。", exception.Message);
        Assert.Contains(compiler.diagnostics.messages, diagnostic => diagnostic.message.Contains("Box::new", StringComparison.Ordinal));
    }

    /// <summary>
    ///     轻量级 `check` 前端也应拦截同样的错误。
    /// </summary>
    [Fact]
    public void CheckFiles_StaticFactoryCalledWithDot_ShouldFailSemanticCheck()
    {
        var tempDirectory = create_temp_directory();
        var sourcePath = Path.Combine(tempDirectory, "main.v");
        File.WriteAllText(sourcePath,
            """
            namespace sample;

            class Box {
            }

            imply Box {
                micro new() -> Self {
                    return Self {}
                }
            }

            [main]
            micro main() -> Unit {
                let value = Box.new()
            }
            """);

        try
        {
            var compiler = new ValkyrieCompiler();
            var plan = new BuildPlan("static_call_syntax", "jvm-openjdk-linux-managed", sourcePath);

            var exception = Assert.Throws<InvalidOperationException>(() => compiler.check_files([sourcePath], plan));

            Assert.Equal("语义分析失败，无法继续检查。", exception.Message);
            Assert.Contains(compiler.diagnostics.messages, diagnostic => diagnostic.message.Contains("Box::new", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }

    private static string create_temp_directory()
    {
        var path = Path.Combine(Path.GetTempPath(), "static-method-syntax-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
