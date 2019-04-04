using Hermes.Generator;
using Hermes.Migration;
using Xunit;

namespace Hermes.Tests.Incremental;

public sealed class ChangedTypeExtractorTests
{
    private readonly ChangedTypeExtractor _extractor = new();

    [Fact]
    public void Extract_无差异_返回空集合()
    {
        var schema = CreateTestSchema();
        var differ = new SchemaDiffer();
        var diff = differ.Diff(schema, schema);

        var (changed, removed) = _extractor.Extract(diff);

        Assert.Empty(changed);
        Assert.Empty(removed);
    }

    [Fact]
    public void Extract_新增Class_标记为变更()
    {
        var source = new SchemaIR("Test", classes: []);
        var target = new SchemaIR("Test", classes: [new ClassDefinition("User", [], [])]);

        var differ = new SchemaDiffer();
        var diff = differ.Diff(source, target);

        var (changed, removed) = _extractor.Extract(diff);

        Assert.Contains("User", changed);
        Assert.Empty(removed);
    }

    [Fact]
    public void Extract_删除Class_标记为移除()
    {
        var source = new SchemaIR("Test", classes: [new ClassDefinition("User", [], [])]);
        var target = new SchemaIR("Test", classes: []);

        var differ = new SchemaDiffer();
        var diff = differ.Diff(source, target);

        var (changed, removed) = _extractor.Extract(diff);

        Assert.Empty(changed);
        Assert.Contains("User", removed);
    }

    [Fact]
    public void Extract_修改Class字段_标记为变更()
    {
        var source = new SchemaIR("Test", classes:
        [
            new ClassDefinition("User", [new FieldDefinition("name", PrimitiveType.Utf8)], [])
        ]);
        var target = new SchemaIR("Test", classes:
        [
            new ClassDefinition("User",
                [new FieldDefinition("name", PrimitiveType.Utf8), new FieldDefinition("age", PrimitiveType.I32)], [])
        ]);

        var differ = new SchemaDiffer();
        var diff = differ.Diff(source, target);

        var (changed, removed) = _extractor.Extract(diff);

        Assert.Contains("User", changed);
        Assert.Empty(removed);
    }

    [Fact]
    public void Extract_新增Enum_标记为变更()
    {
        var source = new SchemaIR("Test", enums: []);
        var target = new SchemaIR("Test", enums: [new EnumDefinition("Status", [new EnumMember("Active", 0)])]);

        var differ = new SchemaDiffer();
        var diff = differ.Diff(source, target);

        var (changed, removed) = _extractor.Extract(diff);

        Assert.Contains("Status", changed);
        Assert.Empty(removed);
    }

    [Fact]
    public void Extract_新增Flags_标记为变更()
    {
        var source = new SchemaIR("Test", flags: []);
        var target = new SchemaIR("Test", flags: [new FlagsDefinition("Permissions", [new EnumMember("Read", 1)])]);

        var differ = new SchemaDiffer();
        var diff = differ.Diff(source, target);

        var (changed, removed) = _extractor.Extract(diff);

        Assert.Contains("Permissions", changed);
        Assert.Empty(removed);
    }

    [Fact]
    public void Extract_新增Union_标记为变更()
    {
        var source = new SchemaIR("Test", unions: []);
        var target = new SchemaIR("Test", unions: [new UnionDefinition("Result", [])]);

        var differ = new SchemaDiffer();
        var diff = differ.Diff(source, target);

        var (changed, removed) = _extractor.Extract(diff);

        Assert.Contains("Result", changed);
        Assert.Empty(removed);
    }

    [Fact]
    public void Extract_混合变更_正确分类()
    {
        var source = new SchemaIR("Test",
            classes: [new ClassDefinition("User", [], [])],
            enums: [new EnumDefinition("Status", [new EnumMember("Active", 0)])]
        );
        var target = new SchemaIR("Test",
            classes:
            [
                new ClassDefinition("User", [new FieldDefinition("name", PrimitiveType.Utf8)], []),
                new ClassDefinition("Admin", [], [])
            ],
            flags: [new FlagsDefinition("Perms", [new EnumMember("Read", 1)])]
        );

        var differ = new SchemaDiffer();
        var diff = differ.Diff(source, target);

        var (changed, removed) = _extractor.Extract(diff);

        Assert.Contains("User", changed);
        Assert.Contains("Admin", changed);
        Assert.Contains("Perms", changed);
        Assert.Contains("Status", removed);
    }

    private static SchemaIR CreateTestSchema()
    {
        return new SchemaIR("Test",
            classes: [new ClassDefinition("User", [new FieldDefinition("name", PrimitiveType.Utf8)], [])],
            enums: [new EnumDefinition("Status", [new EnumMember("Active", 0)])]
        );
    }
}

public sealed class GeneratorDispatcherIncrementalTests
{
    [Fact]
    public void DispatchIncremental_无差异_返回空结果()
    {
        var schema = new SchemaIR("Test", classes: [new ClassDefinition("User", [], [])]);
        var dispatcher = new GeneratorDispatcher();
        dispatcher.Register(new TypeScriptGenerator());

        var configs = new Dictionary<string, Dictionary<string, object>>
        {
            ["typescript"] = new() { ["enabled"] = true, ["output"] = "/tmp/test" }
        };

        var result = dispatcher.DispatchIncremental(schema, schema, "/tmp/test", configs);

        Assert.Equal(0, result.ChangedTypeCount);
        Assert.Equal(0, result.RemovedTypeCount);
        Assert.Empty(result.Files);
    }

    [Fact]
    public void DispatchIncremental_新增类型_只生成新增文件()
    {
        var previousSchema = new SchemaIR("Test", classes: []);
        var newSchema = new SchemaIR("Test",
            classes: [new ClassDefinition("User", [new FieldDefinition("name", PrimitiveType.Utf8)], [])],
            enums: [new EnumDefinition("Status", [new EnumMember("Active", 0)])]
        );

        var dispatcher = new GeneratorDispatcher();
        dispatcher.Register(new TypeScriptGenerator());

        var configs = new Dictionary<string, Dictionary<string, object>>
        {
            ["typescript"] = new() { ["enabled"] = true, ["output"] = "/tmp/test" }
        };

        var result = dispatcher.DispatchIncremental(newSchema, previousSchema, "/tmp/test", configs);

        Assert.Equal(2, result.ChangedTypeCount);
        Assert.Equal(0, result.RemovedTypeCount);
        Assert.Equal(2, result.Files.Count);

        var typeNames = result.Files.Select(f => f.TypeName).ToList();
        Assert.Contains("User", typeNames);
        Assert.Contains("Status", typeNames);
    }

    [Fact]
    public void DispatchIncremental_删除类型_生成删除标记()
    {
        var previousSchema = new SchemaIR("Test",
            classes: [new ClassDefinition("User", [], [])],
            enums: [new EnumDefinition("Status", [new EnumMember("Active", 0)])]
        );
        var newSchema = new SchemaIR("Test", classes: []);

        var dispatcher = new GeneratorDispatcher();
        dispatcher.Register(new TypeScriptGenerator());

        var configs = new Dictionary<string, Dictionary<string, object>>
        {
            ["typescript"] = new() { ["enabled"] = true, ["output"] = "/tmp/test" }
        };

        var result = dispatcher.DispatchIncremental(newSchema, previousSchema, "/tmp/test", configs);

        Assert.Equal(0, result.ChangedTypeCount);
        Assert.Equal(2, result.RemovedTypeCount);
        Assert.NotEmpty(result.DeletedFiles);

        foreach (var file in result.DeletedFiles) Assert.Equal(FileChangeKind.Deleted, file.ChangeKind);
    }

    [Fact]
    public void DispatchIncremental_修改类型_只重新生成变更类型()
    {
        var previousSchema = new SchemaIR("Test",
            classes:
            [
                new ClassDefinition("User", [new FieldDefinition("name", PrimitiveType.Utf8)], []),
                new ClassDefinition("Product", [new FieldDefinition("title", PrimitiveType.Utf8)], [])
            ]
        );
        var newSchema = new SchemaIR("Test",
            classes:
            [
                new ClassDefinition("User",
                    [new FieldDefinition("name", PrimitiveType.Utf8), new FieldDefinition("email", PrimitiveType.Utf8)],
                    []),
                new ClassDefinition("Product", [new FieldDefinition("title", PrimitiveType.Utf8)], [])
            ]
        );

        var dispatcher = new GeneratorDispatcher();
        dispatcher.Register(new TypeScriptGenerator());

        var configs = new Dictionary<string, Dictionary<string, object>>
        {
            ["typescript"] = new() { ["enabled"] = true, ["output"] = "/tmp/test" }
        };

        var result = dispatcher.DispatchIncremental(newSchema, previousSchema, "/tmp/test", configs);

        Assert.Equal(1, result.ChangedTypeCount);
        Assert.Equal(1, result.Files.Count);
        Assert.Equal("User", result.Files[0].TypeName);
        Assert.Equal(FileChangeKind.Added, result.Files[0].ChangeKind);
    }

    [Fact]
    public void DispatchIncremental_增量生成器_只生成变更类型()
    {
        var previousSchema = new SchemaIR("Test",
            classes: [new ClassDefinition("User", [new FieldDefinition("name", PrimitiveType.Utf8)], [])],
            enums: [new EnumDefinition("Status", [new EnumMember("Active", 0)])]
        );
        var newSchema = new SchemaIR("Test",
            classes:
            [
                new ClassDefinition("User", [new FieldDefinition("name", PrimitiveType.Utf8)], []),
                new ClassDefinition("Admin", [new FieldDefinition("role", PrimitiveType.Utf8)], [])
            ],
            enums: [new EnumDefinition("Status", [new EnumMember("Active", 0)])]
        );

        var dispatcher = new GeneratorDispatcher();
        dispatcher.Register(new TypeScriptGenerator());

        var configs = new Dictionary<string, Dictionary<string, object>>
        {
            ["typescript"] = new() { ["enabled"] = true, ["output"] = "/tmp/test" }
        };

        var result = dispatcher.DispatchIncremental(newSchema, previousSchema, "/tmp/test", configs);

        Assert.Equal(1, result.ChangedTypeCount);
        Assert.Equal(1, result.Files.Count);
        Assert.Equal("Admin", result.Files[0].TypeName);
    }
}

public sealed class SchemaSnapshotTests
{
    [Fact]
    public void Save_创建快照目录和文件()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"hermes_test_{Guid.NewGuid():N}");
        try
        {
            var schema = new SchemaIR("Test",
                classes: [new ClassDefinition("User", [new FieldDefinition("name", PrimitiveType.Utf8)], [])]
            );

            var snapshot = SchemaSnapshot.Save(schema, "", tempDir);

            Assert.NotNull(snapshot);
            Assert.Equal("Test", snapshot.Namespace);
            Assert.Contains("User", snapshot.TypeNames);
            Assert.True(Directory.Exists(Path.Combine(tempDir, ".hermes")));
            Assert.True(File.Exists(Path.Combine(tempDir, ".hermes", "snapshot.json")));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void TryLoad_无快照_返回null()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"hermes_test_{Guid.NewGuid():N}");
        try
        {
            var snapshot = SchemaSnapshot.TryLoad(tempDir);
            Assert.Null(snapshot);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void TryLoad_有快照_正确加载()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"hermes_test_{Guid.NewGuid():N}");
        try
        {
            var schema = new SchemaIR("TestNamespace",
                classes: [new ClassDefinition("User", [], [])],
                enums: [new EnumDefinition("Status", [new EnumMember("Active", 0)])]
            );

            SchemaSnapshot.Save(schema, "", tempDir);
            var loaded = SchemaSnapshot.TryLoad(tempDir);

            Assert.NotNull(loaded);
            Assert.Equal("TestNamespace", loaded.Namespace);
            Assert.Equal(2, loaded.TypeNames.Count);
            Assert.Contains("User", loaded.TypeNames);
            Assert.Contains("Status", loaded.TypeNames);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Save_有源文件_复制源文件()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"hermes_test_{Guid.NewGuid():N}");
        var schemaFile = Path.Combine(tempDir, "schema.her");
        try
        {
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(schemaFile, "namespace Test;\n\nclass User {\n  name: utf8;\n}\n");

            var schema = new SchemaIR("Test",
                classes: [new ClassDefinition("User", [new FieldDefinition("name", PrimitiveType.Utf8)], [])]
            );

            SchemaSnapshot.Save(schema, schemaFile, tempDir);

            var snapshotCopy = Path.Combine(tempDir, ".hermes", "schema.her");
            Assert.True(File.Exists(snapshotCopy));
            Assert.Equal(File.ReadAllText(schemaFile), File.ReadAllText(snapshotCopy));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}

public sealed class FileChangeKindTests
{
    [Fact]
    public void GeneratedFile_默认值_为Unchanged()
    {
        var file = new GeneratedFile();
        Assert.Equal(FileChangeKind.Unchanged, file.ChangeKind);
        Assert.Equal("", file.TypeName);
    }

    [Fact]
    public void GeneratedFile_设置变更类型()
    {
        var file = new GeneratedFile
        {
            Path = "/tmp/test.ts",
            Content = "export interface User {}",
            Generator = "typescript",
            TypeName = "User",
            ChangeKind = FileChangeKind.Added
        };

        Assert.Equal(FileChangeKind.Added, file.ChangeKind);
        Assert.Equal("User", file.TypeName);
    }
}