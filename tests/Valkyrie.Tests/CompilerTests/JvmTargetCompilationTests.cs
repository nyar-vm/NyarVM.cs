using Nyar.Language.Valkyrie.Compiler;
using Nyar.Language.Valkyrie.Compiler.Pipeline;

namespace Valkyrie.Tests.CompilerTests;

public sealed class JvmTargetCompilationTests
{
    [Fact]
    public void CompileToTarget_Jvm_ShouldProduceClassAndJarArtifacts()
    {
        var compiler = new ValkyrieCompiler();
        var plan = create_jvm_build_plan("hello_jvm", "main.v");
        var source = """
                     [main]
                     micro hello_jvm() -> Unit {
                         print("Hello JVM!")
                     }
                     """;

        var artifacts = compiler.compile_to_target(source, plan);

        Assert.Equal("hello_jvm.class", artifacts.primary_artifact.name);
        Assert.Equal("application/java-vm", artifacts.primary_artifact.media_type);
        Assert.Contains(artifacts.sidecar_artifacts, artifact => artifact.name == "hello_jvm.jar");
        Assert.Contains(artifacts.sidecar_artifacts, artifact => artifact.name == "hello_jvm.run.ps1");
        Assert.NotNull(artifacts.run_contract);
        Assert.Equal("hello_jvm", artifacts.run_contract!.logical_entry);
    }

    [Fact]
    public void CompileFilesToTarget_Jvm_WithNamespaces_ShouldProduceArtifacts()
    {
        var compiler = new ValkyrieCompiler();
        var plan = create_jvm_build_plan("multi_file_app", "main.v");
        var tempDirectory = create_temp_directory();

        try
        {
            var mathPath = Path.Combine(tempDirectory, "math.v");
            var mainPath = Path.Combine(tempDirectory, "main.v");

            File.WriteAllText(mathPath,
                """
                namespace app.math;

                micro add_one(x: i32) {
                    return x + 1
                }
                """);
            File.WriteAllText(mainPath,
                """
                namespace app;

                [main]
                micro main() -> Unit {
                    app.math.add_one(41)
                    print("Hello Namespace JVM!")
                }
                """);

            var artifacts = compiler.compile_files_to_target([mathPath, mainPath], plan);

            Assert.Equal("multi_file_app.class", artifacts.primary_artifact.name);
            Assert.Contains(artifacts.sidecar_artifacts, artifact => artifact.name == "multi_file_app.jar");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }

    [Fact]
    public void CompileToTarget_Jvm_WithTraitImplyAndDefaultMethod_ShouldProduceArtifacts()
    {
        var compiler = new ValkyrieCompiler();
        var plan = create_jvm_build_plan("trait_jvm", "trait.v");
        var source = """
                     namespace app;

                     trait Map {
                         micro set(mut self, value: i32) -> Unit

                         micro insert(mut self, value: i32) -> Unit {
                             self.set(value)
                         }
                     }

                     structure Buffer {
                     }

                     imply Buffer: Map {
                         micro set(mut self, value: i32) -> Unit {
                         }
                     }

                     [main]
                     micro main() -> Unit {
                         print("trait on jvm")
                     }
                     """;

        var artifacts = compiler.compile_to_target(source, plan);

        Assert.Equal("trait_jvm.class", artifacts.primary_artifact.name);
        Assert.Contains(artifacts.sidecar_artifacts, artifact => artifact.name == "trait_jvm.jar");
    }

    private static BuildPlan create_jvm_build_plan(string moduleName, string filePath)
    {
        return new BuildPlan(moduleName, "jvm-openjdk-linux-managed", filePath);
    }

    private static string create_temp_directory()
    {
        var path = Path.Combine(Path.GetTempPath(), "valkyrie-jvm-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
