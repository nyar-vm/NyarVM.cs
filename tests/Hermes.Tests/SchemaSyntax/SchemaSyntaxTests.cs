using Hermes.Compiler;
using Xunit;

namespace Hermes.Tests.SchemaSyntax;

public class PrimaryKeySyntaxTests
{
    private static CompilationResult CompileSource(string source)
    {
        var compiler = new HermesCompiler();
        return compiler.CompileSource(source);
    }

    #region Parser 测试 - @@ 和 @ 同时使用

    [Fact]
    public void Parse_StorageModel_PrimaryAndUniqueKey_BothWork()
    {
        var source = @"
storage TestDb {
    model User {
        @@user_id: uuid,
        @auth_id: uuid,
        name: utf8,
    }
}";
        var result = CompileSource(source);

        Assert.True(result.Success);
        Assert.NotNull(result.Schema);

        var storage = Assert.Single(result.Schema.Storages);
        var model = Assert.Single(storage.Models);

        var keyField = model.fields.First(f => f.Name == "user_id");
        var uniqueField = model.fields.First(f => f.Name == "auth_id");
        var normalField = model.fields.First(f => f.Name == "name");

        Assert.True(keyField.IsPrimaryKey);
        Assert.False(keyField.IsUniqueKey);

        Assert.False(uniqueField.IsPrimaryKey);
        Assert.True(uniqueField.IsUniqueKey);

        Assert.False(normalField.IsPrimaryKey);
        Assert.False(normalField.IsUniqueKey);
    }

    #endregion

    #region Parser 测试 - 向后兼容 [key(...)] 属性

    [Fact]
    public void Parse_StorageModel_KeyAttribute_StillWorks()
    {
        var source = @"
storage TestDb {
    [key(user_id)]
    model User {
        user_id: uuid,
        name: utf8,
    }
}";
        var result = CompileSource(source);

        Assert.True(result.Success);
        Assert.NotNull(result.Schema);

        var storage = Assert.Single(result.Schema.Storages);
        var model = Assert.Single(storage.Models);

        Assert.Equal("uuid", model.KeyType.TypeName);
    }

    #endregion

    #region Lexer 测试

    [Fact]
    public void Lexer_SchemaMode_AtAtIdentifier_ProducesPrimaryKeyToken()
    {
        var source = "storage Test { model M { @@id: uuid, } }";
        var result = CompileSource(source);

        Assert.True(result.Success, result.Diagnostics.HasErrors
            ? string.Join(", ", result.Diagnostics.Diagnostics.Select(d => d.Message))
            : "编译应成功");
    }

    [Fact]
    public void Lexer_SchemaMode_AtIdentifier_ProducesUniqueKeyToken()
    {
        var source = "storage Test { model M { @name: utf8, } }";
        var result = CompileSource(source);

        Assert.True(result.Success, result.Diagnostics.HasErrors
            ? string.Join(", ", result.Diagnostics.Diagnostics.Select(d => d.Message))
            : "编译应成功");
    }

    #endregion

    #region Parser 测试 - @@ 主键语法

    [Fact]
    public void Parse_StorageModel_PrimaryKeyField_CreatesKeyAttribute()
    {
        var source = @"
storage TestDb {
    model User {
        @@user_id: uuid,
        name: utf8,
    }
}";
        var result = CompileSource(source);

        Assert.True(result.Success);
        Assert.NotNull(result.Schema);

        var storage = Assert.Single(result.Schema.Storages);
        var model = Assert.Single(storage.Models);
        var keyField = model.fields.FirstOrDefault(f => f.Name == "user_id");

        Assert.NotNull(keyField);
        Assert.True(keyField.IsPrimaryKey);
    }

    [Fact]
    public void Parse_Class_PrimaryKeyField_CreatesKeyAttribute()
    {
        var source = @"
class User {
    @@user_id: uuid,
    name: utf8,
}";
        var result = CompileSource(source);

        Assert.True(result.Success, result.Diagnostics.HasErrors
            ? string.Join(", ", result.Diagnostics.Diagnostics.Select(d => d.Message))
            : "编译应成功");
        Assert.NotNull(result.Schema);

        var cls = Assert.Single(result.Schema.Classes);
        var keyField = cls.fields.FirstOrDefault(f => f.Name == "user_id");

        Assert.NotNull(keyField);
        Assert.True(keyField.IsPrimaryKey);
    }

    [Fact]
    public void Parse_StorageModel_PrimaryKeyField_SetsKeyType()
    {
        var source = @"
storage TestDb {
    model User {
        @@user_id: uuid,
        name: utf8,
    }
}";
        var result = CompileSource(source);

        Assert.True(result.Success);
        Assert.NotNull(result.Schema);

        var storage = Assert.Single(result.Schema.Storages);
        var model = Assert.Single(storage.Models);

        Assert.IsType<PrimitiveType>(model.KeyType);
        Assert.Equal("uuid", model.KeyType.TypeName);
    }

    #endregion

    #region Parser 测试 - @ 唯一键语法

    [Fact]
    public void Parse_StorageModel_UniqueKeyField_CreatesUniqueAttribute()
    {
        var source = @"
storage TestDb {
    model User {
        @@user_id: uuid,
        @email: utf8,
    }
}";
        var result = CompileSource(source);

        Assert.True(result.Success);
        Assert.NotNull(result.Schema);

        var storage = Assert.Single(result.Schema.Storages);
        var model = Assert.Single(storage.Models);
        var uniqueField = model.fields.FirstOrDefault(f => f.Name == "email");

        Assert.NotNull(uniqueField);
        Assert.True(uniqueField.IsUniqueKey);
    }

    [Fact]
    public void Parse_Class_UniqueKeyField_CreatesUniqueAttribute()
    {
        var source = @"
class User {
    @@user_id: uuid,
    @auth_id: uuid,
}";
        var result = CompileSource(source);

        Assert.True(result.Success, result.Diagnostics.HasErrors
            ? string.Join(", ", result.Diagnostics.Diagnostics.Select(d => d.Message))
            : "编译应成功");
        Assert.NotNull(result.Schema);

        var cls = Assert.Single(result.Schema.Classes);
        var uniqueField = cls.fields.FirstOrDefault(f => f.Name == "auth_id");

        Assert.NotNull(uniqueField);
        Assert.True(uniqueField.IsUniqueKey);
    }

    #endregion
}

public class ArtGPTSchemaTests
{
    private static CompilationResult CompileSource(string source)
    {
        var compiler = new HermesCompiler();
        return compiler.CompileSource(source);
    }

    [Fact]
    public void Parse_ArtGPTSchema_FullSchema_CompilesSuccessfully()
    {
        var source = @"
[database(main=""gpt_art"", test=""gpt_art_test"")]

enums ArtworkType {
    Image = 0,
    Comic = 1,
    Animation = 2,
    Model3D = 3,
}

enums ArtworkStatus {
    Draft = 0,
    Published = 1,
    Hidden = 2,
    Archived = 3,
    Deleted = 4,
}

enums SourceType {
    Original = 0,
    Redraw = 1,
    Variation = 2,
    Img2Img = 3,
}

enums TaskStatus {
    Pending = 0,
    Queued = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5,
}

storage gpt_art {
    model User {
        @@user_id: uuid,
        @auth_id: uuid,
        display_name: utf8,
        avatar_url: utf8?,
        bio: utf8?,
        created_at: datetime,
    }

    model Tag {
        @@tag_id: uuid,
        name: utf8,
    }

    model Artwork {
        @@artwork_id: uuid,
        title: utf8,
        artwork_type: ArtworkType,
        description: utf8?,
        prompt: utf8?,
        negative_prompt: utf8?,
        model_name: utf8?,
        seed: i64?,
        steps: i32?,
        cfg_scale: f64?,
        width: i32,
        height: i32,
        image_url: utf8,
        thumbnail_url: utf8?,
        source_artwork: option<&Artwork>,
        source_type: SourceType?,
        author: &User,
        is_public: bool,
        status: ArtworkStatus,
        like_count: i64,
        view_count: i64,
        redraw_count: i64,
        collection_count: i64,
        tags: [&Tag],
        created_at: datetime,
        updated_at: datetime,
    }

    model Collection {
        @@collection_id: uuid,
        name: utf8,
        description: utf8?,
        owner: &User,
        is_public: bool,
        item_count: i64,
        created_at: datetime,
        updated_at: datetime,
    }

    model Like {
        @@like_id: uuid,
        user: &User,
        artwork: &Artwork,
        created_at: datetime,
    }

    model Comment {
        @@comment_id: uuid,
        artwork: &Artwork,
        author: &User,
        content: utf8,
        parent: option<&Comment>,
        created_at: datetime,
    }

    model GenerateTask {
        @@task_id: uuid,
        requester: &User,
        prompt: utf8,
        negative_prompt: utf8?,
        model_name: utf8,
        width: i32,
        height: i32,
        steps: i32?,
        cfg_scale: f64?,
        seed: i64?,
        batch_size: i32,
        status: TaskStatus,
        result_artwork_ids: list<uuid>,
        error_message: utf8?,
        delegated_token_id: utf8?,
        created_at: datetime,
        completed_at: datetime?,
    }

    model Follow {
        @@follow_id: uuid,
        follower: &User,
        following: &User,
        created_at: datetime,
    }
}";
        var result = CompileSource(source);

        Assert.True(result.Success, result.Diagnostics.HasErrors
            ? string.Join(", ", result.Diagnostics.Diagnostics.Select(d => d.Message))
            : "编译应成功");
        Assert.NotNull(result.Schema);

        var storage = Assert.Single(result.Schema.Storages);
        Assert.Equal(8, storage.Models.Count);
        Assert.Equal(4, result.Schema.Enums.Count);
    }

    [Fact]
    public void Parse_ArtGPTSchema_EnumFields_UseNamedType()
    {
        var source = @"
enums ArtworkStatus {
    Draft = 0,
    Published = 1,
}

storage gpt_art {
    model Artwork {
        @@artwork_id: uuid,
        status: ArtworkStatus,
    }
}";
        var result = CompileSource(source);

        Assert.True(result.Success);
        Assert.NotNull(result.Schema);

        var storage = Assert.Single(result.Schema.Storages);
        var model = Assert.Single(storage.Models);
        var statusField = model.fields.First(f => f.Name == "status");

        Assert.IsType<NamedType>(statusField.FieldType);
        Assert.Equal("ArtworkStatus", statusField.FieldType.TypeName);
    }

    [Fact]
    public void Parse_ArtGPTSchema_OptionReference_UsesOptionType()
    {
        var source = @"
storage gpt_art {
    model Comment {
        @@comment_id: uuid,
        parent: option<&Comment>,
    }
}";
        var result = CompileSource(source);

        Assert.True(result.Success);
        Assert.NotNull(result.Schema);

        var storage = Assert.Single(result.Schema.Storages);
        var model = Assert.Single(storage.Models);
        var parentField = model.fields.First(f => f.Name == "parent");

        var optionType = Assert.IsType<OptionType>(parentField.FieldType);
        Assert.IsType<ReferenceType>(optionType.InnerType);
    }
}

public class NullableTypeSyntaxTests
{
    private static CompilationResult CompileSource(string source)
    {
        var compiler = new HermesCompiler();
        return compiler.CompileSource(source);
    }

    #region type? 和 option<T> 语义等价测试

    [Fact]
    public void Parse_StorageModel_NullableAndOption_ProduceSameIR()
    {
        var source = @"
storage TestDb {
    model Test {
        @@id: uuid,
        field_a: utf8?,
        field_b: option<utf8>,
    }
}";
        var result = CompileSource(source);

        Assert.True(result.Success);
        Assert.NotNull(result.Schema);

        var storage = Assert.Single(result.Schema.Storages);
        var model = Assert.Single(storage.Models);
        var fieldA = model.fields.First(f => f.Name == "field_a");
        var fieldB = model.fields.First(f => f.Name == "field_b");

        var optionA = Assert.IsType<OptionType>(fieldA.FieldType);
        var optionB = Assert.IsType<OptionType>(fieldB.FieldType);

        Assert.Equal(optionA.InnerType.TypeName, optionB.InnerType.TypeName);
    }

    #endregion

    #region 基础解析测试

    [Fact]
    public void Parse_Class_Basic_NoFields_Works()
    {
        var source = "class Empty { }";
        var result = CompileSource(source);

        var diagMessages = result.Diagnostics.Diagnostics.Select(d => $"[{d.Level}] {d.Code}: {d.Message}").ToList();
        Assert.True(result.Success, $"编译失败：{string.Join("; ", diagMessages)}");
        Assert.NotNull(result.Schema);
        Assert.NotEmpty(result.Schema.Classes);
    }

    [Fact]
    public void Parse_Class_Basic_OneField_Works()
    {
        var source = "class User { name: utf8 }";
        var result = CompileSource(source);

        var diagMessages = result.Diagnostics.Diagnostics.Select(d => $"[{d.Level}] {d.Code}: {d.Message}").ToList();
        Assert.True(result.Success, $"编译失败：{string.Join("; ", diagMessages)}");
        Assert.NotNull(result.Schema);
        Assert.NotEmpty(result.Schema.Classes);
    }

    [Fact]
    public void Parse_Class_OptionUtf8_BasicWorks()
    {
        var source = @"
class Project {
    name: utf8,
    description: option<utf8>,
}";
        var result = CompileSource(source);

        var diagMessages = result.Diagnostics.Diagnostics.Select(d => $"[{d.Level}] {d.Code}: {d.Message}").ToList();
        Assert.True(result.Success, $"编译失败：{string.Join("; ", diagMessages)}");
        Assert.NotNull(result.Schema);
        Assert.NotEmpty(result.Schema.Classes);
    }

    #endregion

    #region type? 语法测试

    [Fact]
    public void Parse_StorageModel_NullablePrimitive_ProducesOptionType()
    {
        var source = @"
storage TestDb {
    model Project {
        @@project_id: uuid,
        name: utf8,
        description: utf8?,
    }
}";
        var result = CompileSource(source);

        Assert.True(result.Success);
        Assert.NotNull(result.Schema);

        var storage = Assert.Single(result.Schema.Storages);
        var model = Assert.Single(storage.Models);
        var descField = model.fields.FirstOrDefault(f => f.Name == "description");

        Assert.NotNull(descField);
        Assert.IsType<OptionType>(descField.FieldType);
    }

    [Fact]
    public void Parse_StorageModel_NullablePrimitive_InnerTypeIsCorrect()
    {
        var source = @"
storage TestDb {
    model Project {
        @@project_id: uuid,
        name: utf8,
        description: utf8?,
    }
}";
        var result = CompileSource(source);

        Assert.True(result.Success);
        Assert.NotNull(result.Schema);

        var storage = Assert.Single(result.Schema.Storages);
        var model = Assert.Single(storage.Models);
        var descField = model.fields.First(f => f.Name == "description");

        var optionType = Assert.IsType<OptionType>(descField.FieldType);
        var innerType = Assert.IsType<PrimitiveType>(optionType.InnerType);
        Assert.Equal("utf8", innerType.TypeName);
    }

    [Fact]
    public void Parse_StorageModel_NullableUuid_ProducesOptionType()
    {
        var source = @"
storage TestDb {
    model Permission {
        @@permission_id: uuid,
        resource_id: uuid?,
        level: utf8,
    }
}";
        var result = CompileSource(source);

        Assert.True(result.Success);
        Assert.NotNull(result.Schema);

        var storage = Assert.Single(result.Schema.Storages);
        var model = Assert.Single(storage.Models);
        var field = model.fields.First(f => f.Name == "resource_id");

        Assert.IsType<OptionType>(field.FieldType);
    }

    #endregion

    #region option<T> 语法测试（保持兼容）

    [Fact]
    public void Parse_StorageModel_OptionUtf8_ProducesOptionType()
    {
        var source = @"
storage TestDb {
    model Project {
        @@project_id: uuid,
        name: utf8,
        description: option<utf8>,
    }
}";
        var result = CompileSource(source);

        Assert.True(result.Success);
        Assert.NotNull(result.Schema);

        var storage = Assert.Single(result.Schema.Storages);
        var model = Assert.Single(storage.Models);
        var descField = model.fields.First(f => f.Name == "description");

        Assert.IsType<OptionType>(descField.FieldType);
    }

    [Fact]
    public void Parse_StorageModel_OptionReference_ProducesOptionType()
    {
        var source = @"
storage TestDb {
    model Role {
        @@role_id: uuid,
        name: utf8,
        inherits_from: option<&Role>,
    }
}";
        var result = CompileSource(source);

        Assert.True(result.Success);
        Assert.NotNull(result.Schema);

        var storage = Assert.Single(result.Schema.Storages);
        var model = Assert.Single(storage.Models);
        var field = model.fields.First(f => f.Name == "inherits_from");

        var optionType = Assert.IsType<OptionType>(field.FieldType);
        Assert.IsType<ReferenceType>(optionType.InnerType);
    }

    #endregion
}

public class CompositeIndexSyntaxTests
{
    private static CompilationResult CompileSource(string source)
    {
        var compiler = new HermesCompiler();
        return compiler.CompileSource(source);
    }

    #region 联合索引测试

    [Fact]
    public void Parse_StorageModel_CompositeIndex_OnModelLevel()
    {
        var source = @"
storage TestDb {
    [index(project, name)]
    model Role {
        @@role_id: uuid,
        name: utf8,
    }
}";
        var result = CompileSource(source);

        Assert.True(result.Success, result.Diagnostics.HasErrors
            ? string.Join(", ", result.Diagnostics.Diagnostics.Select(d => d.Message))
            : "编译应成功");
        Assert.NotNull(result.Schema);

        var storage = Assert.Single(result.Schema.Storages);
        var model = Assert.Single(storage.Models);

        var indexAttr = model.Attributes.FirstOrDefault(a => a.Name == "index");
        Assert.NotNull(indexAttr);
        Assert.Equal(2, indexAttr.Arguments.Count);
    }

    [Fact]
    public void Parse_StorageModel_PrimaryKeyWithCompositeIndex()
    {
        var source = @"
storage TestDb {
    [index(owner, is_public)]
    model Project {
        @@project_id: uuid,
        name: utf8,
        is_public: bool,
    }
}";
        var result = CompileSource(source);

        Assert.True(result.Success, result.Diagnostics.HasErrors
            ? string.Join(", ", result.Diagnostics.Diagnostics.Select(d => d.Message))
            : "编译应成功");
        Assert.NotNull(result.Schema);

        var storage = Assert.Single(result.Schema.Storages);
        var model = Assert.Single(storage.Models);

        var keyField = model.fields.First(f => f.Name == "project_id");
        Assert.True(keyField.IsPrimaryKey);

        var indexAttr = model.Attributes.FirstOrDefault(a => a.Name == "index");
        Assert.NotNull(indexAttr);
    }

    #endregion
}