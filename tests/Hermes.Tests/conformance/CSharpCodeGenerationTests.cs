using Hermes.Compiler;
using Hermes.Generator;
using Xunit;

namespace Hermes.Tests.Conformance;

public class CSharpCodeGenerationTests
{
    #region 命名空间

    [Fact]
    public void Generate_WithCustomNamespace_UsesProvidedNamespace()
    {
        var source = @"
namespace! MyApp

model Post {
    @@id: i64,
}";
        var result = CompileSource(source);

        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var context = new GeneratorContext
        {
            SchemaPath = "test.her",
            OutputPath = "/output",
            Schema = result.Schema!,
            Options = new Dictionary<string, object>
            {
                ["namespace"] = "MyApp.Data",
                ["target"] = "record"
            }
        };
        var generator = new CSharpGenerator();
        var genResult = generator.Generate(context);

        var interfaceFile = genResult.Files.FirstOrDefault(f => f.TypeName == "IMyAppRepository");
        Assert.NotNull(interfaceFile);
        Assert.Contains("namespace MyApp.Data;", interfaceFile!.Content);
    }

    #endregion

    #region Repository 接口生成

    [Fact]
    public void Generate_Model_ProducesRepositoryInterface()
    {
        var source = @"
model Post {
    @@id: i64,
    title: utf8,
    content: utf8,
}";
        var result = CompileSource(source);

        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");
        Assert.NotNull(result.Schema);

        var context = CreateContext(result);
        var generator = new CSharpGenerator();
        var genResult = generator.Generate(context);

        var interfaceFile = genResult.Files.FirstOrDefault(f => f.TypeName == "ISchemaRepository");
        Assert.NotNull(interfaceFile);
        Assert.Contains("public interface ISchemaRepository", interfaceFile!.Content);
        Assert.Contains("FindPostByIdAsync", interfaceFile.Content);
        Assert.Contains("GetAllPostsAsync", interfaceFile.Content);
        Assert.Contains("CreatePostAsync", interfaceFile.Content);
        Assert.Contains("UpdatePostAsync", interfaceFile.Content);
        Assert.Contains("DeletePostAsync", interfaceFile.Content);
    }

    [Fact]
    public void Generate_MultipleModels_ProducesMethodsForEach()
    {
        var source = @"
model Product {
    @@id: i64,
    name: utf8,
    price: f64,
}

model Order {
    @@id: i64,
    productId: i64,
    quantity: i32,
}";
        var result = CompileSource(source);

        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var context = CreateContext(result);
        var generator = new CSharpGenerator();
        var genResult = generator.Generate(context);

        var interfaceFile = genResult.Files.FirstOrDefault(f => f.TypeName == "ISchemaRepository");
        Assert.NotNull(interfaceFile);
        Assert.Contains("FindProductByIdAsync", interfaceFile!.Content);
        Assert.Contains("FindOrderByIdAsync", interfaceFile.Content);
    }

    #endregion

    #region Repository 实现生成

    [Fact]
    public void Generate_Model_ProducesRepositoryImplementation()
    {
        var source = @"
model Post {
    @@id: i64,
    title: utf8,
}";
        var result = CompileSource(source);

        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var context = CreateContext(result);
        var generator = new CSharpGenerator();
        var genResult = generator.Generate(context);

        var implFile = genResult.Files.FirstOrDefault(f => f.TypeName == "SchemaRepository");
        Assert.NotNull(implFile);
        Assert.Contains("public sealed class SchemaRepository", implFile!.Content);
        Assert.Contains(": ISchemaRepository", implFile.Content);
    }

    [Fact]
    public void Generate_Model_ImplementationContainsStubMethods()
    {
        var source = @"
model Post {
    @@id: i64,
    title: utf8,
}";
        var result = CompileSource(source);

        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var context = CreateContext(result);
        var generator = new CSharpGenerator();
        var genResult = generator.Generate(context);

        var implFile = genResult.Files.FirstOrDefault(f => f.TypeName == "SchemaRepository");
        Assert.NotNull(implFile);
        Assert.Contains("FindPostByIdAsync", implFile!.Content);
        Assert.Contains("GetAllPostsAsync", implFile.Content);
    }

    #endregion

    #region Controller 生成

    [Fact]
    public void Generate_Service_ProducesController()
    {
        var source = @"
service BlogService {
    get list_posts() -> utf8

    post create_post() -> utf8
}";
        var result = CompileSource(source);

        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var context = CreateContext(result);
        var generator = new CSharpGenerator();
        var genResult = generator.Generate(context);

        var controllerFile = genResult.Files.FirstOrDefault(f => f.TypeName == "BlogServiceController");
        Assert.NotNull(controllerFile);
        Assert.Contains("[ApiController]", controllerFile!.Content);
        Assert.Contains("public sealed class BlogServiceController", controllerFile.Content);
        Assert.Contains(": ControllerBase", controllerFile.Content);
    }

    [Fact]
    public void Generate_ServiceEndpoint_ProducesActionMethod()
    {
        var source = @"
service UserService {
    [path(""/profile""), json] get get_profile() -> utf8
}";
        var result = CompileSource(source);

        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var context = CreateContext(result);
        var generator = new CSharpGenerator();
        var genResult = generator.Generate(context);

        var controllerFile = genResult.Files.FirstOrDefault(f => f.TypeName == "UserServiceController");
        Assert.NotNull(controllerFile);
        Assert.Contains("[HttpGet]", controllerFile!.Content);
        Assert.Contains("GetProfileAsync", controllerFile.Content);
    }

    #endregion

    #region 辅助方法

    private static CompilationResult CompileSource(string source)
    {
        var compiler = new HermesCompiler();
        return compiler.CompileSource(source);
    }

    private static GeneratorContext CreateContext(CompilationResult compilationResult)
    {
        return new GeneratorContext
        {
            SchemaPath = "test.her",
            OutputPath = "/output",
            Schema = compilationResult.Schema!,
            Options = new Dictionary<string, object>
            {
                ["namespace"] = "TestNamespace",
                ["target"] = "record"
            }
        };
    }

    private static string GetDiagnosticsMessage(CompilationResult result)
    {
        if (result.Success) return "";

        var messages = result.Diagnostics.Diagnostics.Select(d => d.Message);
        return string.Join("; ", messages);
    }

    #endregion
}