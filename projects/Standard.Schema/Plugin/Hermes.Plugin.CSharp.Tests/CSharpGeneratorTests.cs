using Hermes.Generator;

namespace Hermes.Plugin.CSharp.Tests;

public sealed class CSharpGeneratorTests
{
    private static SchemaType IntType => PrimitiveType.I32;

    private static SchemaType StringType => PrimitiveType.Utf8;

    private static SchemaType BoolType => PrimitiveType.Bool;

    private static SchemaType FloatType => PrimitiveType.F64;

    [Fact]
    public void GenerateRecord_基本Class_生成正确()
    {
        var generator = new CSharpGenerator();
        var classDef = new ClassDefinition("User", new[]
        {
            new FieldDefinition("name", StringType, false),
            new FieldDefinition("age", IntType, false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);
        var context = new GeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = new Dictionary<string, object> { ["namespace"] = "Models", ["targets"] = "record" }
        };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("public record User", file.Content);
        Assert.Contains("public string Name", file.Content);
        Assert.Contains("public int Age", file.Content);
    }

    [Fact]
    public void GenerateClass_模式_生成SealedClass()
    {
        var generator = new CSharpGenerator();
        var classDef = new ClassDefinition("Config", new[]
        {
            new FieldDefinition("port", IntType, false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);

        var context = new GeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = new Dictionary<string, object> { ["namespace"] = "App", ["targets"] = "class" }
        };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("public sealed class Config", file.Content);
        Assert.DoesNotContain("public sealed record", file.Content);
    }

    [Fact]
    public void GenerateJson_模式_生成JsonAttribute()
    {
        var generator = new CSharpGenerator();
        var classDef = new ClassDefinition("ApiResponse", new[]
        {
            new FieldDefinition("data", StringType, false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);

        var context = new GeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = new Dictionary<string, object> { ["namespace"] = "Api", ["targets"] = "json" }
        };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("JsonSerializable", file.Content);
        Assert.Contains("JsonPropertyName", file.Content);
    }

    [Fact]
    public void GenerateModel_含Getter_生成正确()
    {
        var generator = new CSharpGenerator();
        var modelDef = new ModelDefinition("Product", IntType, new[]
        {
            new FieldDefinition("price", FloatType, false),
            new FieldDefinition("tax_rate", FloatType, false)
        }, new[]
        {
            new GetterDefinition("total", null, "self.price * self.tax_rate")
        });

        var storage = new StorageDefinition("shop", models: [modelDef]);
        var schema = new SchemaIR("test", storages: [storage]);
        var context = new GeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = new Dictionary<string, object> { ["namespace"] = "Shop", ["targets"] = "record" }
        };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var modelFile = result.Files.First(f => f.TypeName == "Product");
        Assert.Contains("public double Price", modelFile.Content);
        Assert.Contains("public double TaxRate", modelFile.Content);
        Assert.Contains("public object Total => this.Price * this.TaxRate;", modelFile.Content);
    }

    [Fact]
    public void GenerateEnum_生成正确()
    {
        var generator = new CSharpGenerator();
        var enumDef = new EnumDefinition("OrderStatus",
        [
            new EnumMember("Pending", 0),
            new EnumMember("Shipped", 1),
            new EnumMember("Delivered", 2)
        ]);

        var schema = new SchemaIR("test", enums: [enumDef]);
        var context = new GeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = new Dictionary<string, object> { ["namespace"] = "Models" }
        };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("public enum OrderStatus", file.Content);
        Assert.Contains("Pending = 0", file.Content);
    }

    [Fact]
    public void GenerateFlags_生成正确()
    {
        var generator = new CSharpGenerator();
        var flagDef = new FlagsDefinition("Permissions",
        [
            new EnumMember("Read", 0),
            new EnumMember("Write", 1),
            new EnumMember("Execute", 2)
        ]);

        var schema = new SchemaIR("test", flags: [flagDef]);
        var context = new GeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = new Dictionary<string, object> { ["namespace"] = "Auth" }
        };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("[Flags]", file.Content);
        Assert.Contains("public enum Permissions", file.Content);
        Assert.Contains("1 << 0", file.Content);
    }

    [Fact]
    public void GenerateUnion_生成正确()
    {
        var generator = new CSharpGenerator();
        var unionDef = new UnionDefinition("Result",
        [
            new UnionVariant("Ok", null, PrimitiveType.Utf8),
            new UnionVariant("Error", null, PrimitiveType.Utf8)
        ]);

        var schema = new SchemaIR("test", unions: [unionDef]);
        var context = new GeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = new Dictionary<string, object> { ["namespace"] = "Sonic.Core", ["targets"] = "record" }
        };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("public abstract record Result", file.Content);
        Assert.Contains("OkVariant", file.Content);
        Assert.Contains("ErrorVariant", file.Content);
    }

    [Fact]
    public void HasOne_导航属性_生成正确()
    {
        var generator = new CSharpGenerator();
        var modelDef = new ModelDefinition("Comment", IntType, new[]
        {
            new FieldDefinition("id", IntType, false, null, [new("key", [])]),
            new FieldDefinition("author_id", IntType, false, null,
            [
                new("HasOne",
                [
                    new KeyValuePair<string, string>("type", "User")
                ])
            ]),
            new FieldDefinition("content", StringType, false)
        });

        var storage = new StorageDefinition("blog", models: [modelDef]);
        var schema = new SchemaIR("test", storages: [storage]);
        var context = new GeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = new Dictionary<string, object> { ["namespace"] = "Blog", ["targets"] = "record" }
        };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = result.Files.First(f => f.TypeName == "Comment");
        Assert.Contains("public User AuthorId { get; set; }", file.Content);
    }

    [Fact]
    public void HasMany_导航属性_生成正确()
    {
        var generator = new CSharpGenerator();
        var modelDef = new ModelDefinition("Blog", IntType, new[]
        {
            new FieldDefinition("id", IntType, false, null, [new("key", [])]),
            new FieldDefinition("posts", StringType, false, null,
            [
                new("HasMany",
                [
                    new KeyValuePair<string, string>("type", "Post")
                ])
            ])
        });

        var storage = new StorageDefinition("cms", models: [modelDef]);
        var schema = new SchemaIR("test", storages: [storage]);
        var context = new GeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = new Dictionary<string, object> { ["namespace"] = "CMS", ["targets"] = "record" }
        };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = result.Files.First(f => f.TypeName == "Blog");
        Assert.Contains("ICollection<Post> Posts", file.Content);
    }

    [Fact]
    public void Required_验证属性_生成正确()
    {
        var generator = new CSharpGenerator();
        var modelDef = new ModelDefinition("User", IntType, new[]
        {
            new FieldDefinition("id", IntType, false, null, [new("key", [])]),
            new FieldDefinition("email", StringType, false, null, [new("required", [])])
        });

        var storage = new StorageDefinition("app", models: [modelDef]);
        var schema = new SchemaIR("test", storages: [storage]);

        var context = new GeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = new Dictionary<string, object> { ["namespace"] = "App" }
        };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = result.Files.First(f => f.TypeName == "User");
        Assert.Contains("Required", file.Content);
    }

    [Fact]
    public void MaxLength_验证属性_生成正确()
    {
        var generator = new CSharpGenerator();
        var modelDef = new ModelDefinition("Profile", IntType, new[]
        {
            new FieldDefinition("id", IntType, false, null, [new("key", [])]),
            new FieldDefinition("bio", StringType, false, null,
            [
                new("maxLength",
                [
                    new KeyValuePair<string, string>("", "500")
                ])
            ])
        });

        var storage = new StorageDefinition("app", models: [modelDef]);
        var schema = new SchemaIR("test", storages: [storage]);

        var context = new GeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = new Dictionary<string, object> { ["namespace"] = "App" }
        };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = result.Files.First(f => f.TypeName == "Profile");
        Assert.Contains("StringLength(500)", file.Content);
    }

    [Fact]
    public void MinMax_验证属性_生成Range()
    {
        var generator = new CSharpGenerator();
        var modelDef = new ModelDefinition("Score", IntType, new[]
        {
            new FieldDefinition("id", IntType, false, null, [new("key", [])]),
            new FieldDefinition("value", IntType, false, null,
            [
                new("min", [new KeyValuePair<string, string>("", "0")]),
                new("max", [new KeyValuePair<string, string>("", "100")])
            ])
        });

        var storage = new StorageDefinition("game", models: [modelDef]);
        var schema = new SchemaIR("test", storages: [storage]);

        var context = new GeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = new Dictionary<string, object> { ["namespace"] = "Game" }
        };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = result.Files.First(f => f.TypeName == "Score");
        Assert.Contains("Range(0, int.MaxValue)", file.Content);
        Assert.Contains("Range(int.MinValue, 100)", file.Content);
    }

    [Fact]
    public void Regex_Email_验证属性_生成正确()
    {
        var generator = new CSharpGenerator();
        var modelDef = new ModelDefinition("Contact", IntType, new[]
        {
            new FieldDefinition("id", IntType, false, null, [new("key", [])]),
            new FieldDefinition("email", StringType, false, null, [new("email", [])]),
            new FieldDefinition("phone", StringType, false, null,
            [
                new("regex", [new KeyValuePair<string, string>("", @"^\d{11}$")])
            ])
        });

        var storage = new StorageDefinition("crm", models: [modelDef]);
        var schema = new SchemaIR("test", storages: [storage]);
        var context = new GeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = new Dictionary<string, object> { ["namespace"] = "CRM" }
        };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = result.Files.First(f => f.TypeName == "Contact");
        Assert.Contains("EmailAddress", file.Content);
        Assert.Contains(@"RegularExpression(""^\d{11}$"")", file.Content);
    }

    [Fact]
    public void Repository_接口_生成正确()
    {
        var generator = new CSharpGenerator();
        var modelDef = new ModelDefinition("Item", IntType, new[]
        {
            new FieldDefinition("id", IntType, false, null, [new("key", [])]),
            new FieldDefinition("name", StringType, false)
        });

        var storage = new StorageDefinition("warehouse", models: [modelDef]);
        var schema = new SchemaIR("test", storages: [storage]);
        var context = new GeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = new Dictionary<string, object> { ["namespace"] = "Warehouse" }
        };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var ifaceFile = result.Files.First(f => f.TypeName == "IWarehouseRepository");
        Assert.Contains("public interface IWarehouseRepository", ifaceFile.Content);
        Assert.Contains("FindItemByIdAsync", ifaceFile.Content);
        Assert.Contains("CreateItemAsync", ifaceFile.Content);
    }

    [Fact]
    public void Controller_生成正确()
    {
        var generator = new CSharpGenerator();
        var serviceDef = new ServiceDefinition("Health",
        [
            new HttpEndpoint("check", [], null, "GET", "/api/health")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = new Dictionary<string, object> { ["namespace"] = "Api" }
        };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("HealthController", file.Content);
        Assert.Contains("[ApiController]", file.Content);
        Assert.Contains("[HttpGet]", file.Content);
    }

    [Fact]
    public void PrimitiveType_类型映射_正确()
    {
        var generator = new CSharpGenerator();
        var classDef = new ClassDefinition("Types", new[]
        {
            new FieldDefinition("a", PrimitiveType.I32, false),
            new FieldDefinition("b", PrimitiveType.F64, false),
            new FieldDefinition("c", PrimitiveType.Bool, false),
            new FieldDefinition("d", PrimitiveType.Utf8, false),
            new FieldDefinition("e", PrimitiveType.Uuid, false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);

        var context = new GeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = new Dictionary<string, object> { ["namespace"] = "Types" }
        };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("public int A", file.Content);
        Assert.Contains("public double B", file.Content);
        Assert.Contains("public bool C", file.Content);
        Assert.Contains("public string D", file.Content);
        Assert.Contains("public Guid E", file.Content);
    }

    [Fact]
    public void ListType_类型映射_正确()
    {
        var generator = new CSharpGenerator();
        var classDef = new ClassDefinition("Container", new[]
        {
            new FieldDefinition("items", new ListType(PrimitiveType.Utf8), false),
            new FieldDefinition("optional", new OptionType(PrimitiveType.I32), false, null, null)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);

        var context = new GeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = new Dictionary<string, object> { ["namespace"] = "Data" }
        };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("List<string> Items", file.Content);
        Assert.Contains("int? Optional", file.Content);
    }

    [Fact]
    public void CamelCase_命名_自动转换()
    {
        var generator = new CSharpGenerator();
        var classDef = new ClassDefinition("SnakeTest", new[]
        {
            new FieldDefinition("user_name", StringType, false),
            new FieldDefinition("first_name", StringType, false),
            new FieldDefinition("created_at", StringType, false)
        }, []);

        var schema = new SchemaIR("test", classes: [classDef]);

        var context = new GeneratorContext
        {
            Schema = schema,
            OutputPath = "./out",
            Options = new Dictionary<string, object> { ["namespace"] = "Naming" }
        };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var file = Assert.Single(result.Files);
        Assert.Contains("public string UserName", file.Content);
        Assert.Contains("public string FirstName", file.Content);
        Assert.Contains("public string CreatedAt", file.Content);
    }
}