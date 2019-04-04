using Nyar.Language.Build;

namespace Valkyrie.Tests.CompilerTests;

public sealed class CompilationMatrixBuilderTests
{
    [Fact]
    public void BuildContexts_ExplicitClrAlias_ShouldUseCanonicalManifestOptions()
    {
        var projectDirectory = create_temp_directory();
        var outputDirectory = Path.Combine(projectDirectory, "dist");

        File.WriteAllText(Path.Combine(projectDirectory, "legion.von"),
            """
            {
                name: "matrix_builder_test",
                build: [
                    {
                        target: "clr-microsoft-unknown-managed",
                        msil: true
                    }
                ]
            }
            """);

        try
        {
            var builder = new CompilationMatrixBuilder();

            var contexts = builder.build_contexts(projectDirectory, "clr", outputDirectory, verbose: false);

            var context = Assert.Single(contexts);
            Assert.Equal("clr-microsoft-unknown-managed", context.canonical_triple);
            Assert.Equal(Path.Combine(outputDirectory, "clr-microsoft-unknown-managed"), context.output_dir);
            Assert.NotNull(context.build_options);
            Assert.True(context.build_options!.msil);
        }
        finally
        {
            if (Directory.Exists(projectDirectory))
            {
                Directory.Delete(projectDirectory, true);
            }
        }
    }

    private static string create_temp_directory()
    {
        var path = Path.Combine(Path.GetTempPath(), "compilation-matrix-builder-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
