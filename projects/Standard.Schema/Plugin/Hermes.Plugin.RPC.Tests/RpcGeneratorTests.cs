using Hermes.Generator;

namespace Hermes.Plugin.RPC.Tests;

public sealed class RpcGeneratorTests
{
    private static readonly Dictionary<string, object> ClientOptions = new()
    {
        ["namespace"] = "Hermes.Generated",
        ["targets"] = "rpc-client"
    };

    private static readonly Dictionary<string, object> ServerOptions = new()
    {
        ["namespace"] = "Hermes.Generated",
        ["targets"] = "rpc-server"
    };

    [Fact]
    public void GenerateClientProxy_GET_生成异步方法()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("User",
        [
            new HttpEndpoint("getUser", [], PrimitiveType.Utf8, "GET", "/api/users")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Files.Count);
        var proxy = result.Files.First(f => f.TypeName == "UserRpcClient");
        Assert.Contains("public sealed class UserRpcClient", proxy.Content);
        Assert.Contains("public async Task<string> GetUserAsync", proxy.Content);
        Assert.Contains("_httpClient.GetAsync", proxy.Content);
    }

    [Fact]
    public void GenerateClientProxy_POST_生成JSONBody()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Order",
        [
            new HttpEndpoint("create", [new ParameterDefinition("order", PrimitiveType.Utf8)], PrimitiveType.Utf8,
                "POST", "/api/orders")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "OrderRpcClient");
        Assert.Contains("public async Task<string> CreateAsync", proxy.Content);
        Assert.Contains("JsonSerializer.Serialize(order", proxy.Content);
        Assert.Contains("_httpClient.PostAsync", proxy.Content);
        Assert.Contains("\"application/json\"", proxy.Content);
    }

    [Fact]
    public void GenerateClientProxy_PUT_DELETE_生成对应方法()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Item",
        [
            new HttpEndpoint("update", [new ParameterDefinition("item", PrimitiveType.Utf8)], PrimitiveType.Utf8, "PUT",
                "/api/items"),
            new HttpEndpoint("remove", [new ParameterDefinition("id", PrimitiveType.I32)], PrimitiveType.Utf8, "DELETE",
                "/api/items")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "ItemRpcClient");
        Assert.Contains("UpdateAsync", proxy.Content);
        Assert.Contains("_httpClient.PutAsync", proxy.Content);
        Assert.Contains("RemoveAsync", proxy.Content);
        Assert.Contains("_httpClient.DeleteAsync", proxy.Content);
    }

    [Fact]
    public void GenerateClientProxy_查询参数_生成QueryString()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Search",
        [
            new HttpEndpoint("search",
            [
                new ParameterDefinition("q", PrimitiveType.Utf8),
                new ParameterDefinition("page", PrimitiveType.I32)
            ], PrimitiveType.Utf8, "GET", "/api/search")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "SearchRpcClient");
        Assert.Contains("queryParams", proxy.Content);
        Assert.Contains("q={q}", proxy.Content);
        Assert.Contains("page={page}", proxy.Content);
        Assert.Contains("?\" + string.Join(\"&\", queryParams)", proxy.Content);
    }

    [Fact]
    public void GenerateClientProxy_返回基本类型_正确映射()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Stats",
        [
            new HttpEndpoint("count", [], PrimitiveType.I32, "GET", "/api/stats/count"),
            new HttpEndpoint("active", [], PrimitiveType.Bool, "GET", "/api/stats/active"),
            new HttpEndpoint("ratio", [], PrimitiveType.F64, "GET", "/api/stats/ratio")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "StatsRpcClient");
        Assert.Contains("Task<int>", proxy.Content);
        Assert.Contains("Task<bool>", proxy.Content);
        Assert.Contains("Task<double>", proxy.Content);
    }

    [Fact]
    public void GenerateClientProxy_返回复杂类型_JSON反序列化()
    {
        var generator = new RpcGenerator();
        var returnType = new NamedType("UserProfile");
        var serviceDef = new ServiceDefinition("Profile",
        [
            new HttpEndpoint("get", [new ParameterDefinition("id", PrimitiveType.I32)], returnType, "GET",
                "/api/profiles")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "ProfileRpcClient");
        Assert.Contains("Task<UserProfile>", proxy.Content);
        Assert.Contains("Deserialize<UserProfile>", proxy.Content);
    }

    [Fact]
    public void GenerateClientProxy_List返回_生成List类型()
    {
        var generator = new RpcGenerator();
        var returnType = new ListType(PrimitiveType.Utf8);
        var serviceDef = new ServiceDefinition("List",
        [
            new HttpEndpoint("all", [], returnType, "GET", "/api/items")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "ListRpcClient");
        Assert.Contains("Task<List<string>>", proxy.Content);
    }

    [Fact]
    public void GenerateServerInterface_生成接口定义()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("User",
        [
            new HttpEndpoint("getUser", [new ParameterDefinition("id", PrimitiveType.I32)], PrimitiveType.Utf8, "GET",
                "/api/users"),
            new HttpEndpoint("createUser", [new ParameterDefinition("name", PrimitiveType.Utf8)], PrimitiveType.Utf8,
                "POST", "/api/users")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ServerOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var iface = result.Files.First(f => f.TypeName == "IUserService");
        Assert.Contains("public interface IUserService", iface.Content);
        Assert.Contains("Task<string> GetUserAsync", iface.Content);
        Assert.Contains("Task<string> CreateUserAsync", iface.Content);
        Assert.Contains("int id", iface.Content);
        Assert.Contains("string name", iface.Content);
        Assert.Contains("CancellationToken cancellationToken", iface.Content);
    }

    [Fact]
    public void GenerateServerBase_生成抽象基类()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("User",
        [
            new HttpEndpoint("getUser", [new ParameterDefinition("id", PrimitiveType.I32)], PrimitiveType.Utf8, "GET",
                "/api/users")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ServerOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var baseClass = result.Files.First(f => f.TypeName == "UserServiceBase");
        Assert.Contains("public abstract class UserServiceBase : IUserService", baseClass.Content);
        Assert.Contains("public virtual Task<string> GetUserAsync", baseClass.Content);
        Assert.Contains("NotImplementedException", baseClass.Content);
    }

    [Fact]
    public void GenerateClientProxy_gRPC_生成gRPC集成点()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Calc",
        [
            new GrpcEndpoint("add",
                [new ParameterDefinition("a", PrimitiveType.I32), new ParameterDefinition("b", PrimitiveType.I32)],
                PrimitiveType.I32, "Calculator", "Add")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "CalcRpcClient");
        Assert.Contains("AddAsync", proxy.Content);
        Assert.Contains("gRPC 一元调用", proxy.Content);
        Assert.Contains("Calculator.Add", proxy.Content);
        Assert.Contains("NotImplementedException", proxy.Content);
    }

    [Fact]
    public void GenerateClientProxy_WebSocket_生成WS集成点()
    {
        var generator = new RpcGenerator();
        var returnType = PrimitiveType.Utf8;
        var serviceDef = new ServiceDefinition("Chat",
        [
            new WsEndpoint("connect", [], returnType, "/ws/chat")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "ChatRpcClient");
        Assert.Contains("ConnectAsync", proxy.Content);
        Assert.Contains("WebSocket 一元", proxy.Content);
    }

    [Fact]
    public void GenerateClientProxy_MessageQueue_生成MQ集成点()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Event",
        [
            new MessageEndpoint("publish", [new ParameterDefinition("msg", PrimitiveType.Utf8)], null, "events.orders")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "EventRpcClient");
        Assert.Contains("PublishAsync", proxy.Content);
        Assert.Contains("events.orders", proxy.Content);
    }

    [Fact]
    public void GenerateMultiEndpointService_生成所有端点()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Full",
        [
            new HttpEndpoint("check", [], PrimitiveType.Utf8, "GET", "/api/health"),
            new GrpcEndpoint("stream", [],
                PrimitiveType.Utf8, "DataService", "StreamData", streamingMode: StreamingMode.Server),
            new WsEndpoint("watch", [], PrimitiveType.Utf8, "/ws/watch"),
            new MessageEndpoint("notify", [new ParameterDefinition("event", PrimitiveType.Utf8)], null, "events.system")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "FullRpcClient");
        Assert.Contains("CheckAsync", proxy.Content);
        Assert.Contains("StreamAsync", proxy.Content);
        Assert.Contains("WatchAsync", proxy.Content);
        Assert.Contains("NotifyAsync", proxy.Content);
    }

    [Fact]
    public void GenerateClientFactory_生成工厂方法()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("User",
        [
            new HttpEndpoint("get", [], PrimitiveType.Utf8, "GET", "/api/users")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var factory = result.Files.First(f => f.TypeName == "UserRpcClientFactory");
        Assert.Contains("public static class UserRpcClientFactory", factory.Content);
        Assert.Contains("UserRpcClient Create(HttpClient", factory.Content);
    }

    [Fact]
    public void GenerateVoidEndpoint_不生成返回类型()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Health",
        [
            new HttpEndpoint("check", [], null, "GET", "/api/health")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "HealthRpcClient");
        Assert.Contains("public async Task CheckAsync", proxy.Content);
        Assert.DoesNotContain("Task<>", proxy.Content);
    }

    [Fact]
    public void GenerateGrpcClientStream_生成客户端流方法()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Upload",
        [
            new GrpcEndpoint("uploadChunks", [new ParameterDefinition("chunk", PrimitiveType.Utf8)], PrimitiveType.I32,
                "UploadService", "UploadChunks", streamingMode: StreamingMode.Client)
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "UploadRpcClient");
        Assert.Contains("IAsyncEnumerable<string> requests", proxy.Content);
        Assert.Contains("Task<int>", proxy.Content);
        Assert.Contains("客户端流", proxy.Content);
    }

    [Fact]
    public void GenerateGrpcDuplexStream_生成双向流方法()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Chat",
        [
            new GrpcEndpoint("chatStream", [new ParameterDefinition("message", PrimitiveType.Utf8)], PrimitiveType.Utf8,
                "ChatService", "ChatStream", streamingMode: StreamingMode.Duplex)
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "ChatRpcClient");
        Assert.Contains("IAsyncEnumerable<string> requests", proxy.Content);
        Assert.Contains("IAsyncEnumerable<string>", proxy.Content);
        Assert.Contains("双向流", proxy.Content);
    }

    [Fact]
    public void GenerateWsServerStream_生成服务端流方法()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Monitor",
        [
            new WsEndpoint("watchMetrics", [], PrimitiveType.F64, "/ws/metrics", streamingMode: StreamingMode.Server)
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "MonitorRpcClient");
        Assert.Contains("IAsyncEnumerable<double>", proxy.Content);
        Assert.Contains("服务端流", proxy.Content);
        Assert.Contains("StreamWebSocketMessages", proxy.Content);
    }

    [Fact]
    public void GenerateWsDuplexStream_生成双向流方法()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Terminal",
        [
            new WsEndpoint("shell", [new ParameterDefinition("input", PrimitiveType.Utf8)], PrimitiveType.Utf8,
                "/ws/shell", streamingMode: StreamingMode.Duplex)
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "TerminalRpcClient");
        Assert.Contains("IAsyncEnumerable<string> requests", proxy.Content);
        Assert.Contains("双向流", proxy.Content);
        Assert.Contains("sendTask", proxy.Content);
    }

    [Fact]
    public void GenerateMessageServerStream_生成服务端流方法()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Events",
        [
            new MessageEndpoint("subscribe", [], PrimitiveType.Utf8, "events.updates",
                streamingMode: StreamingMode.Server)
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "EventsRpcClient");
        Assert.Contains("IAsyncEnumerable<string>", proxy.Content);
        Assert.Contains("SubscribeStream", proxy.Content);
        Assert.Contains("服务端流", proxy.Content);
    }

    #region M6: RPC 版本兼容测试

    [Fact]
    public void GenerateClientProxy_DeprecatedEndpoint_生成ObsoleteAttribute()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("User",
        [
            new HttpEndpoint("getUser", [new ParameterDefinition("id", PrimitiveType.I32)], PrimitiveType.Utf8, "GET",
                "/api/users", isDeprecated: true, deprecatedMessage: "请使用 findUser 替代"),
            new HttpEndpoint("findUser", [new ParameterDefinition("id", PrimitiveType.I32)], PrimitiveType.Utf8, "GET",
                "/api/users/find")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "UserRpcClient");
        Assert.Contains("[System.Obsolete(\"请使用 findUser 替代\")]", proxy.Content);
        Assert.Contains("GetUserAsync", proxy.Content);
        Assert.Contains("FindUserAsync", proxy.Content);
    }

    [Fact]
    public void GenerateClientProxy_DeprecatedWithReplaceWith_生成转发方法()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Order",
        [
            new HttpEndpoint("createOrder", [new ParameterDefinition("order", PrimitiveType.Utf8)], PrimitiveType.Utf8,
                "POST", "/api/orders", isDeprecated: true, deprecatedMessage: "请使用 placeOrder 替代",
                replaceWith: "placeOrder"),
            new HttpEndpoint("placeOrder", [new ParameterDefinition("order", PrimitiveType.Utf8)], PrimitiveType.Utf8,
                "POST", "/api/orders/place")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "OrderRpcClient");
        Assert.Contains("[System.Obsolete(\"请使用 placeOrder 替代\")]", proxy.Content);
        Assert.Contains("CreateOrderAsync", proxy.Content);
        Assert.Contains("PlaceOrderAsync", proxy.Content);
        Assert.Contains("await PlaceOrderAsync(order, cancellationToken)", proxy.Content);
    }

    [Fact]
    public void GenerateServerInterface_DeprecatedEndpoint_生成ObsoleteAttribute()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("User",
        [
            new HttpEndpoint("getUser", [new ParameterDefinition("id", PrimitiveType.I32)], PrimitiveType.Utf8, "GET",
                "/api/users", isDeprecated: true, deprecatedMessage: "已废弃"),
            new HttpEndpoint("findUser", [new ParameterDefinition("id", PrimitiveType.I32)], PrimitiveType.Utf8, "GET",
                "/api/users/find")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ServerOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var iface = result.Files.First(f => f.TypeName == "IUserService");
        Assert.Contains("[System.Obsolete(\"已废弃\")]", iface.Content);
    }

    [Fact]
    public void GenerateServerBase_DeprecatedEndpoint_生成ObsoleteAttribute()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("User",
        [
            new HttpEndpoint("getUser", [new ParameterDefinition("id", PrimitiveType.I32)], PrimitiveType.Utf8, "GET",
                "/api/users", isDeprecated: true, replaceWith: "findUser"),
            new HttpEndpoint("findUser", [new ParameterDefinition("id", PrimitiveType.I32)], PrimitiveType.Utf8, "GET",
                "/api/users/find")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ServerOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var baseClass = result.Files.First(f => f.TypeName == "UserServiceBase");
        Assert.Contains("[System.Obsolete(\"请使用 FindUserAsync 替代\")]", baseClass.Content);
    }

    [Fact]
    public void GenerateServerController_DeprecatedEndpoint_生成ObsoleteAttribute()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("User",
        [
            new HttpEndpoint("getUser", [new ParameterDefinition("id", PrimitiveType.I32)], PrimitiveType.Utf8, "GET",
                "/api/users", isDeprecated: true, deprecatedMessage: "使用 findUser")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ServerOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var controller = result.Files.First(f => f.TypeName == "UserController");
        Assert.Contains("[System.Obsolete(\"使用 findUser\")]", controller.Content);
    }

    [Fact]
    public void GenerateClientProxy_GrpcDeprecated_生成ObsoleteAttribute()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Calc",
        [
            new GrpcEndpoint("add",
                [new ParameterDefinition("a", PrimitiveType.I32), new ParameterDefinition("b", PrimitiveType.I32)],
                PrimitiveType.I32, "Calculator", "Add", isDeprecated: true, deprecatedMessage: "请使用 sum 替代")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "CalcRpcClient");
        Assert.Contains("[System.Obsolete(\"请使用 sum 替代\")]", proxy.Content);
    }

    [Fact]
    public void GenerateClientProxy_WsDeprecated_生成ObsoleteAttribute()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Chat",
        [
            new WsEndpoint("connect", [], PrimitiveType.Utf8, "/ws/chat", isDeprecated: true,
                deprecatedMessage: "请使用 stream 替代")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "ChatRpcClient");
        Assert.Contains("[System.Obsolete(\"请使用 stream 替代\")]", proxy.Content);
    }

    [Fact]
    public void GenerateClientProxy_MessageDeprecated_生成ObsoleteAttribute()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Event",
        [
            new MessageEndpoint("publish", [new ParameterDefinition("msg", PrimitiveType.Utf8)], null, "events.orders",
                isDeprecated: true, deprecatedMessage: "请使用 send 替代")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "EventRpcClient");
        Assert.Contains("[System.Obsolete(\"请使用 send 替代\")]", proxy.Content);
    }

    [Fact]
    public void GenerateClientProxy_V1ToV2SchemaEvolution_新旧方法共存()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("UserService",
        [
            new HttpEndpoint("getUser", [new ParameterDefinition("id", PrimitiveType.I32)], PrimitiveType.Utf8, "GET",
                "/api/users", isDeprecated: true, deprecatedMessage: "v2: 请使用 findUser 替代", replaceWith: "findUser"),
            new HttpEndpoint("findUser", [new ParameterDefinition("id", PrimitiveType.I32)], PrimitiveType.Utf8, "GET",
                "/api/users/find")
        ], version: "2.0", deprecatedEndpoints: ["getUser"]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "UserServiceRpcClient");
        Assert.Contains("GetUserAsync", proxy.Content);
        Assert.Contains("FindUserAsync", proxy.Content);
        Assert.Contains("[System.Obsolete(\"v2: 请使用 findUser 替代\")]", proxy.Content);
        Assert.Contains("await FindUserAsync(id, cancellationToken)", proxy.Content);
    }

    [Fact]
    public void GenerateClientProxy_DeprecatedWithoutReplaceWith_仅标记Obsolete()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Legacy",
        [
            new HttpEndpoint("oldMethod", [], PrimitiveType.Utf8, "GET", "/api/old", isDeprecated: true)
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "LegacyRpcClient");
        Assert.Contains("[System.Obsolete(\"端点 oldMethod 已废弃\")]", proxy.Content);
        Assert.Contains("OldMethodAsync", proxy.Content);
    }

    [Fact]
    public void GenerateClientProxy_DeprecatedVoidEndpoint_转发方法无返回值()
    {
        var generator = new RpcGenerator();
        var serviceDef = new ServiceDefinition("Admin",
        [
            new HttpEndpoint("deleteOld", [new ParameterDefinition("id", PrimitiveType.I32)], null, "DELETE",
                "/api/old", isDeprecated: true, replaceWith: "remove"),
            new HttpEndpoint("remove", [new ParameterDefinition("id", PrimitiveType.I32)], null, "DELETE",
                "/api/remove")
        ]);

        var schema = new SchemaIR("test", services: [serviceDef]);
        var context = new GeneratorContext { Schema = schema, OutputPath = "./out", Options = ClientOptions };

        var result = generator.Generate(context);

        Assert.Empty(result.Errors);
        var proxy = result.Files.First(f => f.TypeName == "AdminRpcClient");
        Assert.Contains("await RemoveAsync(id, cancellationToken)", proxy.Content);
    }

    #endregion
}