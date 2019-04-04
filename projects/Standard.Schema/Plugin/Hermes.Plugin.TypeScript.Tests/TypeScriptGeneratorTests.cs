using Hermes.Generator;

namespace Hermes.Plugin.TypeScript.Tests;

public sealed class TypeScriptGeneratorTests
{
    private static readonly Dictionary<string, object> TypesOptions = new()
    {
        ["namespace"] = "Models",
        ["targets"] = "types"
    };

    [Fact]
    public void GenerateClassInterface_基本接口_生成正确()
    {
        var generator = new TypeScriptGenerator();
        var classDef = new ClassDefinition("User", new[]
        {
            new FieldDefinition("name", PrimitiveType.Utf8, false),
            new FieldDefinition("age", PrimitiveType.I32, false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = TypesOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("export interface User {", file.Content);
        Assert.Contains("name: string;", file.Content);
        Assert.Contains("age: number;", file.Content);
    }

    [Fact]
    public void GenerateClassInterface_可选字段_生成问号()
    {
        var generator = new TypeScriptGenerator();
        var classDef = new ClassDefinition("Profile", new[]
        {
            new FieldDefinition("name", PrimitiveType.Utf8, false),
            new FieldDefinition("bio", PrimitiveType.Utf8, true)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = TypesOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("name: string;", file.Content);
        Assert.Contains("bio?: string;", file.Content);
    }

    [Fact]
    public void GenerateModelInterface_模型接口_含Getter()
    {
        var generator = new TypeScriptGenerator();
        var modelDef = new ModelDefinition("Product", PrimitiveType.I32, new[]
        {
            new FieldDefinition("price", PrimitiveType.F64, false),
            new FieldDefinition("tax_rate", PrimitiveType.F64, false)
        }, new[]
        {
            new GetterDefinition("total", null, "self.price * self.tax_rate")
        });

        var storage = new StorageDefinition("shop", models: [modelDef]);
        var schema = new SchemaIR("test", storages: [storage]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = TypesOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = result.Files.First(f => f.TypeName == "Product");
        Assert.Contains("export interface Product {", file.Content);
        Assert.Contains("price: number;", file.Content);
        Assert.Contains("taxRate: number;", file.Content);
        Assert.Contains("total: unknown;", file.Content);
        Assert.Contains("computed:", file.Content);
    }

    [Fact]
    public void GenerateEnumType_枚举_生成正确()
    {
        var generator = new TypeScriptGenerator();
        var enumDef = new EnumDefinition("Status",
        [
            new EnumMember("Active", 0),
            new EnumMember("Inactive", 1)
        ]);

        var schema = new SchemaIR("test", enums: [enumDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = TypesOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("export enum Status {", file.Content);
        Assert.Contains("Active = 0", file.Content);
    }

    [Fact]
    public void GenerateFlagsType_标志_生成Enum()
    {
        var generator = new TypeScriptGenerator();
        var flagDef = new FlagsDefinition("Permissions",
        [
            new EnumMember("Read", 0),
            new EnumMember("Write", 1)
        ]);

        var schema = new SchemaIR("test", flags: [flagDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = TypesOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("export enum Permissions {", file.Content);
    }

    [Fact]
    public void GenerateUnionType_联合类型_生成DiscriminatedUnion()
    {
        var generator = new TypeScriptGenerator();
        var unionDef = new UnionDefinition("Result",
        [
            new UnionVariant("Ok", null, PrimitiveType.Utf8),
            new UnionVariant("Error", null, PrimitiveType.Utf8)
        ]);

        var schema = new SchemaIR("test", unions: [unionDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = TypesOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("export type Result =", file.Content);
        Assert.Contains("ResultOk", file.Content);
        Assert.Contains("ResultError", file.Content);
        Assert.Contains("kind:", file.Content);
    }

    [Fact]
    public void GenerateServiceClient_HTTP_生成FetchClient()
    {
        var generator = new TypeScriptGenerator();
        var serviceDef = new ServiceDefinition("User",
        [
            new HttpEndpoint("getUser", [], PrimitiveType.Utf8, "GET", "/api/users")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = TypesOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("export class UserClient {", file.Content);
        Assert.Contains("async getUser(): Promise<string>", file.Content);
        Assert.Contains("method: 'GET'", file.Content);
    }

    [Fact]
    public void GenerateServiceClient_POST_生成JSONBody()
    {
        var generator = new TypeScriptGenerator();
        var serviceDef = new ServiceDefinition("Order",
        [
            new HttpEndpoint("create", [], PrimitiveType.Utf8, "POST", "/api/orders", true,
            [
                new Common.AttributeDefinition("json", [])
            ])
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = TypesOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("method: 'POST'", file.Content);
        Assert.Contains("Content-Type': 'application/json'", file.Content);
        Assert.Contains("response.json()", file.Content);
    }

    [Fact]
    public void MapType_Primitive_正确映射()
    {
        var generator = new TypeScriptGenerator();
        var classDef = new ClassDefinition("TypeCheck", new[]
        {
            new FieldDefinition("a", PrimitiveType.I32, false),
            new FieldDefinition("b", PrimitiveType.F64, false),
            new FieldDefinition("c", PrimitiveType.Bool, false),
            new FieldDefinition("d", PrimitiveType.Utf8, false),
            new FieldDefinition("e", PrimitiveType.Uuid, false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = TypesOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("a: number;", file.Content);
        Assert.Contains("b: number;", file.Content);
        Assert.Contains("c: boolean;", file.Content);
        Assert.Contains("d: string;", file.Content);
        Assert.Contains("e: string;", file.Content);
    }

    [Fact]
    public void MapType_List_生成数组()
    {
        var generator = new TypeScriptGenerator();
        var classDef = new ClassDefinition("ListContainer", new[]
        {
            new FieldDefinition("items", new ListType(PrimitiveType.Utf8), false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = TypesOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("items: string[];", file.Content);
    }

    [Fact]
    public void MapType_Option_生成NullUnion()
    {
        var generator = new TypeScriptGenerator();
        var classDef = new ClassDefinition("Nullable", new[]
        {
            new FieldDefinition("value", new OptionType(PrimitiveType.I32), false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = TypesOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("value: number | null;", file.Content);
    }

    [Fact]
    public void MapType_Dict_生成Record()
    {
        var generator = new TypeScriptGenerator();
        var classDef = new ClassDefinition("Dictionary", new[]
        {
            new FieldDefinition("map", new DictType(
                PrimitiveType.Utf8,
                PrimitiveType.I32), false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = TypesOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("map: Record<string, number>;", file.Content);
    }

    [Fact]
    public void ToCamelCase_SnakeCase_正确转换()
    {
        var generator = new TypeScriptGenerator();
        var classDef = new ClassDefinition("NamingTest", new[]
        {
            new FieldDefinition("user_name", PrimitiveType.Utf8, false),
            new FieldDefinition("first_name", PrimitiveType.Utf8, false),
            new FieldDefinition("created_at", PrimitiveType.Utf8, false),
            new FieldDefinition("id", PrimitiveType.I32, false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = TypesOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("userName: string;", file.Content);
        Assert.Contains("firstName: string;", file.Content);
        Assert.Contains("createdAt: string;", file.Content);
        Assert.Contains("id: number;", file.Content);
    }

    [Fact]
    public void ToCamelCase_PascalCase_转小写开头()
    {
        var generator = new TypeScriptGenerator();
        var classDef = new ClassDefinition("Pascal", new[]
        {
            new FieldDefinition("Name", PrimitiveType.Utf8, false),
            new FieldDefinition("IsActive", PrimitiveType.Bool, false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = TypesOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("name: string;", file.Content);
        Assert.Contains("isActive: boolean;", file.Content);
    }

    [Fact]
    public void GenerateIncremental_变更检测_仅生成修改项()
    {
        var generator = new TypeScriptGenerator();
        var classDef = new ClassDefinition("User", new[]
        {
            new FieldDefinition("name", PrimitiveType.Utf8, false)
        }, []);
        var classDef2 = new ClassDefinition("Role", new[]
        {
            new FieldDefinition("title", PrimitiveType.Utf8, false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef, classDef2]);
        var context = new IncrementalGeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = TypesOptions,
            SchemaPath = "./schema.he",
            ChangedTypeNames = new HashSet<string> { "User" }
        };

        var result = generator.GenerateIncremental(context);

        Assert.Equal(1, result.Files.Count);
        var file = Assert.Single(result.Files);
        Assert.Equal("User", file.TypeName);
        Assert.Equal(FileChangeKind.Added, file.ChangeKind);
    }

    [Fact]
    public void GenerateIncremental_Model变更_标记Modified()
    {
        var generator = new TypeScriptGenerator();
        var modelDef = new ModelDefinition("Item", PrimitiveType.I32, new[]
        {
            new FieldDefinition("name", PrimitiveType.Utf8, false)
        });

        var storage = new StorageDefinition("inventory", models: [modelDef]);
        var schema = new SchemaIR("test", storages: [storage]);
        var context = new IncrementalGeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = TypesOptions,
            SchemaPath = "./schema.he",
            ChangedTypeNames = new HashSet<string> { "Item" }
        };

        var result = generator.GenerateIncremental(context);

        var file = Assert.Single(result.Files);
        Assert.Equal("Item", file.TypeName);
        Assert.Equal(FileChangeKind.Modified, file.ChangeKind);
    }
}