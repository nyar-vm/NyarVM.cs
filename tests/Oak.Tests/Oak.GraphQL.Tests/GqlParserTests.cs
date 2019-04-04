namespace Oak.GraphQL.Tests;

public class GqlParserTests : GqlTestBase
{
    #region Type 定义测试

    [Fact]
    public void Parse_TypeDefinition_ShouldReturnTypeDef()
    {
        var source = @"
            type User {
                name: String
                age: Int
            }";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Single(result.Value.TypeDefinitions);
        Assert.Equal("User", result.Value.TypeDefinitions[0].Name);
        Assert.Equal("type", result.Value.TypeDefinitions[0].Kind);
        Assert.Equal(2, result.Value.TypeDefinitions[0].fields.Count);
    }

    [Fact]
    public void Parse_TypeWithImplements_ShouldCaptureImplements()
    {
        var source = @"
            type Admin implements User {
                role: String
            }";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Single(result.Value.TypeDefinitions);
        Assert.NotEmpty(result.Value.TypeDefinitions[0].Implements);
    }

    [Fact]
    public void Parse_TypeWithArguments_ShouldCaptureArguments()
    {
        var source = @"
            type Query {
                user(id: Int): User
            }";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        var field = result.Value.TypeDefinitions[0].fields[0];
        Assert.Equal("user", field.Name);
        Assert.Single(field.Arguments);
    }

    #endregion

    #region Input 定义测试

    [Fact]
    public void Parse_InputDefinition_ShouldReturnInputDef()
    {
        var source = @"
            input CreateUserInput {
                name: String!
                email: String!
            }";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Single(result.Value.TypeDefinitions);
        Assert.Equal("CreateUserInput", result.Value.TypeDefinitions[0].Name);
        Assert.Equal("input", result.Value.TypeDefinitions[0].Kind);
    }

    #endregion

    #region Interface 定义测试

    [Fact]
    public void Parse_InterfaceDefinition_ShouldReturnInterfaceDef()
    {
        var source = @"
            interface Node {
                id: ID!
            }";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Single(result.Value.TypeDefinitions);
        Assert.Equal("Node", result.Value.TypeDefinitions[0].Name);
        Assert.Equal("interface", result.Value.TypeDefinitions[0].Kind);
    }

    #endregion

    #region Enum 定义测试

    [Fact]
    public void Parse_EnumDefinition_ShouldReturnEnumDef()
    {
        var source = @"
            enum Status {
                ACTIVE
                INACTIVE
                PENDING
            }";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Single(result.Value.TypeDefinitions);
        Assert.Equal("Status", result.Value.TypeDefinitions[0].Name);
        Assert.Equal("enum", result.Value.TypeDefinitions[0].Kind);
    }

    #endregion

    #region Union 定义测试

    [Fact]
    public void Parse_UnionDefinition_ShouldReturnUnionDef()
    {
        var source = "union SearchResult = User | Post | Comment";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Single(result.Value.TypeDefinitions);
        Assert.Equal("SearchResult", result.Value.TypeDefinitions[0].Name);
        Assert.Equal("union", result.Value.TypeDefinitions[0].Kind);
    }

    #endregion

    #region Scalar 定义测试

    [Fact]
    public void Parse_ScalarDefinition_ShouldReturnScalarDef()
    {
        var source = "scalar DateTime";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Single(result.Value.TypeDefinitions);
        Assert.Equal("DateTime", result.Value.TypeDefinitions[0].Name);
        Assert.Equal("scalar", result.Value.TypeDefinitions[0].Kind);
    }

    #endregion

    #region Directive 定义测试

    [Fact]
    public void Parse_DirectiveDefinition_ShouldReturnDirectiveDef()
    {
        var source = "directive @auth(requires: String) on FIELD_DEFINITION";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Single(result.Value.DirectiveDefinitions);
        Assert.Equal("auth", result.Value.DirectiveDefinitions[0].Name);
    }

    [Fact]
    public void Parse_DirectiveDefinitionRepeatable_ShouldSetFlag()
    {
        var source = "directive @deprecated(reason: String) repeatable on FIELD_DEFINITION | ENUM_VALUE";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Single(result.Value.DirectiveDefinitions);
        Assert.True(result.Value.DirectiveDefinitions[0].Repeatable);
    }

    #endregion

    #region 类型引用测试

    [Fact]
    public void Parse_NonNullType_ShouldReturnNonNullTypeRef()
    {
        var source = "type Query { name: String! }";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        var field = result.Value.TypeDefinitions[0].fields[0];
        Assert.IsType<GqlNonNullType>(field.type);
    }

    [Fact]
    public void Parse_ListType_ShouldReturnListTypeRef()
    {
        var source = "type Query { tags: [String] }";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        var field = result.Value.TypeDefinitions[0].fields[0];
        Assert.IsType<GqlListType>(field.type);
    }

    #endregion

    #region 复合 Schema 测试

    [Fact]
    public void Parse_FullSchema_ShouldParseAllDefinitions()
    {
        var source = @"
            type Query {
                user(id: Int!): User
                posts: [Post]
            }

            type User {
                id: Int!
                name: String!
                email: String
            }

            type Post {
                id: Int!
                title: String!
                author: User
            }

            enum Role {
                ADMIN
                USER
            }

            scalar DateTime

            directive @auth on FIELD_DEFINITION
        ";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Equal(5, result.Value.TypeDefinitions.Count);
        Assert.Single(result.Value.DirectiveDefinitions);
    }

    #endregion
}
