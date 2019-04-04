using Legion.CLI.Commands;

namespace Nyar.Tests.Command;

public sealed class LegionHelperCachingTests : IDisposable
{
    private readonly string _root_dir;

    public LegionHelperCachingTests()
    {
        _root_dir = Path.Combine(Path.GetTempPath(), $"legion-helper-cache-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root_dir);
    }

    [Fact]
    public void GetCachedLegionCompiler_SameProject_ShouldReuseInstance()
    {
        var projectDir = create_standalone_project("app");

        var first = LegionHelper.get_cached_legion_compiler(projectDir);
        var second = LegionHelper.get_cached_legion_compiler(projectDir);

        Assert.Same(first, second);
    }

    [Fact]
    public void GetCachedLegionCompiler_SameWorkspaceMembers_ShouldReuseWorkspaceCompiler()
    {
        var workspaceDir = create_workspace(["apps/a", "apps/b"]);
        var projectA = Path.Combine(workspaceDir, "apps", "a");
        var projectB = Path.Combine(workspaceDir, "apps", "b");

        var first = LegionHelper.get_cached_legion_compiler(projectA);
        var second = LegionHelper.get_cached_legion_compiler(projectB);

        Assert.Same(first, second);
    }

    [Fact]
    public void ResolveExternalTestArtifactPath_WhenSingleRunnableArtifact_ShouldReturnIt()
    {
        var artifactPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["suite.jar"] = @"E:\temp\suite.jar",
            ["suite.run.ps1"] = @"E:\temp\suite.run.ps1"
        };

        var artifactPath = LegionHelper.test_resolve_external_test_artifact_path("jvm", artifactPaths, "tests.alpha");

        Assert.Equal(@"E:\temp\suite.jar", artifactPath);
    }

    [Fact]
    public void ResolveExternalTestArtifactPath_WhenMultipleRunnableArtifacts_ShouldMatchSanitizedShortName()
    {
        var artifactPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["alpha.exe"] = @"E:\temp\alpha.exe",
            ["beta_case.exe"] = @"E:\temp\beta_case.exe",
            ["beta_case.runtimeconfig.json"] = @"E:\temp\beta_case.runtimeconfig.json"
        };

        var artifactPath = LegionHelper.test_resolve_external_test_artifact_path("clr", artifactPaths, "tests.beta-case");

        Assert.Equal(@"E:\temp\beta_case.exe", artifactPath);
    }

    [Fact]
    public void DescribeRunnableArtifacts_ShouldOnlyListRunnableFiles()
    {
        var artifactPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["alpha.jar"] = @"E:\temp\alpha.jar",
            ["alpha.class"] = @"E:\temp\alpha.class",
            ["beta.jar"] = @"E:\temp\beta.jar"
        };

        var description = LegionHelper.test_describe_runnable_artifacts("jvm", artifactPaths);

        Assert.Equal("可执行产物：alpha.jar, beta.jar", description);
    }

    [Fact]
    public void InferCoverageFeatures_ShouldMergeFeaturesAcrossProjectFiles()
    {
        var projectDir = create_project_with_sources(
            "coverage-merge",
            ("source/main.v", "micro add(a: i32, b: i32): i32 = a + b\nusing helper"),
            ("test/main_test.v", "[test]\nmicro test_add(): void {}\nmatch value {}"));

        var features = LegionHelper.infer_coverage_features(projectDir);

        Assert.Contains("micro", features);
        Assert.Contains("multi-file", features);
        Assert.Contains("test", features);
        Assert.Contains("match", features);
    }

    [Fact]
    public void InferCoverageFeatures_WhenFileChanges_ShouldRefreshCachedFeatures()
    {
        var projectDir = create_project_with_sources(
            "coverage-refresh",
            ("source/main.v", "micro add(a: i32, b: i32): i32 = a + b"));
        var sourceFile = Path.Combine(projectDir, "source", "main.v");

        var first = LegionHelper.infer_coverage_features(projectDir);

        File.WriteAllText(sourceFile, "mezzo compute(a: f64): f64 = if a > 0 { a } else { 0 }");

        var second = LegionHelper.infer_coverage_features(projectDir);

        Assert.Contains("micro", first);
        Assert.DoesNotContain("mezzo", first);
        Assert.DoesNotContain("float", first);

        Assert.Contains("mezzo", second);
        Assert.Contains("float", second);
        Assert.Contains("if-expr", second);
        Assert.DoesNotContain("micro", second);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root_dir))
        {
            Directory.Delete(_root_dir, true);
        }
    }

    private string create_standalone_project(string name)
    {
        var projectDir = Path.Combine(_root_dir, name);
        Directory.CreateDirectory(projectDir);
        File.WriteAllText(Path.Combine(projectDir, "legion.von"), "name: app");
        return projectDir;
    }

    private string create_project_with_sources(string name, params (string relativePath, string content)[] files)
    {
        var projectDir = create_standalone_project(name);
        foreach (var (relativePath, content) in files)
        {
            var fullPath = Path.Combine(projectDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content);
        }

        return projectDir;
    }

    private string create_workspace(IReadOnlyList<string> members)
    {
        var workspaceDir = Path.Combine(_root_dir, "workspace");
        Directory.CreateDirectory(workspaceDir);
        foreach (var member in members)
        {
            var memberDir = Path.Combine(workspaceDir, member.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(memberDir);
            File.WriteAllText(Path.Combine(memberDir, "legion.von"), "name: member");
        }

        var serializedMembers = string.Join(", ", members.Select(member => $"\"{member}\""));
        File.WriteAllText(Path.Combine(workspaceDir, "legions.von"), $"members: [{serializedMembers}]");
        return workspaceDir;
    }
}


