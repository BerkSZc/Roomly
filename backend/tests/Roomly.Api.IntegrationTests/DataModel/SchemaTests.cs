using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Roomly.Api.Features.Auth;
using Roomly.Api.Features.Groups;
using Roomly.Api.IntegrationTests.Infrastructure;

namespace Roomly.Api.IntegrationTests.DataModel;

[Collection(PostgresCollection.Name)]
public sealed partial class SchemaTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Migrate_OnEmptyDatabase_CreatesExpectedSchema_DM01()
    {
        var tables = await QueryAsync(
            "SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public'");

        tables.Should().BeEquivalentTo(
            "asp_net_users", "asp_net_user_claims", "asp_net_user_logins", "asp_net_user_tokens",
            "refresh_tokens", "groups", "group_memberships", "invite_codes", "__ef_migrations_history");
    }

    [Fact]
    public async Task Migrate_AllTableColumnIndexAndConstraintNames_AreSnakeCase_DM02()
    {
        var names = new List<string>();
        names.AddRange(await QueryAsync(
            "SELECT table_name || '.' || column_name AS \"Value\" FROM information_schema.columns WHERE table_schema = 'public'"));
        names.AddRange(await QueryAsync(
            "SELECT indexname AS \"Value\" FROM pg_indexes WHERE schemaname = 'public'"));
        names.AddRange(await QueryAsync(
            "SELECT c.conname AS \"Value\" FROM pg_constraint c JOIN pg_namespace n ON n.oid = c.connamespace WHERE n.nspname = 'public'"));

        names.Should().NotBeEmpty();
        names.Should().AllSatisfy(name => SnakeCase().IsMatch(name).Should().BeTrue($"'{name}' snake_case olmalı"));
    }

    [Fact]
    public async Task Migrate_TimeColumns_AreTimestamptzWithoutDefaults_DM13()
    {
        var timeColumns = await QueryAsync(
            "SELECT table_name || '.' || column_name || ':' || data_type AS \"Value\" FROM information_schema.columns " +
            "WHERE table_schema = 'public' AND (column_name LIKE '%\\_at' OR data_type LIKE 'timestamp%')");
        var columnsWithDefault = await QueryAsync(
            "SELECT table_name || '.' || column_name AS \"Value\" FROM information_schema.columns " +
            "WHERE table_schema = 'public' AND column_default IS NOT NULL");

        timeColumns.Should().NotBeEmpty()
            .And.AllSatisfy(c => c.Should().EndWith(":timestamp with time zone"));
        columnsWithDefault.Should().BeEmpty("zamanı ve kimlikleri uygulama yazar, şemada varsayılan yok");
    }

    [Fact]
    public async Task Migrate_EveryNamedConstraintConstant_ExistsInDatabase_DM14()
    {
        var constraintNames = typeof(AuthDbNames).GetFields()
            .Concat(typeof(GroupsDbNames).GetFields())
            .Where(f => f.IsLiteral && !f.Name.EndsWith("Table", StringComparison.Ordinal))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();
        var databaseNames = await QueryAsync(
            "SELECT indexname AS \"Value\" FROM pg_indexes WHERE schemaname = 'public' " +
            "UNION SELECT c.conname FROM pg_constraint c JOIN pg_namespace n ON n.oid = c.connamespace WHERE n.nspname = 'public'");

        constraintNames.Should().HaveCount(16, "spec 0002'deki Kısıt adları tablosunda 16 ad var");
        databaseNames.Should().Contain(constraintNames);
    }

    [Fact]
    public async Task InviteCodes_HaveNoPlainTextCodeColumn_DM07()
    {
        var columns = await QueryAsync(
            "SELECT column_name AS \"Value\" FROM information_schema.columns WHERE table_name = 'invite_codes'");

        columns.Should().BeEquivalentTo(
            "id", "group_id", "code_hash", "created_by_user_id", "created_at", "expires_at", "revoked_at");
    }

    [Fact]
    public void AppDbContext_DeclaresNoDbSets_ModulesRegisterThroughConfigurations_DM12()
    {
        var declaredSets = typeof(Roomly.Api.Infrastructure.Persistence.AppDbContext)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));
        using var db = fixture.CreateContext();

        declaredSets.Should().BeEmpty();
        db.Model.FindEntityType(typeof(Roomly.Api.Features.Groups.Group)).Should().NotBeNull();
        db.Model.FindEntityType(typeof(GroupMembership)).Should().NotBeNull();
        db.Model.FindEntityType(typeof(InviteCode)).Should().NotBeNull();
        db.Model.FindEntityType(typeof(RefreshToken)).Should().NotBeNull();
    }

    private async Task<List<string>> QueryAsync(string sql)
    {
        await using var db = fixture.CreateContext();
        return await db.Database.SqlQueryRaw<string>(sql).ToListAsync();
    }

    [GeneratedRegex("^[a-z0-9_.]+$")]
    private static partial Regex SnakeCase();
}
