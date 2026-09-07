using FluentAssertions;
using TdpGis.Domain;
using TdpGis.Infrastructure.Persistence;
using Xunit;

namespace TdpGis.Infrastructure.Tests.Persistence;

public class GisConfigurationRepositoryTests
{
    [Fact]
    public async Task CreateConnectionAsync_ShouldPersistConnection_WithExistingDataSource()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var source = new DataSourceSetting
        {
            Id = Guid.NewGuid(),
            Name = "Test source",
            ConnectionString = "mongodb://localhost:27017/db",
            DatabaseType = SourceType.Mongodb
        };
        fixture.DbContext.DataSourceSettings.Add(source);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new GisConfigurationRepository(fixture.DbContext);
        var created = await sut.CreateConnectionAsync(
            new GisConnection
            {
                Id = Guid.NewGuid(),
                Name = "Roads",
                Description = "roads",
                GeometryType = GeometryType.MultiPolygon,
                QueryField = "name",
                PropertyMappings = [],
                Entity = "roads",
                EntityLabel = "Roads",
                DataSource = source
            },
            TestContext.Current.CancellationToken);

        created.Id.Should().NotBeEmpty();
        fixture.DbContext.GisConnections.Count().Should().Be(1);
    }

    [Fact]
    public async Task CreateMongoDataSourceAsync_ShouldTrimAndDeduplicate()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var sut = new GisConfigurationRepository(fixture.DbContext);

        var first = await sut.CreateMongoDataSourceAsync("Primary mongo", "  mongodb://localhost:27017/db  ",
            TestContext.Current.CancellationToken);
        var second =
            await sut.CreateMongoDataSourceAsync("Primary mongo renamed", "mongodb://localhost:27017/db",
                TestContext.Current.CancellationToken);

        second.Id.Should().Be(first.Id);
        second.Name.Should().Be("Primary mongo renamed");
        fixture.DbContext.DataSourceSettings.Count().Should().Be(1);
    }

    [Fact]
    public async Task GisConnectionNameExists_ShouldHonorExcludeId()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var workspace = new GisWorkspace { Id = Guid.NewGuid(), Name = "ws" };
        var source = new DataSourceSetting
        {
            Id = Guid.NewGuid(),
            Name = "Test source",
            ConnectionString = "mongodb://localhost:27017/db",
            DatabaseType = SourceType.Mongodb
        };
        var conn = new GisConnection
        {
            Id = Guid.NewGuid(),
            Name = "Roads",
            Description = "d",
            GeometryType = GeometryType.MultiPolygon,
            QueryField = "name",
            PropertyMappings = [],
            Entity = "roads",
            EntityLabel = "Roads",
            GisWorkspace = workspace,
            DataSource = source
        };
        fixture.DbContext.GisConnections.Add(conn);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sut = new GisConfigurationRepository(fixture.DbContext);

        sut.GisConnectionNameExists(" Roads ").Should().BeTrue();
        sut.GisConnectionNameExists("Roads", conn.Id).Should().BeFalse();
    }

    [Fact]
    public async Task CreateWorkspaceAccessTokenAsync_ShouldCreateToken_AndNormalizeUtcDate()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var workspace = new GisWorkspace { Id = Guid.NewGuid(), Name = "ws" };
        fixture.DbContext.GisWorkspaces.Add(workspace);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sut = new GisConfigurationRepository(fixture.DbContext);

        var localDate = DateTime.SpecifyKind(new DateTime(2030, 4, 5, 6, 7, 8), DateTimeKind.Local);
        var token = await sut.CreateWorkspaceAccessTokenAsync(
            workspace.Id,
            " token ",
            localDate,
            true,
            false,
            TestContext.Current.CancellationToken);

        token.Name.Should().Be("token");
        token.AccessToken.Should().NotBeNullOrWhiteSpace();
        token.AccessToken.Should().NotContain("+").And.NotContain("/").And.NotContain("=");
        token.ExpiredDateTime.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task UpdateWorkspaceAccessTokenAsync_ShouldReturnNull_WhenTokenDoesNotExist()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var workspace = new GisWorkspace { Id = Guid.NewGuid(), Name = "ws" };
        fixture.DbContext.GisWorkspaces.Add(workspace);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sut = new GisConfigurationRepository(fixture.DbContext);

        var result = await sut.UpdateWorkspaceAccessTokenAsync(
            Guid.NewGuid(),
            workspace.Id,
            "x",
            DateTime.UtcNow.AddDays(1),
            true,
            false,
            TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetConnectionsWorkspaceAsync_ShouldAssignWorkspaceAndReturnUpdatedCount()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var workspace = new GisWorkspace { Id = Guid.NewGuid(), Name = "ws" };
        var source = new DataSourceSetting
        {
            Id = Guid.NewGuid(),
            Name = "Test source",
            ConnectionString = "mongodb://localhost:27017/db",
            DatabaseType = SourceType.Mongodb
        };

        var c1 = new GisConnection
        {
            Id = Guid.NewGuid(),
            Name = "A",
            Description = "a",
            GeometryType = GeometryType.MultiPoint,
            QueryField = "q",
            PropertyMappings = [],
            Entity = "a",
            EntityLabel = "A",
            DataSource = source
        };
        var c2 = new GisConnection
        {
            Id = Guid.NewGuid(),
            Name = "B",
            Description = "b",
            GeometryType = GeometryType.MultiPolygon,
            QueryField = "q",
            PropertyMappings = [],
            Entity = "b",
            EntityLabel = "B",
            DataSource = source
        };

        fixture.DbContext.GisWorkspaces.Add(workspace);
        fixture.DbContext.GisConnections.AddRange(c1, c2);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new GisConfigurationRepository(fixture.DbContext);
        var updated = await sut.SetConnectionsWorkspaceAsync(
            workspace.Id,
            [c1.Id, c2.Id],
            TestContext.Current.CancellationToken);

        updated.Should().Be(2);
        fixture.DbContext.GisConnections.All(x => x.GisWorkspaceId == workspace.Id).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateConnectionAsync_ShouldReturnNull_WhenConnectionDoesNotExist()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var source = new DataSourceSetting
        {
            Id = Guid.NewGuid(),
            Name = "Test source",
            ConnectionString = "mongodb://localhost:27017/db",
            DatabaseType = SourceType.Mongodb
        };
        fixture.DbContext.DataSourceSettings.Add(source);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sut = new GisConfigurationRepository(fixture.DbContext);

        var result = await sut.UpdateConnectionAsync(
            Guid.NewGuid(),
            source.Id,
            "Name",
            "Desc",
            "Entity",
            "Label",
            "q",
            GeometryType.MultiPoint,
            null,
            [],
            TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateConnectionAsync_ShouldUpdateFieldsAndReplacePropertyMappings()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var source = new DataSourceSetting
        {
            Id = Guid.NewGuid(),
            Name = "Test source",
            ConnectionString = "mongodb://localhost:27017/db",
            DatabaseType = SourceType.Mongodb
        };
        var workspace = new GisWorkspace { Id = Guid.NewGuid(), Name = "ws" };
        var existing = new GisConnection
        {
            Id = Guid.NewGuid(),
            Name = "Old",
            Description = "old",
            GeometryType = GeometryType.MultiPoint,
            QueryField = "oldq",
            PropertyMappings =
            [
                new PropertyMapping
                {
                    Id = Guid.NewGuid(), PropertyName = "old", PropertyLabel = "Old", ColumnType = PropertyType.Normal
                }
            ],
            Entity = "old_entity",
            EntityLabel = "OldLabel",
            DataSource = source,
            GisWorkspace = workspace
        };
        fixture.DbContext.GisConnections.Add(existing);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new GisConfigurationRepository(fixture.DbContext);
        var result = await sut.UpdateConnectionAsync(
            existing.Id,
            source.Id,
            "  NewName  ",
            "  NewDescription  ",
            "  new_entity  ",
            "  NewLabel  ",
            "  new_q  ",
            GeometryType.MultiPolygon,
            workspace.Id,
            [
                new PropertyMapping
                {
                    Id = Guid.NewGuid(), PropertyName = "new", PropertyLabel = "New", ColumnType = PropertyType.Object
                }
            ],
            TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Name.Should().Be("NewName");
        result.Description.Should().Be("NewDescription");
        result.Entity.Should().Be("new_entity");
        result.EntityLabel.Should().Be("NewLabel");
        result.QueryField.Should().Be("new_q");
        result.GeometryType.Should().Be(GeometryType.MultiPolygon);
        result.PropertyMappings.Should().ContainSingle(p => p.PropertyName == "new");
    }

    [Fact]
    public async Task GetAllConnections_ShouldOrderByName_AndIncludeRelatedData()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var ws = new GisWorkspace { Id = Guid.NewGuid(), Name = "ws" };
        var source = new DataSourceSetting
        {
            Id = Guid.NewGuid(),
            Name = "Test source",
            ConnectionString = "mongodb://localhost:27017/db",
            DatabaseType = SourceType.Mongodb
        };
        fixture.DbContext.GisConnections.AddRange(
            new GisConnection
            {
                Id = Guid.NewGuid(),
                Name = "B",
                Description = "",
                GeometryType = GeometryType.MultiPoint,
                QueryField = "q",
                PropertyMappings = [],
                Entity = "b",
                EntityLabel = "B",
                DataSource = source,
                GisWorkspace = ws
            },
            new GisConnection
            {
                Id = Guid.NewGuid(),
                Name = "A",
                Description = "",
                GeometryType = GeometryType.MultiPoint,
                QueryField = "q",
                PropertyMappings =
                [
                    new PropertyMapping
                    {
                        Id = Guid.NewGuid(),
                        PropertyName = "p",
                        PropertyLabel = "P",
                        ColumnType = PropertyType.Normal
                    }
                ],
                Entity = "a",
                EntityLabel = "A",
                DataSource = source,
                GisWorkspace = ws
            });
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new GisConfigurationRepository(fixture.DbContext);
        var list = sut.GetAllConnections();

        list.Should().HaveCount(2);
        list[0].Name.Should().Be("A");
        list[0].PropertyMappings.Should().ContainSingle();
        list[0].DataSource.Should().NotBeNull();
        list[0].GisWorkspace.Should().NotBeNull();
        list[1].Name.Should().Be("B");
    }

    [Fact]
    public async Task GetConnectionById_ShouldReturnTrackedGraph_WhenFound()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var source = new DataSourceSetting
        {
            Id = Guid.NewGuid(),
            Name = "Test source",
            ConnectionString = "mongodb://localhost:27017/db",
            DatabaseType = SourceType.Mongodb
        };
        var id = Guid.NewGuid();
        fixture.DbContext.GisConnections.Add(
            new GisConnection
            {
                Id = id,
                Name = "One",
                Description = "d",
                GeometryType = GeometryType.MultiPolygon,
                QueryField = "q",
                PropertyMappings =
                [
                    new PropertyMapping
                    {
                        Id = Guid.NewGuid(),
                        PropertyName = "x",
                        PropertyLabel = "X",
                        ColumnType = PropertyType.Object
                    }
                ],
                Entity = "e",
                EntityLabel = "E",
                DataSource = source
            });
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new GisConfigurationRepository(fixture.DbContext);
        var found = sut.GetConnectionById(id);
        var missing = sut.GetConnectionById(Guid.NewGuid());

        missing.Should().BeNull();
        found.Should().NotBeNull();
        found!.Id.Should().Be(id);
        found.PropertyMappings.Should().ContainSingle();
        found.DataSource!.ConnectionString.Should().Contain("mongodb");
    }

    [Fact]
    public async Task GisConnectionNameExists_ShouldReturnFalse_WhenNoMatch()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var sut = new GisConfigurationRepository(fixture.DbContext);

        sut.GisConnectionNameExists("Nothing").Should().BeFalse();
    }

    [Fact]
    public async Task CreateDataSourceAsync_ShouldPersistPostgresAndSqlServer()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var sut = new GisConfigurationRepository(fixture.DbContext);

        var pg = await sut.CreateDataSourceAsync(SourceType.Postgres, "Local PG", "Host=localhost;Database=gis",
            TestContext.Current.CancellationToken);
        var sql = await sut.CreateDataSourceAsync(SourceType.SqlServer, "Local SQL", "Server=localhost;Database=GisDb",
            TestContext.Current.CancellationToken);

        pg.DatabaseType.Should().Be(SourceType.Postgres);
        pg.Name.Should().Be("Local PG");
        sql.DatabaseType.Should().Be(SourceType.SqlServer);
        sql.Name.Should().Be("Local SQL");
        sut.GetDataSources().Should().HaveCount(2);
        sut.GetMongoDataSources().Should().BeEmpty();
    }

    [Fact]
    public async Task GetDataSources_ShouldFilterByType_WhenRequested()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        fixture.DbContext.DataSourceSettings.AddRange(
            new DataSourceSetting
            {
                Id = Guid.NewGuid(),
                Name = "Test source",
                ConnectionString = "mongodb://a",
                DatabaseType = SourceType.Mongodb
            },
            new DataSourceSetting
            {
                Id = Guid.NewGuid(),
                Name = "Test source",
                ConnectionString = "Host=pg",
                DatabaseType = SourceType.Postgres
            },
            new DataSourceSetting
            {
                Id = Guid.NewGuid(),
                Name = "Test source",
                ConnectionString = "Server=sql",
                DatabaseType = SourceType.SqlServer
            });
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new GisConfigurationRepository(fixture.DbContext);

        sut.GetDataSources().Should().HaveCount(3);
        sut.GetDataSources(SourceType.Postgres).Should().ContainSingle(x => x.DatabaseType == SourceType.Postgres);
        sut.GetDataSources(SourceType.SqlServer).Should().ContainSingle(x => x.DatabaseType == SourceType.SqlServer);
    }

    [Fact]
    public async Task GetMongoDataSources_ShouldExcludeNonMongo_AndOrderByConnectionString()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        fixture.DbContext.DataSourceSettings.AddRange(
            new DataSourceSetting
            {
                Id = Guid.NewGuid(),
                Name = "Test source",
                ConnectionString = "mongodb://b",
                DatabaseType = SourceType.Mongodb
            },
            new DataSourceSetting
            {
                Id = Guid.NewGuid(),
                Name = "Test source",
                ConnectionString = "mongodb://a",
                DatabaseType = SourceType.Mongodb
            },
            new DataSourceSetting
            {
                Id = Guid.NewGuid(),
                Name = "Test source",
                ConnectionString = "postgres://x",
                DatabaseType = SourceType.Postgres
            });
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new GisConfigurationRepository(fixture.DbContext);
        var mongo = sut.GetMongoDataSources();

        mongo.Should().HaveCount(2);
        mongo[0].ConnectionString.Should().Be("mongodb://a");
        mongo[1].ConnectionString.Should().Be("mongodb://b");
    }

    [Fact]
    public async Task GetDataSourceById_ShouldReturnRowOrNull()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var ds = new DataSourceSetting
        {
            Id = Guid.NewGuid(),
            Name = "Test source",
            ConnectionString = "mongodb://localhost:27017/db",
            DatabaseType = SourceType.Mongodb
        };
        fixture.DbContext.DataSourceSettings.Add(ds);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new GisConfigurationRepository(fixture.DbContext);

        sut.GetDataSourceById(ds.Id)!.Id.Should().Be(ds.Id);
        sut.GetDataSourceById(Guid.NewGuid()).Should().BeNull();
    }

    [Fact]
    public async Task CreateMongoDataSourceAsync_ShouldInsertNewRow_WhenConnectionStringDiffers()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var sut = new GisConfigurationRepository(fixture.DbContext);

        var a = await sut.CreateMongoDataSourceAsync("Host A", "mongodb://host-a/db",
            TestContext.Current.CancellationToken);
        var b = await sut.CreateMongoDataSourceAsync("Host B", "mongodb://host-b/db",
            TestContext.Current.CancellationToken);

        a.Id.Should().NotBe(b.Id);
        a.Name.Should().Be("Host A");
        b.Name.Should().Be("Host B");
        fixture.DbContext.DataSourceSettings.Count().Should().Be(2);
    }

    [Fact]
    public async Task GetAllWorkspaces_AndGetWorkspaceById_ShouldIncludeNavigations()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var ws = new GisWorkspace { Id = Guid.NewGuid(), Name = "Alpha" };
        var source = new DataSourceSetting
        {
            Id = Guid.NewGuid(),
            Name = "Test source",
            ConnectionString = "mongodb://localhost:27017/db",
            DatabaseType = SourceType.Mongodb
        };
        var conn = new GisConnection
        {
            Id = Guid.NewGuid(),
            Name = "C",
            Description = "",
            GeometryType = GeometryType.MultiPoint,
            QueryField = "q",
            PropertyMappings = [],
            Entity = "e",
            EntityLabel = "E",
            GisWorkspace = ws,
            DataSource = source
        };
        var tok = new GisWorkspaceAccessToken
        {
            Id = Guid.NewGuid(),
            Name = "t",
            AccessToken = "tok-unique-1",
            ExpiredDateTime = DateTime.UtcNow.AddDays(1),
            IsActive = true,
            IsPublic = false,
            GisWorkspace = ws
        };
        fixture.DbContext.GisWorkspaceAccessTokens.Add(tok);
        fixture.DbContext.GisConnections.Add(conn);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new GisConfigurationRepository(fixture.DbContext);
        var all = sut.GetAllWorkspaces();
        var one = sut.GetWorkspaceById(ws.Id);
        var missing = sut.GetWorkspaceById(Guid.NewGuid());

        missing.Should().BeNull();
        all.Should().ContainSingle(w => w.Id == ws.Id);
        all[0].Entities.Should().ContainSingle(e => e.Id == conn.Id);
        all[0].AccessTokens.Should().ContainSingle(t => t.Id == tok.Id);

        one.Should().NotBeNull();
        one!.Entities.Should().ContainSingle();
        one.AccessTokens.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateWorkspaceAsync_ShouldTrimName()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var sut = new GisConfigurationRepository(fixture.DbContext);

        var ws = await sut.CreateWorkspaceAsync("  spaced  ", TestContext.Current.CancellationToken);

        ws.Name.Should().Be("spaced");
        fixture.DbContext.GisWorkspaces.Count().Should().Be(1);
    }

    [Fact]
    public async Task UpdateWorkspaceAsync_ShouldTrimAndPersist_WhenFound()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var ws = new GisWorkspace { Id = Guid.NewGuid(), Name = "old" };
        fixture.DbContext.GisWorkspaces.Add(ws);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sut = new GisConfigurationRepository(fixture.DbContext);

        var updated = await sut.UpdateWorkspaceAsync(ws.Id, "  new  ", TestContext.Current.CancellationToken);
        var missing = await sut.UpdateWorkspaceAsync(Guid.NewGuid(), "x", TestContext.Current.CancellationToken);

        missing.Should().BeNull();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("new");
    }

    [Fact]
    public async Task CreateWorkspaceAccessTokenAsync_ShouldThrow_WhenWorkspaceMissing()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var sut = new GisConfigurationRepository(fixture.DbContext);

        var act = async () => await sut.CreateWorkspaceAccessTokenAsync(
            Guid.NewGuid(),
            "n",
            DateTime.UtcNow.AddDays(1),
            true,
            false,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Workspace*");
    }

    [Fact]
    public async Task UpdateWorkspaceAccessTokenAsync_ShouldPersistChanges_WhenTokenExists()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var ws = new GisWorkspace { Id = Guid.NewGuid(), Name = "ws" };
        var other = new GisWorkspace { Id = Guid.NewGuid(), Name = "other" };
        fixture.DbContext.GisWorkspaces.AddRange(ws, other);
        var token = new GisWorkspaceAccessToken
        {
            Id = Guid.NewGuid(),
            GisWorkspaceId = ws.Id,
            Name = "old",
            AccessToken = "tok-unique-update-1",
            ExpiredDateTime = DateTime.UtcNow.AddDays(1),
            IsActive = true,
            IsPublic = false,
            GisWorkspace = ws
        };
        fixture.DbContext.GisWorkspaceAccessTokens.Add(token);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sut = new GisConfigurationRepository(fixture.DbContext);

        var exp = new DateTime(2040, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var updated = await sut.UpdateWorkspaceAccessTokenAsync(
            token.Id,
            other.Id,
            "  renamed  ",
            exp,
            false,
            true,
            TestContext.Current.CancellationToken);

        updated.Should().NotBeNull();
        updated!.GisWorkspaceId.Should().Be(other.Id);
        updated.Name.Should().Be("renamed");
        updated.IsActive.Should().BeFalse();
        updated.IsPublic.Should().BeTrue();
        updated.ExpiredDateTime.Should().Be(exp);
    }

    [Fact]
    public async Task UpdateWorkspaceAccessTokenAsync_ShouldThrow_WhenTargetWorkspaceMissing()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var ws = new GisWorkspace { Id = Guid.NewGuid(), Name = "ws" };
        fixture.DbContext.GisWorkspaces.Add(ws);
        var token = new GisWorkspaceAccessToken
        {
            Id = Guid.NewGuid(),
            GisWorkspaceId = ws.Id,
            Name = "t",
            AccessToken = "tok-unique-throw-1",
            ExpiredDateTime = DateTime.UtcNow.AddDays(1),
            IsActive = true,
            IsPublic = false,
            GisWorkspace = ws
        };
        fixture.DbContext.GisWorkspaceAccessTokens.Add(token);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sut = new GisConfigurationRepository(fixture.DbContext);

        var act = async () => await sut.UpdateWorkspaceAccessTokenAsync(
            token.Id,
            Guid.NewGuid(),
            "n",
            DateTime.UtcNow.AddDays(2),
            true,
            false,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Workspace*");
    }

    [Fact]
    public async Task SetConnectionsWorkspaceAsync_ShouldReturnZero_WhenNoIds()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var ws = new GisWorkspace { Id = Guid.NewGuid(), Name = "ws" };
        fixture.DbContext.GisWorkspaces.Add(ws);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sut = new GisConfigurationRepository(fixture.DbContext);

        var n = await sut.SetConnectionsWorkspaceAsync(ws.Id, [], TestContext.Current.CancellationToken);

        n.Should().Be(0);
    }

    [Fact]
    public async Task SetConnectionsWorkspaceAsync_ShouldClearUncheckedEntitiesFromWorkspace()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var ws = new GisWorkspace { Id = Guid.NewGuid(), Name = "ws" };
        var other = new GisWorkspace { Id = Guid.NewGuid(), Name = "other" };
        var source = new DataSourceSetting
        {
            Id = Guid.NewGuid(),
            Name = "Test source",
            ConnectionString = "mongodb://localhost:27017/db",
            DatabaseType = SourceType.Mongodb
        };

        var keep = new GisConnection
        {
            Id = Guid.NewGuid(),
            Name = "Keep",
            Description = "",
            GeometryType = GeometryType.Point,
            QueryField = "q",
            PropertyMappings = [],
            Entity = "keep",
            EntityLabel = "Keep",
            GisWorkspaceId = ws.Id,
            DataSource = source
        };
        var remove = new GisConnection
        {
            Id = Guid.NewGuid(),
            Name = "Remove",
            Description = "",
            GeometryType = GeometryType.Point,
            QueryField = "q",
            PropertyMappings = [],
            Entity = "remove",
            EntityLabel = "Remove",
            GisWorkspaceId = ws.Id,
            DataSource = source
        };
        var elsewhere = new GisConnection
        {
            Id = Guid.NewGuid(),
            Name = "Elsewhere",
            Description = "",
            GeometryType = GeometryType.Point,
            QueryField = "q",
            PropertyMappings = [],
            Entity = "else",
            EntityLabel = "Else",
            GisWorkspaceId = other.Id,
            DataSource = source
        };

        fixture.DbContext.GisWorkspaces.AddRange(ws, other);
        fixture.DbContext.GisConnections.AddRange(keep, remove, elsewhere);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new GisConfigurationRepository(fixture.DbContext);
        var assigned = await sut.SetConnectionsWorkspaceAsync(
            ws.Id,
            [keep.Id],
            TestContext.Current.CancellationToken);

        assigned.Should().Be(1);
        keep.GisWorkspaceId.Should().Be(ws.Id);
        remove.GisWorkspaceId.Should().BeNull();
        elsewhere.GisWorkspaceId.Should().Be(other.Id);
    }

    [Fact]
    public async Task SetConnectionsWorkspaceAsync_ShouldThrow_WhenWorkspaceDoesNotExist()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var sut = new GisConfigurationRepository(fixture.DbContext);

        var act = async () => await sut.SetConnectionsWorkspaceAsync(
            Guid.NewGuid(),
            [Guid.NewGuid()],
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Workspace*");
    }

    [Fact]
    public async Task SetConnectionsWorkspaceAsync_ShouldOnlyAssignExistingConnectionIds()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var ws = new GisWorkspace { Id = Guid.NewGuid(), Name = "ws" };
        var source = new DataSourceSetting
        {
            Id = Guid.NewGuid(),
            Name = "Test source",
            ConnectionString = "mongodb://localhost:27017/db",
            DatabaseType = SourceType.Mongodb
        };
        var existing = new GisConnection
        {
            Id = Guid.NewGuid(),
            Name = "X",
            Description = "",
            GeometryType = GeometryType.MultiPoint,
            QueryField = "q",
            PropertyMappings = [],
            Entity = "x",
            EntityLabel = "X",
            DataSource = source
        };
        fixture.DbContext.GisWorkspaces.Add(ws);
        fixture.DbContext.GisConnections.Add(existing);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sut = new GisConfigurationRepository(fixture.DbContext);

        var updated = await sut.SetConnectionsWorkspaceAsync(
            ws.Id,
            [existing.Id, Guid.NewGuid()],
            TestContext.Current.CancellationToken);

        updated.Should().Be(1);
        existing.GisWorkspaceId.Should().Be(ws.Id);
    }

    [Fact]
    public async Task UpdateConnectionAsync_ShouldThrow_WhenDataSourceIdNotFound()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var source = new DataSourceSetting
        {
            Id = Guid.NewGuid(),
            Name = "Test source",
            ConnectionString = "mongodb://localhost:27017/db",
            DatabaseType = SourceType.Mongodb
        };
        var conn = new GisConnection
        {
            Id = Guid.NewGuid(),
            Name = "N",
            Description = "d",
            GeometryType = GeometryType.MultiPoint,
            QueryField = "q",
            PropertyMappings = [],
            Entity = "e",
            EntityLabel = "E",
            DataSource = source
        };
        fixture.DbContext.GisConnections.Add(conn);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sut = new GisConfigurationRepository(fixture.DbContext);

        var act = async () => await sut.UpdateConnectionAsync(
            conn.Id,
            Guid.NewGuid(),
            "a",
            "b",
            "c",
            "d",
            "e",
            GeometryType.MultiPolygon,
            null,
            [],
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*data source*");
    }
}