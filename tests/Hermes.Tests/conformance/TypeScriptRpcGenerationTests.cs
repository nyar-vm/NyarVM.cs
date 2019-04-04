using Hermes.Compiler;
using Hermes.Generator;
using Xunit;

namespace Hermes.Tests.Conformance;

public class TypeScriptRpcGenerationTests
{
    #region Service Client 生成

    [Fact]
    public void GenerateRpc_ServiceWithGetEndpoint_ProducesClientClass()
    {
        var source = @"
service BlogService {
    [path(""/posts""), json] get list_posts() -> utf8
}";
        var result = CompileSource(source);
        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var context = CreateContext(result);
        var generator = new TypeScriptGenerator();
        var genResult = generator.Generate(context);

        var clientFile = genResult.Files.FirstOrDefault(f => f.TypeName == "BlogServiceClient");
        Assert.NotNull(clientFile);
        Assert.Contains("export class BlogServiceClient", clientFile!.Content);
        Assert.Contains("baseUrl", clientFile.Content);
    }

    [Fact]
    public void GenerateRpc_GetEndpoint_GeneratesFetchCall()
    {
        var source = @"
service UserService {
    [path(""/profile""), json] get get_profile() -> utf8
}";
        var result = CompileSource(source);
        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var context = CreateContext(result);
        var generator = new TypeScriptGenerator();
        var genResult = generator.Generate(context);

        var clientFile = genResult.Files.FirstOrDefault(f => f.TypeName == "UserServiceClient");
        Assert.NotNull(clientFile);
        Assert.Contains("method: 'GET'", clientFile!.Content);
        Assert.Contains("async getProfile", clientFile.Content);
    }

    [Fact]
    public void GenerateRpc_PostEndpoint_IncludesRequestBody()
    {
        var source = @"
service BlogService {
    [path(""/posts""), json] post create_post() -> utf8
}";
        var result = CompileSource(source);
        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var context = CreateContext(result);
        var generator = new TypeScriptGenerator();
        var genResult = generator.Generate(context);

        var clientFile = genResult.Files.FirstOrDefault(f => f.TypeName == "BlogServiceClient");
        Assert.NotNull(clientFile);
        Assert.Contains("method: 'POST'", clientFile!.Content);
        Assert.Contains("JSON.stringify(body)", clientFile.Content);
    }

    [Fact]
    public void GenerateRpc_MultipleEndpoints_AllIncluded()
    {
        var source = @"
service ApiService {
    [path(""/items""), json] get list_items() -> utf8
    [path(""/items""), json] post add_item() -> utf8
    [path(""/items/{id}""), json] delete remove_item() -> utf8
}";
        var result = CompileSource(source);
        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var context = CreateContext(result);
        var generator = new TypeScriptGenerator();
        var genResult = generator.Generate(context);

        var clientFile = genResult.Files.FirstOrDefault(f => f.TypeName == "ApiServiceClient");
        Assert.NotNull(clientFile);
        Assert.Contains("listItems", clientFile!.Content);
        Assert.Contains("addItem", clientFile.Content);
        Assert.Contains("removeItem", clientFile.Content);
    }

    [Fact]
    public void GenerateRpc_TypesAlsoGenerated()
    {
        var source = @"
model Post {
    @@id: i64,
    title: utf8,
}

service BlogService {
    [path(""/posts""), json] get list_posts() -> utf8
}";
        var result = CompileSource(source);
        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var context = CreateContext(result);
        var generator = new TypeScriptGenerator();
        var genResult = generator.Generate(context);

        Assert.Contains(genResult.Files, f => f.TypeName == "Post");
        Assert.Contains(genResult.Files, f => f.TypeName == "BlogServiceClient");
    }

    [Fact]
    public void GenerateRpc_JsonEndpoint_ReturnsParsedJson()
    {
        var source = @"
service ApiService {
    [path(""/data""), json] get fetch_data() -> utf8
}";
        var result = CompileSource(source);
        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var context = CreateContext(result);
        var generator = new TypeScriptGenerator();
        var genResult = generator.Generate(context);

        var clientFile = genResult.Files.FirstOrDefault(f => f.TypeName == "ApiServiceClient");
        Assert.NotNull(clientFile);
        Assert.Contains("return response.json()", clientFile!.Content);
    }

    #endregion

    #region 辅助方法

    private static CompilationResult CompileSource(string source)
    {
        var compiler = new HermesCompiler();
        return compiler.CompileSource(source);
    }

    private static GeneratorContext CreateContext(CompilationResult result)
    {
        return new GeneratorContext
        {
            SchemaPath = "test.her",
            OutputPath = "/output",
            Schema = result.Schema!,
            Options = new Dictionary<string, object>
            {
                ["namespace"] = "Api",
                ["target"] = "types"
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