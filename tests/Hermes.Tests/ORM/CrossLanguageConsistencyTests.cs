using Hermes.Generator;
using Hermes.Plugin.Java;
using Xunit;
using Xunit.Abstractions;

namespace Hermes.ORM.Tests;

/// <summary>
///     跨语言一致性测试——验证同一 Schema 在 C#/TypeScript/Java 三语言中生成的代码结构一致
/// </summary>
public sealed class CrossLanguageConsistencyTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region 类型映射对照表验证

    [Theory]
    [InlineData("i8", "sbyte", "number", "byte")]
    [InlineData("i16", "short", "number", "short")]
    [InlineData("i32", "int", "number", "int")]
    [InlineData("i64", "long", "number", "long")]
    [InlineData("u8", "byte", "number", "short")]
    [InlineData("u16", "ushort", "number", "int")]
    [InlineData("u32", "uint", "number", "long")]
    [InlineData("u64", "ulong", "number", "long")]
    [InlineData("f32", "float", "number", "float")]
    [InlineData("f64", "double", "number", "double")]
    [InlineData("bool", "bool", "boolean", "boolean")]
    [InlineData("utf8", "string", "string", "String")]
    public void PrimitiveTypeMapping_ConsistentAcrossLanguages(
        string schemaType, string csharpType, string tsType, string javaType)
    {
        var schema = CreateSchemaWithSingleField(schemaType);
        var results = GenerateAllLanguages(schema);

        var csharpContent = GetMainFileContent(results["csharp"]);
        var tsContent = GetMainFileContent(results["typescript"]);
        var javaContent = GetMainFileContent(results["java"]);

        Assert.Contains(csharpType, csharpContent);
        Assert.Contains(tsType, tsContent);
        Assert.Contains(javaType, javaContent);

        _output.WriteLine($"  {schemaType} → C#:{csharpType}, TS:{tsType}, Java:{javaType}");
    }

    #endregion

    #region 类型映射一致性

    [Fact]
    public void PrimitiveTypes_AllLanguagesGenerateFields()
    {
        var schema = CreateSchemaWithAllPrimitiveTypes();
        var results = GenerateAllLanguages(schema);

        foreach (var (lang, result) in results)
        {
            Assert.True(result.Success, $"{lang} 生成失败：{string.Join(", ", result.Errors)}");
            Assert.True(result.Files.Count > 0, $"{lang} 未生成任何文件");

            var mainFile = result.Files.FirstOrDefault(f => f.Path.Contains("AllPrimitives")) ?? result.Files[0];
            Assert.True(mainFile.Content.Length > 100, $"{lang} 生成的文件内容过短");

            _output.WriteLine($"  {lang}: {result.Files.Count} 个文件, 主文件 {mainFile.Content.Length} 字符");
        }
    }

    [Fact]
    public void OptionalFields_AllLanguagesGenerateSuccessfully()
    {
        var schema = CreateSchemaWithOptionalFields();
        var results = GenerateAllLanguages(schema);

        foreach (var (lang, result) in results)
        {
            Assert.True(result.Success, $"{lang} 生成失败：{string.Join(", ", result.Errors)}");
            var content = GetAllFileContent(result);
            Assert.Contains("name", content.ToLowerInvariant());
            Assert.Contains("age", content.ToLowerInvariant());
            Assert.Contains("email", content.ToLowerInvariant());
        }

        _output.WriteLine("  C# 可空字段生成成功");
        _output.WriteLine("  TypeScript 可空字段生成成功");
        _output.WriteLine("  Java 可空字段生成成功");
    }

    [Fact]
    public void ListFields_AllLanguagesRepresentCollection()
    {
        var schema = CreateSchemaWithListFields();
        var results = GenerateAllLanguages(schema);

        var csharpContent = GetMainFileContent(results["csharp"]);
        var tsContent = GetMainFileContent(results["typescript"]);
        var javaContent = GetMainFileContent(results["java"]);

        Assert.Contains("List<", csharpContent);
        Assert.Contains("Array<", tsContent);
        Assert.Contains("List<", javaContent);

        _output.WriteLine("  C# 列表表示: List<T>");
        _output.WriteLine("  TypeScript 列表表示: Array<T>");
        _output.WriteLine("  Java 列表表示: List<T>");
    }

    [Fact]
    public void DictFields_AllLanguagesRepresentMap()
    {
        var schema = CreateSchemaWithDictFields();
        var results = GenerateAllLanguages(schema);

        var csharpContent = GetMainFileContent(results["csharp"]);
        var tsContent = GetMainFileContent(results["typescript"]);
        var javaContent = GetMainFileContent(results["java"]);

        Assert.Contains("Dictionary<", csharpContent);
        Assert.Contains("Record<", tsContent);
        Assert.Contains("Map<", javaContent);

        _output.WriteLine("  C# 字典表示: Dictionary<K,V>");
        _output.WriteLine("  TypeScript 字典表示: Record<K,V>");
        _output.WriteLine("  Java 字典表示: Map<K,V>");
    }

    #endregion

    #region 实体结构一致性

    [Fact]
    public void EntityStructure_AllLanguagesGenerateSameFields()
    {
        var schema = CreateTypicalEntitySchema();
        var results = GenerateAllLanguages(schema);

        foreach (var (lang, result) in results)
            Assert.True(result.Success, $"{lang} 生成失败：{string.Join(", ", result.Errors)}");

        var csharpContent = GetAllFileContent(results["csharp"]);
        var tsContent = GetAllFileContent(results["typescript"]);
        var javaContent = GetAllFileContent(results["java"]);

        Assert.Contains("Id", csharpContent);
        Assert.Contains("Name", csharpContent);
        Assert.Contains("IsActive", csharpContent);

        Assert.Contains("id", tsContent);
        Assert.Contains("name", tsContent);
        Assert.Contains("isActive", tsContent);

        Assert.Contains("id", javaContent);
        Assert.Contains("name", javaContent);
        Assert.Contains("isActive", javaContent);

        _output.WriteLine("  所有语言均包含 Id/Name/IsActive 字段");
    }

    [Fact]
    public void EnumDefinition_AllLanguagesGenerateAllValues()
    {
        var schema = CreateSchemaWithEnum();
        var results = GenerateAllLanguages(schema);

        var csharpContent = GetMainFileContent(results["csharp"]);
        var tsContent = GetMainFileContent(results["typescript"]);
        var javaContent = GetMainFileContent(results["java"]);

        var enumValues = new[] { "Active", "Inactive", "Suspended", "Deleted" };

        foreach (var value in enumValues)
        {
            Assert.Contains(value, csharpContent);
            Assert.Contains(value, tsContent);
            Assert.Contains(value, javaContent);
        }

        _output.WriteLine($"  所有语言均包含 {enumValues.Length} 个枚举值");
    }

    [Fact]
    public void FlagsDefinition_AllLanguagesGenerateAllFlags()
    {
        var schema = CreateSchemaWithFlags();
        var results = GenerateAllLanguages(schema);

        var csharpContent = GetMainFileContent(results["csharp"]);
        var tsContent = GetMainFileContent(results["typescript"]);
        var javaContent = GetMainFileContent(results["java"]);

        var flagValues = new[] { "Read", "Write", "Execute", "Admin" };

        foreach (var value in flagValues)
        {
            Assert.Contains(value, csharpContent);
            Assert.Contains(value, tsContent);
            Assert.Contains(value, javaContent);
        }

        _output.WriteLine($"  所有语言均包含 {flagValues.Length} 个标志值");
    }

    #endregion

    #region 值对象一致性

    [Fact]
    public void ValueObject_AllLanguagesGenerateImmutableType()
    {
        var schema = CreateSchemaWithValueObject();
        var results = GenerateAllLanguages(schema);

        foreach (var (lang, result) in results)
        {
            Assert.True(result.Success, $"{lang} 生成失败：{string.Join(", ", result.Errors)}");
            Assert.True(result.Files.Count > 0, $"{lang} 未生成任何文件，文件数={result.Files.Count}");
        }

        var csharpContent = GetAllFileContent(results["csharp"]);
        var tsContent = GetAllFileContent(results["typescript"]);
        var javaContent = GetAllFileContent(results["java"]);

        Assert.Contains("readonly record struct", csharpContent);
        Assert.Contains("readonly", tsContent);
        Assert.Contains("final record", javaContent);

        _output.WriteLine("  C# 生成 readonly record struct");
        _output.WriteLine("  TypeScript 生成 readonly interface");
        _output.WriteLine("  Java 生成 final record");
    }

    [Fact]
    public void ValueObject_AllLanguagesGenerateAllFields()
    {
        var schema = CreateSchemaWithValueObject();
        var results = GenerateAllLanguages(schema);

        var csharpContent = GetAllFileContent(results["csharp"]);
        var tsContent = GetAllFileContent(results["typescript"]);
        var javaContent = GetAllFileContent(results["java"]);

        Assert.Contains("x", csharpContent);
        Assert.Contains("y", csharpContent);
        Assert.Contains("z", csharpContent);

        Assert.Contains("x", tsContent);
        Assert.Contains("y", tsContent);
        Assert.Contains("z", tsContent);

        Assert.Contains("x", javaContent);
        Assert.Contains("y", javaContent);
        Assert.Contains("z", javaContent);

        _output.WriteLine("  所有语言均包含 X/Y/Z 字段");
    }

    [Fact]
    public void ValueObject_WithValidation_AllLanguagesGenerateChecks()
    {
        var schema = CreateSchemaWithValidatedValueObject();
        var results = GenerateAllLanguages(schema);

        var csharpContent = GetAllFileContent(results["csharp"]);
        var javaContent = GetAllFileContent(results["java"]);

        Assert.Contains("ArgumentOutOfRangeException", csharpContent);
        Assert.Contains("IllegalArgumentException", javaContent);

        _output.WriteLine("  C# 生成 ArgumentOutOfRangeException 验证");
        _output.WriteLine("  Java 生成 IllegalArgumentException 验证");
    }

    [Fact]
    public void ValueObject_MultipleStructures_AllGenerated()
    {
        var schema = CreateSchemaWithMultipleValueObjects();
        var results = GenerateAllLanguages(schema);

        foreach (var (lang, result) in results)
        {
            Assert.True(result.Success, $"{lang} 生成失败：{string.Join(", ", result.Errors)}");

            var content = GetAllFileContent(result);
            Assert.Contains("Point", content);
            Assert.Contains("Money", content);
            Assert.Contains("EmailAddress", content);

            _output.WriteLine($"  {lang}: 3 个值对象均生成成功");
        }
    }

    [Fact]
    public void ValueObject_TypeMapping_ConsistentWithClass()
    {
        var schema = CreateSchemaWithValueObject();
        var results = GenerateAllLanguages(schema);

        var csharpContent = GetAllFileContent(results["csharp"]);
        var tsContent = GetAllFileContent(results["typescript"]);
        var javaContent = GetAllFileContent(results["java"]);

        Assert.Contains("float", csharpContent);
        Assert.Contains("number", tsContent);
        Assert.Contains("float", javaContent);

        _output.WriteLine("  值对象类型映射与类一致: f32 → C#:float, TS:number, Java:float");
    }

    #endregion

    #region Schema 测试用例工厂

    private static SchemaIR CreateSchemaWithAllPrimitiveTypes()
    {
        var fields = new List<FieldDefinition>
        {
            new("Int8Field", PrimitiveType.I8),
            new("Int16Field", PrimitiveType.I16),
            new("Int32Field", PrimitiveType.I32),
            new("Int64Field", PrimitiveType.I64),
            new("UInt8Field", PrimitiveType.U8),
            new("UInt16Field", PrimitiveType.U16),
            new("UInt32Field", PrimitiveType.U32),
            new("UInt64Field", PrimitiveType.U64),
            new("Float32Field", PrimitiveType.F32),
            new("Float64Field", PrimitiveType.F64),
            new("BoolField", PrimitiveType.Bool),
            new("StringField", PrimitiveType.Utf8),
            new("UuidField", PrimitiveType.Uuid)
        };

        return new SchemaIR("TestAllPrimitives",
            classes: [new ClassDefinition("AllPrimitives", fields, [])]);
    }

    private static SchemaIR CreateSchemaWithOptionalFields()
    {
        var fields = new List<FieldDefinition>
        {
            new("Id", PrimitiveType.I32),
            new("Name", PrimitiveType.Utf8, isOptional: true),
            new("Age", PrimitiveType.I32, isOptional: true),
            new("Email", PrimitiveType.Utf8, isOptional: true)
        };

        return new SchemaIR("TestOptional",
            classes: [new ClassDefinition("OptionalEntity", fields, [])]);
    }

    private static SchemaIR CreateSchemaWithListFields()
    {
        var fields = new List<FieldDefinition>
        {
            new("Id", PrimitiveType.I32),
            new("Tags", new ListType(PrimitiveType.Utf8)),
            new("Scores", new ListType(PrimitiveType.F64))
        };

        return new SchemaIR("TestList",
            classes: [new ClassDefinition("ListEntity", fields, [])]);
    }

    private static SchemaIR CreateSchemaWithDictFields()
    {
        var fields = new List<FieldDefinition>
        {
            new("Id", PrimitiveType.I32),
            new("Metadata", new DictType(PrimitiveType.Utf8, PrimitiveType.Utf8))
        };

        return new SchemaIR("TestDict",
            classes: [new ClassDefinition("DictEntity", fields, [])]);
    }

    private static SchemaIR CreateSchemaWithSingleField(string typeName)
    {
        var fieldType = PrimitiveType.FromName(typeName) ?? PrimitiveType.I32;
        var fields = new List<FieldDefinition>
        {
            new("Value", fieldType)
        };

        return new SchemaIR("TestSingleField",
            classes: [new ClassDefinition("SingleFieldEntity", fields, [])]);
    }

    private static SchemaIR CreateTypicalEntitySchema()
    {
        var fields = new List<FieldDefinition>
        {
            new("Id", PrimitiveType.I32, attributes: [new AttributeDefinition("key")]),
            new("Name", PrimitiveType.Utf8),
            new("Email", PrimitiveType.Utf8, isOptional: true),
            new("Age", PrimitiveType.I32, isOptional: true),
            new("IsActive", PrimitiveType.Bool),
            new("Score", PrimitiveType.F64)
        };

        return new SchemaIR("TestEntity",
            classes: [new ClassDefinition("User", fields, [])]);
    }

    private static SchemaIR CreateSchemaWithEnum()
    {
        var enumValues = new List<EnumMember>
        {
            new("Active", 0),
            new("Inactive", 1),
            new("Suspended", 2),
            new("Deleted", 3)
        };

        return new SchemaIR("TestEnum",
            enums: [new EnumDefinition("UserStatus", enumValues, [])]);
    }

    private static SchemaIR CreateSchemaWithFlags()
    {
        var flagValues = new List<EnumMember>
        {
            new("Read", 1),
            new("Write", 2),
            new("Execute", 4),
            new("Admin", 8)
        };

        return new SchemaIR("TestFlags",
            flags: [new FlagsDefinition("Permission", flagValues, [])]);
    }

    private static SchemaIR CreateSchemaWithValueObject()
    {
        var fields = new List<FieldDefinition>
        {
            new("X", PrimitiveType.F32),
            new("Y", PrimitiveType.F32),
            new("Z", PrimitiveType.F32)
        };

        return new SchemaIR("TestValueObject",
            structures: [new StructureDefinition("Point", fields, [])]);
    }

    private static SchemaIR CreateSchemaWithValidatedValueObject()
    {
        var fields = new List<FieldDefinition>
        {
            new("Amount", PrimitiveType.I32,
                attributes:
                [
                    new AttributeDefinition("min", [new KeyValuePair<string, string>("value", "0")]),
                    new AttributeDefinition("max", [new KeyValuePair<string, string>("value", "1000000")])
                ]),
            new("Currency", PrimitiveType.Utf8,
                attributes: [new AttributeDefinition("len", [new KeyValuePair<string, string>("value", "3")])])
        };

        return new SchemaIR("TestValidatedValueObject",
            structures: [new StructureDefinition("Money", fields, [])]);
    }

    private static SchemaIR CreateSchemaWithMultipleValueObjects()
    {
        var pointFields = new List<FieldDefinition>
        {
            new("X", PrimitiveType.F64),
            new("Y", PrimitiveType.F64)
        };

        var moneyFields = new List<FieldDefinition>
        {
            new("Amount", PrimitiveType.I64),
            new("Currency", PrimitiveType.Utf8)
        };

        var emailFields = new List<FieldDefinition>
        {
            new("Address", PrimitiveType.Utf8),
            new("IsVerified", PrimitiveType.Bool)
        };

        return new SchemaIR("TestMultipleValueObjects",
            structures:
            [
                new StructureDefinition("Point", pointFields, []),
                new StructureDefinition("Money", moneyFields, []),
                new StructureDefinition("EmailAddress", emailFields, [])
            ]);
    }

    #endregion

    #region 辅助方法

    private static Dictionary<string, GeneratorResult> GenerateAllLanguages(SchemaIR schema)
    {
        var context = new GeneratorContext
        {
            Schema = schema,
            SchemaPath = "test.hms",
            OutputPath = "./output",
            Options = new Dictionary<string, object>
            {
                ["namespace"] = "Test.Models",
                ["targets"] = "record"
            }
        };

        return new Dictionary<string, GeneratorResult>
        {
            ["csharp"] = new CSharpGenerator().Generate(context),
            ["typescript"] = new TypeScriptGenerator().Generate(context),
            ["java"] = new JavaGenerator().Generate(context)
        };
    }

    private static string GetMainFileContent(GeneratorResult result)
    {
        if (result.Files.Count == 0) return "";

        var mainFile = result.Files.FirstOrDefault(f =>
            !f.Path.Contains("Repository") &&
            !f.Path.Contains("DbContext") &&
            !f.Path.Contains("Controller") &&
            !f.Path.Contains("Client") &&
            !f.Path.Contains("index")) ?? result.Files[0];

        return mainFile.Content;
    }

    private static string GetAllFileContent(GeneratorResult result)
    {
        return string.Join("\n", result.Files.Select(f => f.Content));
    }

    #endregion
}