namespace Valkyrie.Tests.ParserTests;

public class SystemParserTests : ValkyrieParserTestBase
{
    [Fact]
    public void Parse_SimpleSystem_ShouldSucceed()
    {
        var source = """
                     system MovementSystem {
                         query = Query.all(Position, Velocity);
                         
                         update(dt: f32) {
                             // implementation
                         }
                     }
                     """;
        var unit = parse_with_timeout(source);
        var system = Assert.IsType<DeclareSystem>(unit.Declarations[0]);

        Assert.Equal("MovementSystem", system.Name?.Name);
        Assert.Single(system.Queries);

        Assert.NotNull(system.Body);
        Assert.Single(system.Body!.Methods);
        Assert.Equal("update", system.Body.Methods[0].Name?.Name);
    }

    [Fact]
    public void Parse_SystemWithMultipleQueries_ShouldSucceed()
    {
        var source = """
                     system CollisionSystem {
                         query all(Collider, Transform);
                         query any(Trigger, StaticObject);
                     }
                     """;
        var unit = parse_with_timeout(source);
        var system = Assert.IsType<DeclareSystem>(unit.Declarations[0]);

        Assert.Equal(2, system.Queries.Count);
    }

    [Fact]
    public void Parse_SystemWithAnnotations_ShouldSucceed()
    {
        var source = """
                     [ExecuteIn(Stage.Update)]
                     public system PhysicsSystem {
                         query all(Rigidbody);
                     }
                     """;
        var unit = parse_with_timeout(source);
        var system = Assert.IsType<DeclareSystem>(unit.Declarations[0]);

        var attrs = system.Annotations.Attributes();
        Assert.Single(attrs);
        Assert.Equal("ExecuteIn", attrs[0].Name);

        Assert.Single(system.Annotations.Modifiers);
        Assert.Equal("public", system.Annotations.Modifiers[0].Name);
    }

    [Fact]
    public void Parse_SystemWithDomains_ShouldSucceed()
    {
        var source = """
                     system DomainSystem {
                         Client {
                             // implementation
                         }
                         Server {
                             // implementation
                         }
                         
                         query all(SyncData);
                     }
                     """;
        var unit = parse_with_timeout(source);
        var system = Assert.IsType<DeclareSystem>(unit.Declarations[0]);

        Assert.Equal(2, system.Body!.Domains.Count);
        Assert.Equal("Client", system.Body.Domains[0].Name?.Name);
        Assert.Equal("Server", system.Body.Domains[1].Name?.Name);
    }
}
