using Hermes.Generator;

namespace Hermes.Plugin.GGScript.Tests;

public sealed class GGScriptGeneratorTests
{
    private static readonly Dictionary<string, object> Options = new()
    {
        ["namespace"] = "Test",
        ["targets"] = "struct"
    };

    [Fact]
    public void GenerateStruct_基本字段_输出正确()
    {
        var generator = new GGScriptGenerator();
        var classDef = new ClassDefinition("Player", new[]
        {
            new FieldDefinition("name", PrimitiveType.Utf8, false),
            new FieldDefinition("level", PrimitiveType.I32, false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = Options };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("struct Player", file.Content);
        Assert.Contains("name: string;", file.Content);
        Assert.Contains("level: int;", file.Content);
    }

    [Fact]
    public void GenerateStruct_可选字段_标记问号()
    {
        var generator = new GGScriptGenerator();
        var classDef = new ClassDefinition("Config", new[]
        {
            new FieldDefinition("title", PrimitiveType.Utf8, false),
            new FieldDefinition("desc", PrimitiveType.Utf8, true)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);

        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = Options };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("title: string;", file.Content);
        Assert.Contains("desc?: string;", file.Content);
    }

    [Fact]
    public void GenerateModel_模型struct_输出正确()
    {
        var generator = new GGScriptGenerator();
        var modelDef = new ModelDefinition("Item", PrimitiveType.I32, new[]
        {
            new FieldDefinition("id", PrimitiveType.I32, false, null, [new("key", [])]),
            new FieldDefinition("count", PrimitiveType.I32, false)
        });

        var storage = new StorageDefinition("bag", models: [modelDef]);
        var schema = new SchemaIR("test", storages: [storage]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = Options };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = result.Files.First(f => f.TypeName == "Item");
        Assert.Contains("struct Item", file.Content);
        Assert.Contains("id: int;", file.Content);
        Assert.Contains("count: int;", file.Content);
    }

    [Fact]
    public void GenerateEnum_枚举_输出正确()
    {
        var generator = new GGScriptGenerator();
        var enumDef = new EnumDefinition("Direction",
        [
            new EnumMember("North", 0),
            new EnumMember("South", 1),
            new EnumMember("East", 2),
            new EnumMember("West", 3)
        ]);

        var schema = new SchemaIR("test", enums: [enumDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = Options };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("enum Direction", file.Content);
        Assert.Contains("North = 0", file.Content);
        Assert.Contains("East = 2,", file.Content);
    }

    [Fact]
    public void GenerateFlags_标志类型_输出正确()
    {
        var generator = new GGScriptGenerator();
        var flagDef = new FlagsDefinition("Access",
        [
            new EnumMember("Read", 0),
            new EnumMember("Write", 1),
            new EnumMember("Admin", 2)
        ]);

        var schema = new SchemaIR("test", flags: [flagDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = Options };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("flags Access", file.Content);
        Assert.Contains("Read = 0", file.Content);
    }

    [Fact]
    public void GenerateUnion_联合类型_输出正确()
    {
        var generator = new GGScriptGenerator();
        var unionDef = new UnionDefinition("Payload",
        [
            new UnionVariant("Text", null, PrimitiveType.Utf8),
            new UnionVariant("Data", null, new ListType(PrimitiveType.U8))
        ]);

        var schema = new SchemaIR("test", unions: [unionDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = Options };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("union Payload", file.Content);
        Assert.Contains("Text: string", file.Content);
        Assert.Contains("Data: uint[]", file.Content);
    }

    [Fact]
    public void MapType_List_输出数组()
    {
        var generator = new GGScriptGenerator();
        var classDef = new ClassDefinition("Bag", new[]
        {
            new FieldDefinition("items", new ListType(PrimitiveType.I32), false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);

        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = Options };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("items: int[];", file.Content);
    }

    [Fact]
    public void MapType_Option_输出可选标记()
    {
        var generator = new GGScriptGenerator();
        var classDef = new ClassDefinition("Nullable", new[]
        {
            new FieldDefinition("value", new OptionType(PrimitiveType.F64), false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = Options };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("value: double?;", file.Content);
    }

    [Fact]
    public void MapType_Dict_输出Map()
    {
        var generator = new GGScriptGenerator();
        var classDef = new ClassDefinition("Settings", new[]
        {
            new FieldDefinition("kv", new DictType(PrimitiveType.Utf8, PrimitiveType.I32), false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);

        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = Options };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("kv: map<string, int>;", file.Content);
    }

    [Fact]
    public void GenerateService_HTTP_输出正确()
    {
        var generator = new GGScriptGenerator();
        var serviceDef = new ServiceDefinition("API",
        [
            new HttpEndpoint("ping", [], PrimitiveType.Utf8, "GET", "/api/ping")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = Options };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("service API", file.Content);
        Assert.Contains("GET /api/ping", file.Content);
        Assert.Contains("// → string", file.Content);
    }

    [Fact]
    public void GenerateFileHeader_含autoGenerated()
    {
        var generator = new GGScriptGenerator();
        var classDef = new ClassDefinition("Foo", new[]
        {
            new FieldDefinition("x", PrimitiveType.I32, false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);

        var context = new GeneratorContext
            { Schema = schema, OutputPath = "./out", Options = Options, SchemaPath = "/game/test.he" };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("<auto-generated>", file.Content);
        Assert.Contains("hermes 命令自动生成", file.Content);
        Assert.Contains("/game/test.he", file.Content);
    }

    [Fact]
    public void MapType_NamedType_原样输出()
    {
        var generator = new GGScriptGenerator();
        var classDef = new ClassDefinition("Ref", new[]
        {
            new FieldDefinition("target", new NamedType("Enemy"), false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);

        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = Options };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("target: Enemy;", file.Content);
    }
}