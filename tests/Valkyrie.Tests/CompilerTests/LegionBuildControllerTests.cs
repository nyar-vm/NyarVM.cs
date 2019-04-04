using Nyar.Language.Valkyrie.Compiler;
using Nyar.Language.Valkyrie.Compiler.Pipeline;

namespace Valkyrie.Tests.CompilerTests;

/// <summary>
///     多文件直接编译集成测试，验证 `ValkyrieCompiler` 的多文件主链路可通�?/// </summary>
public sealed class LegionBuildControllerTests
{
    [Fact]
    public void CompileFilesToTarget_WithNamespacedMultiFileProject_ShouldProduceArtifacts()
    {
        var tempDirectory = create_temp_directory();
        var sourceDirectory = Path.Combine(tempDirectory, "source");
        Directory.CreateDirectory(sourceDirectory);

        var mathPath = Path.Combine(sourceDirectory, "math.v");
        var mainPath = Path.Combine(sourceDirectory, "main.v");

        File.WriteAllText(mathPath,
            """
            namespace app.math;

            micro add_one(x: i32) -> i32 {
                return x + 1
            }
            """);
        File.WriteAllText(mainPath,
            """
            namespace app;

            [main]
            micro main() -> Unit {
                let answer = app.math.add_one(41)
                if answer == 42 {
                    print("Hello Namespace Build!")
                }
            }
            """);

        try
        {
            var compiler = new ValkyrieCompiler();
            var plan = new BuildPlan("multi_file_build", "jvm-openjdk-linux-managed", mainPath);

            var artifacts = compiler.compile_files_to_target([mathPath, mainPath], plan);

            Assert.NotNull(artifacts);
            Assert.Equal("multi_file_build.class", artifacts.primary_artifact.name);
            Assert.Contains(artifacts.sidecar_artifacts, artifact => artifact.name == "multi_file_build.jar");
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
        var path = Path.Combine(Path.GetTempPath(), "legion-build-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
